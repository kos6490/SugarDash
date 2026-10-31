using System;
using System.Collections.Generic;
using System.Linq;
using SugarDash.App;
using SugarDash.Domain.Config;
using SugarDash.Domain.Director;
using SugarDash.Domain.Events;
using SugarDash.Domain.Glucose;
using SugarDash.Domain.Hypo;
using SugarDash.Domain.Ports;
using SugarDash.Domain.Tests;
using SugarDash.Presentation.Runner;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SugarDash.Presentation.Tests
{
    public class PlayerControllerTests
    {
        private const float Eps = 1e-4f;

        private sealed class NoRandom : IRandomSource
        {
            public double NextDouble() => throw new InvalidOperationException("급하강 확률 0에서는 난수를 읽지 않아야 한다");
        }

        private sealed class NullLog : ILogRepository
        {
            public string BeginRun() => "run";
            public void EndRun() { }
        }

        private GameObject _go;
        private PlayerController _player;
        private RunController _run;
        private List<TrackSnackEaten> _eaten;

        [SetUp]
        public void SetUp()
        {
            var config = new ConfigBuilder().Build();
            var bus = new DomainEventBus();
            _eaten = new List<TrackSnackEaten>();
            bus.Subscribe<TrackSnackEaten>(e => _eaten.Add(e));
            _run = new RunController(config.Runner, () =>
            {
                var model = new GlucoseModel(config.Glucose);
                var director = new EventDirector(config.Glucose, model, new NoRandom(), bus);
                var fsm = new HypoEventFsm(config, model, director, bus);
                return new RunSession(model, director, fsm, bus.Subscribe<HypoEventTriggered>(fsm.Begin));
            }, new NullLog(), bus);
            _run.StartRun();

            _go = new GameObject("Player");
            _player = _go.AddComponent<PlayerController>();
            _player.Init(_run, false);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_go);

        private static TrackItem Item(TrackItemKind kind, int lane = 0, string snackId = null) =>
            new TrackItem(kind, lane, 0, snackId);

        [Test]
        public void MoveLeftRight_ClampsAtOuterLanes_AndSlidesOverLaneChangeTime()
        {
            _player.MoveLeft();
            _player.MoveLeft();
            Assert.That(_player.TargetLane, Is.EqualTo(-1));

            _player.Advance(_player.LaneChangeSec / 2);
            Assert.That(_player.X, Is.EqualTo(-_player.LaneWidth / 2).Within(Eps));
            _player.Advance(_player.LaneChangeSec);
            Assert.That(_player.X, Is.EqualTo(-_player.LaneWidth).Within(Eps));
            Assert.That(_player.CurrentLane, Is.EqualTo(-1));

            _player.MoveRight();
            _player.MoveRight();
            _player.MoveRight();
            Assert.That(_player.TargetLane, Is.EqualTo(1));
        }

        [Test]
        public void ZeroWorldDt_ChangesNothing()
        {
            _player.MoveRight();
            _player.Jump();

            _player.Advance(0);
            _player.Advance(0);

            Assert.That(_player.X, Is.EqualTo(0));
            Assert.That(_player.IsAirborne, Is.True, "세계가 멈춘 동안 점프 시간이 흐르지 않는다");
        }

        [Test]
        public void Jump_LastsJumpSec_AndClearsLowObstacle()
        {
            _player.Jump();
            _player.Advance(_player.JumpSec * 0.9);

            Assert.That(_player.IsAirborne, Is.True);
            Assert.That(_player.Resolve(Item(TrackItemKind.LowObstacle)), Is.EqualTo(PlayerController.Outcome.Passed));

            _player.Advance(_player.JumpSec * 0.2);
            Assert.That(_player.IsAirborne, Is.False);
            Assert.That(_player.Resolve(Item(TrackItemKind.LowObstacle)), Is.EqualTo(PlayerController.Outcome.Hit));
        }

        [Test]
        public void Slide_ClearsHighObstacle_ButStandingHitsIt()
        {
            Assert.That(_player.Resolve(Item(TrackItemKind.HighObstacle)), Is.EqualTo(PlayerController.Outcome.Hit));

            _player.Slide();
            Assert.That(_player.Resolve(Item(TrackItemKind.HighObstacle)), Is.EqualTo(PlayerController.Outcome.Passed));
        }

        [Test]
        public void LaneBlock_HitsEvenWhenJumping_PassesInOtherLane()
        {
            _player.Jump();
            Assert.That(_player.Resolve(Item(TrackItemKind.LaneBlock)), Is.EqualTo(PlayerController.Outcome.Hit));
            Assert.That(_player.Resolve(Item(TrackItemKind.LaneBlock, 1)), Is.EqualTo(PlayerController.Outcome.Passed));
        }

        [Test]
        public void JumpAndSlide_CancelEachOther()
        {
            _player.Slide();
            _player.Jump();
            Assert.That(_player.IsSliding, Is.False);
            Assert.That(_player.IsAirborne, Is.True);

            _player.Slide();
            Assert.That(_player.IsAirborne, Is.False);
            Assert.That(_player.IsSliding, Is.True);
        }

        [Test]
        public void Hit_SlowsDownForStumbleSec()
        {
            Assert.That(_player.CurrentSpeed, Is.EqualTo(_player.RunSpeed));

            _player.Resolve(Item(TrackItemKind.LaneBlock));
            Assert.That(_player.CurrentSpeed, Is.EqualTo(_player.RunSpeed * _player.StumbleSpeedFactor).Within(Eps));

            _player.Advance(_player.StumbleSec + 0.01);
            Assert.That(_player.CurrentSpeed, Is.EqualTo(_player.RunSpeed));
        }

        [Test]
        public void Snack_IsSentToRunController_AsCommand()
        {
            var outcome = _player.Resolve(Item(TrackItemKind.Snack, 0, "juice"));
            Assert.That(outcome, Is.EqualTo(PlayerController.Outcome.Collected));
            Assert.That(_eaten, Is.Empty, "명령은 다음 Tick에서 처리된다");

            _run.Tick(0.1);

            Assert.That(_eaten.Single().SnackId, Is.EqualTo("juice"));
            Assert.That(_player.IsStumbling, Is.False, "간식은 속도에 영향이 없다");
        }

        [Test]
        public void ItemInOtherLane_OrMarker_Passes()
        {
            Assert.That(_player.Resolve(Item(TrackItemKind.Snack, 1, "juice")), Is.EqualTo(PlayerController.Outcome.Passed));
            Assert.That(_player.Resolve(Item(TrackItemKind.Marker)), Is.EqualTo(PlayerController.Outcome.Passed));
        }

        [Test]
        public void ResetRun_ReturnsToCenterStanding()
        {
            _player.MoveLeft();
            _player.Advance(1);
            _player.Jump();
            _player.Resolve(Item(TrackItemKind.LaneBlock, -1));

            _player.ResetRun();

            Assert.That(_player.X, Is.EqualTo(0));
            Assert.That(_player.TargetLane, Is.EqualTo(0));
            Assert.That(_player.IsAirborne, Is.False);
            Assert.That(_player.IsStumbling, Is.False);
        }
    }

    public class TrackSpawnerTests
    {
        private static readonly string[] Snacks = { "juice", "cracker" };
        private const double SnackInterval = 8;
        private const double Speed = 10;

        private GameObject _go;
        private TrackSpawner _track;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("Track");
            _track = _go.AddComponent<TrackSpawner>();
            _track.Init(SnackInterval, Snacks, 42, false);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_go);

        /// <summary>dt 간격으로 seconds만큼 진행하며 지나간 물체를 모은다.</summary>
        private List<TrackItem> Run(double seconds, bool obstaclesAllowed = true, double dt = 0.1)
        {
            var crossed = new List<TrackItem>();
            int steps = (int)Math.Round(seconds / dt);
            for (int i = 0; i < steps; i++) crossed.AddRange(_track.Advance(dt, Speed, obstaclesAllowed));
            return crossed;
        }

        private static IEnumerable<TrackItem> Obstacles(IEnumerable<TrackItem> items) => items.Where(i => i.IsObstacle);

        [Test]
        public void ZeroWorldDt_MovesNothing()
        {
            var before = _track.Items.Select(i => i.Z).ToList();

            var crossed = _track.Advance(0, Speed, true);

            Assert.That(crossed, Is.Empty);
            Assert.That(_track.Items.Select(i => i.Z), Is.EqualTo(before));
            Assert.That(_track.Distance, Is.EqualTo(0));
        }

        [Test]
        public void StartRunway_IsClear_ThenObstaclesArriveEverySpacing()
        {
            var early = Run((_track.StartClearDistance - 1) / Speed);
            Assert.That(Obstacles(early), Is.Empty, "시작 구간에는 장애물이 없다");

            var later = Run(_track.ObstacleSpacing * 5 / Speed);
            var obstacles = Obstacles(later).ToList();
            Assert.That(obstacles, Has.Count.EqualTo(5).Or.Count.EqualTo(6));
            Assert.That(obstacles.Select(o => o.Lane), Is.All.InRange(-1, 1));
        }

        [Test]
        public void EachObstacleRow_BlocksOnlyOneLane()
        {
            Run(60);

            var rows = _track.Items.Where(i => i.IsObstacle).GroupBy(i => Math.Round(i.Z, 3));
            Assert.That(rows.All(r => r.Count() == 1), Is.True);
        }

        [Test]
        public void Snacks_ComeEveryInterval_FromConfigList()
        {
            // 마지막 간식이 지나갈 때까지 2초 여유를 둔다.
            var crossed = Run(SnackInterval * 10 + _track.SpawnAheadDistance / Speed + 2);

            var snacks = crossed.Where(i => i.Kind == TrackItemKind.Snack).ToList();
            Assert.That(snacks, Has.Count.EqualTo(10));
            Assert.That(snacks.Select(s => s.SnackId).Distinct(), Is.SubsetOf(Snacks));
        }

        [Test]
        public void ObstaclesNotAllowed_RemovesAndStopsObstacles_ButKeepsSnacks()
        {
            Run(5);
            Assert.That(Obstacles(_track.Items), Is.Not.Empty);

            var crossed = Run(SnackInterval * 3, false);

            Assert.That(Obstacles(_track.Items), Is.Empty);
            Assert.That(Obstacles(crossed), Is.Empty);
            Assert.That(_track.Items.Any(i => i.Kind == TrackItemKind.Snack), Is.True);
        }

        [Test]
        public void ObstaclesNotAllowed_WithFrozenWorld_StillClearsObstacles()
        {
            Run(5);

            _track.Advance(0, Speed, false);

            Assert.That(Obstacles(_track.Items), Is.Empty);
        }

        [Test]
        public void SameSeed_GivesSameTrack()
        {
            var first = Run(30).Select(i => (i.Kind, i.Lane, i.SnackId)).ToList();

            _track.ResetRun(42);
            var second = Run(30).Select(i => (i.Kind, i.Lane, i.SnackId)).ToList();

            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void Remove_TakesItemOffTrack()
        {
            var snack = Run(SnackInterval + _track.SpawnAheadDistance / Speed + 1)
                .First(i => i.Kind == TrackItemKind.Snack);

            _track.Remove(snack);

            Assert.That(_track.Items, Has.No.Member(snack));
        }

        [Test]
        public void PassedItems_AreDespawnedBehind()
        {
            Run(30);

            Assert.That(_track.Items.All(i => i.Z >= -10), Is.True);
        }
    }

    public class InputAdapterTests
    {
        [Test]
        public void Classify_UsesDominantAxis_AndIgnoresShortTaps()
        {
            Assert.That(InputAdapter.Classify(new Vector2(3, 2), 10), Is.EqualTo(InputAdapter.Swipe.None));
            Assert.That(InputAdapter.Classify(new Vector2(-50, 10), 10), Is.EqualTo(InputAdapter.Swipe.Left));
            Assert.That(InputAdapter.Classify(new Vector2(50, -10), 10), Is.EqualTo(InputAdapter.Swipe.Right));
            Assert.That(InputAdapter.Classify(new Vector2(5, 40), 10), Is.EqualTo(InputAdapter.Swipe.Up));
            Assert.That(InputAdapter.Classify(new Vector2(-5, -40), 10), Is.EqualTo(InputAdapter.Swipe.Down));
        }
    }
}
