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
            public string BeginRun(string configVersion, SugarDash.Domain.Config.TrackType trackType) => "run";
            public void BeginUnrecordedRun() { }
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
            _run = new RunController(_ =>
            {
                var model = new GlucoseModel(config.Glucose);
                var director = new EventDirector(config.Glucose, model, new NoRandom(), bus);
                var fsm = new HypoEventFsm(config, model, director, bus, () => _run.DistanceM);
                var scoring = new SugarDash.Domain.Scoring.ScoringService(config.Score);
                return new RunSession(config, model, director, fsm, scoring, bus.Subscribe<HypoEventTriggered>(fsm.Begin),
                    scoring.Subscribe(bus));
            }, new NullLog(), bus);
            _run.StartRun(SugarDash.Domain.Config.TrackType.Type1);

            _go = new GameObject("Player");
            _player = _go.AddComponent<PlayerController>();
            _player.Init(_run, false);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_go);

        private RunController StartType2Run()
        {
            var config = new ConfigBuilder().BuildType2();
            var bus = new DomainEventBus();
            RunController run = null;
            run = new RunController(_ =>
            {
                var model = new GlucoseModel(config.Glucose);
                var director = new EventDirector(config.Glucose, model, new NoRandom(), bus, config.Type2);
                var fsm = new HypoEventFsm(config, model, director, bus, () => run.DistanceM);
                var scoring = new SugarDash.Domain.Scoring.ScoringService(config.Score);
                return new RunSession(config, model, director, fsm, scoring, scoring.Subscribe(bus),
                    bus.Subscribe<LowEventTriggered>(fsm.BeginLow), bus.Subscribe<HyperEventTriggered>(fsm.BeginHyper));
            }, new NullLog(), bus);
            run.StartRun(SugarDash.Domain.Config.TrackType.Type2);
            return run;
        }

        [Test]
        public void RestSpot_SameLane_StartsRest_ReturnsRested()
        {
            var run = StartType2Run();
            _player.Init(run, false);

            Assert.That(_player.Resolve(Item(TrackItemKind.RestSpot, 1)), Is.EqualTo(PlayerController.Outcome.Passed));
            Assert.That(_player.Resolve(Item(TrackItemKind.RestSpot, 0)), Is.EqualTo(PlayerController.Outcome.Rested));
            run.Tick(0.1);

            Assert.That(run.IsResting, Is.True);
        }

        [Test]
        public void Resting_SpeedZero_IgnoresInput()
        {
            var run = StartType2Run();
            _player.Init(run, false);
            _player.Resolve(Item(TrackItemKind.RestSpot, 0));
            run.Tick(0.1);
            Assert.That(run.IsResting, Is.True);

            _player.MoveLeft();
            _player.MoveRight();
            _player.Jump();
            _player.Slide();
            float speedBefore = _player.CurrentSpeed;
            _player.Advance(0.5);

            Assert.That(_player.CurrentSpeed, Is.EqualTo(0f));
            Assert.That(speedBefore, Is.EqualTo(0f));
            Assert.That(_player.TargetLane, Is.EqualTo(0));
            Assert.That(_player.X, Is.EqualTo(0f));
            Assert.That(_player.IsAirborne, Is.False);
            Assert.That(_player.IsSliding, Is.False);
        }

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
            Assert.That(_player.CurrentSpeed,
                Is.EqualTo(_player.RunSpeed + _player.SpeedGainPerSec * (_player.StumbleSec + 0.01f)).Within(Eps));
        }

        [Test]
        public void Speed_IncreasesAndCaps()
        {
            _player.Advance(10);
            Assert.That(_player.CurrentSpeed, Is.EqualTo(_player.RunSpeed + _player.SpeedGainPerSec * 10).Within(Eps));

            _player.Advance(1000);
            Assert.That(_player.CurrentSpeed, Is.EqualTo(_player.MaxRunSpeed).Within(Eps));
        }

        [Test]
        public void ResetRun_RestoresSpeed()
        {
            _player.Advance(10);
            _player.ResetRun();
            Assert.That(_player.CurrentSpeed, Is.EqualTo(_player.RunSpeed));
        }

        [Test]
        public void Hit_EliminatesRun()
        {
            _player.Resolve(Item(TrackItemKind.LaneBlock));

            _run.Tick(0.01);

            Assert.That(_run.State, Is.EqualTo(RunState.Eliminated));
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

        private static TrackLayout Type1(double min, double max) => new TrackLayout(Snacks, min, max, false, 0, 0);

        private static TrackLayout Type2(double foodSec = 3, double restSec = 12) =>
            new TrackLayout(new[] { "juice", "water", "cracker" }, foodSec, foodSec, true, restSec, restSec);

        private static TrackSpawner NewType2(GameObject go, int seed = 7, double foodSec = 3, double restSec = 12)
        {
            var track = go.AddComponent<TrackSpawner>();
            track.Init(Type2(foodSec, restSec), seed, false);
            return track;
        }

        [Test]
        public void Type1Layout_Unchanged()
        {
            int snacks = 0;
            for (int i = 0; i < 400; i++)
            {
                var crossed = _track.Advance(0.1, Speed, true);
                Assert.That(crossed.Any(x => x.Kind == TrackItemKind.RestSpot), Is.False);
                Assert.That(crossed.Count(x => x.Kind == TrackItemKind.Snack), Is.LessThanOrEqualTo(1), "1형은 한 번에 간식 한 개");
                snacks += crossed.Count(x => x.Kind == TrackItemKind.Snack);
            }
            Assert.That(snacks, Is.GreaterThan(0));
            Assert.That(_track.Items.Any(x => x.Kind == TrackItemKind.RestSpot), Is.False);
        }

        [Test]
        public void Type2Layout_FoodRowsHaveTwoDistinctFoodsAndOneEmptyLane()
        {
            var go = new GameObject("T2");
            try
            {
                var track = NewType2(go, 7, 3, 0);
                var rows = new Dictionary<TrackItem, int>();
                var all = new List<TrackItem>();
                var seen = new HashSet<TrackItem>();
                for (int i = 0; i < 600; i++)
                {
                    track.Advance(0.1, Speed, true);
                    foreach (var item in track.Items.Where(x => x.Kind == TrackItemKind.Snack && seen.Add(x))) all.Add(item);
                }
                // 같은 줄에서 같이 놓인 것끼리 묶는다(생성 시 Z가 같다 = 같은 프레임 같은 거리).
                Assert.That(all.Count, Is.GreaterThanOrEqualTo(8));
                Assert.That(all.Count % 2, Is.EqualTo(0));
                for (int i = 0; i < all.Count; i += 2)
                {
                    var a = all[i];
                    var b = all[i + 1];
                    Assert.That(a.SnackId, Is.Not.EqualTo(b.SnackId));
                    Assert.That(a.Lane, Is.Not.EqualTo(b.Lane));
                    Assert.That(a.Z, Is.EqualTo(b.Z).Within(1e-9));
                }
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Type2Layout_RestSpotHasClearZone()
        {
            var go = new GameObject("T2");
            try
            {
                var track = NewType2(go, 11, 2, 12);
                const double clear = 8;
                int restCount = 0;
                var checkedSpots = new HashSet<TrackItem>();
                for (int i = 0; i < 1500; i++)
                {
                    track.Advance(0.1, Speed, true);
                    foreach (var rest in track.Items.Where(x => x.Kind == TrackItemKind.RestSpot))
                    {
                        // 놓인 직후(맨 앞)에만 앞쪽 검사가 끝나지 않으므로 화면 안으로 완전히 들어온 뒤 검사한다.
                        if (rest.Z > track.SpawnAheadDistance - 1) continue;
                        if (!checkedSpots.Add(rest)) continue;
                        restCount++;
                        foreach (var other in track.Items.Where(x => x.IsObstacle || x.Kind == TrackItemKind.Snack))
                            Assert.That(Math.Abs(other.Z - rest.Z), Is.GreaterThanOrEqualTo(clear),
                                $"{other.Kind} z={other.Z} rest z={rest.Z}");
                    }
                }
                Assert.That(restCount, Is.GreaterThanOrEqualTo(3));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void SpawnTimersPauseWhileSpeedZero()
        {
            var go = new GameObject("T2");
            try
            {
                var track = NewType2(go, 5, 2, 3);
                int before = track.Items.Count;
                for (int i = 0; i < 300; i++) track.Advance(0.1, 0, true);
                Assert.That(track.Items.Count, Is.EqualTo(before), "속도 0이면 아무것도 새로 놓이지 않는다");
                Assert.That(track.Distance, Is.EqualTo(0));

                for (int i = 0; i < 40; i++) track.Advance(0.1, Speed, true);
                Assert.That(track.Items.Any(x => x.Kind == TrackItemKind.Snack), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void SetLayout_AppliesFromNextResetRun()
        {
            var go = new GameObject("T2");
            try
            {
                var track = go.AddComponent<TrackSpawner>();
                track.Init(Type1(2, 2), 3, false);
                track.SetLayout(Type2(2, 3));
                for (int i = 0; i < 100; i++) track.Advance(0.1, Speed, true);
                Assert.That(track.Items.Any(x => x.Kind == TrackItemKind.RestSpot), Is.False);

                track.ResetRun(3);
                for (int i = 0; i < 100; i++) track.Advance(0.1, Speed, true);
                Assert.That(track.Items.Any(x => x.Kind == TrackItemKind.RestSpot), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Tutorial_GuidedPlacesOnlyTargets()
        {
            var go = new GameObject("T2Guided");
            try
            {
                var track = NewType2(go, 7, 1, 2);
                track.SetTutorialGuided(true);
                Assert.That(track.NearestTutorialTargetZ, Is.Null);
                for (int i = 0; i < 600; i++) track.Advance(0.1, Speed, true);
                Assert.That(track.Items.Count(x => x.IsObstacle || x.Kind == TrackItemKind.Snack || x.Kind == TrackItemKind.RestSpot),
                    Is.EqualTo(0));
                Assert.That(track.Items.Any(x => x.Kind == TrackItemKind.Marker), Is.True, "바닥 표시는 놓는다");

                track.PlaceTutorial(new TutorialPlacement(TutorialObject.LowObstacle, new[] { -1, 0, 1 }));
                var targets = track.Items.Where(x => x.IsTutorialTarget).ToList();
                Assert.That(targets.Count, Is.EqualTo(3));
                Assert.That(targets.All(x => x.Kind == TrackItemKind.LowObstacle), Is.True);
                Assert.That(targets.Select(x => x.Lane).OrderBy(l => l), Is.EqualTo(new[] { -1, 0, 1 }));
                Assert.That(targets.Select(x => x.Z).Distinct().Count(), Is.EqualTo(1));
                Assert.That(track.NearestTutorialTargetZ, Is.EqualTo(targets[0].Z).Within(1e-9));
                Assert.That(targets[0].Z, Is.EqualTo(track.SpawnAheadDistance).Within(1e-6));

                track.SetTutorialGuided(false);
                for (int i = 0; i < 600; i++) track.Advance(0.1, Speed, true);
                Assert.That(track.Items.Any(x => x.IsObstacle || x.Kind == TrackItemKind.Snack || x.Kind == TrackItemKind.RestSpot),
                    Is.True, "guided를 끄면 다시 무작위로 놓는다");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Tutorial_FoodRowAndSnackIds()
        {
            var go = new GameObject("T2Food");
            try
            {
                var track = NewType2(go, 7, 3, 0);
                track.SetTutorialGuided(true);
                track.PlaceTutorial(new TutorialPlacement(TutorialObject.FoodRow, new int[0]));
                var row = track.Items.Where(x => x.IsTutorialTarget).ToList();
                Assert.That(row.Count, Is.EqualTo(2));
                Assert.That(row.All(x => x.Kind == TrackItemKind.Snack), Is.True);
                Assert.That(row[0].SnackId, Is.Not.EqualTo(row[1].SnackId));
                Assert.That(row[0].Lane, Is.Not.EqualTo(row[1].Lane));

                track.PlaceTutorial(new TutorialPlacement(TutorialObject.RestSpot, new[] { 1 }));
                var rest = track.Items.Single(x => x.Kind == TrackItemKind.RestSpot);
                Assert.That(rest.Lane, Is.EqualTo(1));
                Assert.That(rest.IsTutorialTarget, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Tutorial_SnackUsesFirstSnackId()
        {
            _track.SetTutorialGuided(true);
            _track.PlaceTutorial(new TutorialPlacement(TutorialObject.Snack, new[] { 0 }));
            var snack = _track.Items.Single(x => x.IsTutorialTarget);
            Assert.That(snack.Kind, Is.EqualTo(TrackItemKind.Snack));
            Assert.That(snack.SnackId, Is.EqualTo(Snacks[0]));
            Assert.That(snack.Lane, Is.EqualTo(0));
        }

        private GameObject _go;
        private TrackSpawner _track;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("Track");
            _track = _go.AddComponent<TrackSpawner>();
            _track.Init(Type1(SnackInterval, SnackInterval), 42, false);
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

        private static TrackLayout NoSpawns() =>
            new TrackLayout(new[] { "juice", "water", "cracker" }, 1, 1, true, 2, 2, spawnsItems: false);

        [Test]
        public void ChangeLayout_NoSpawns_PlacesNoObstaclesSnacksOrRests()
        {
            var before = new HashSet<TrackItem>(_track.Items);

            _track.ChangeLayout(NoSpawns());
            Run(20);

            var added = _track.Items.Where(i => !before.Contains(i)).ToList();
            Assert.That(added, Is.Not.Empty, "마커는 계속 놓는다");
            Assert.That(added.Select(i => i.Kind), Is.All.EqualTo(TrackItemKind.Marker));
        }

        [Test]
        public void ChangeLayout_NoSpawns_RemovesItemsAhead()
        {
            Run(10);
            Assert.That(_track.Items.Any(x => x.Kind != TrackItemKind.Marker && x.Z > 0), Is.True);

            _track.ChangeLayout(NoSpawns());

            Assert.That(_track.Items.Where(x => x.Z > 0).Select(x => x.Kind), Is.All.EqualTo(TrackItemKind.Marker),
                "물체를 놓지 않는 장면에는 앞 장면의 물체도 남기지 않는다");
        }

        [Test]
        public void ResumeSpawns_PlacesNothingNearerThanSpawnAhead()
        {
            _track.ChangeLayout(NoSpawns());
            Run(20);
            _track.ChangeLayout(Type1(0.5, 0.5));

            var seen = new HashSet<TrackItem>(_track.Items);
            const double dt = 0.1;
            double move = Speed * dt;
            int placed = 0;
            for (int i = 0; i < 30; i++)
            {
                _track.Advance(dt, Speed, true);
                foreach (var item in _track.Items.Where(x => seen.Add(x) && x.Kind != TrackItemKind.Marker))
                {
                    Assert.That(item.Z, Is.GreaterThanOrEqualTo(_track.SpawnAheadDistance - move), item.Kind.ToString());
                    placed++;
                }
            }
            Assert.That(placed, Is.GreaterThan(0), "배치가 돌아온다");
            Assert.That(_track.Items.Where(x => x.Kind != TrackItemKind.Marker).Select(x => x.Z),
                Is.All.GreaterThanOrEqualTo(_track.SpawnAheadDistance - move * 30), "집 장면 전 물체는 모두 지나갔다");
        }

        [Test]
        public void ChangeLayout_AppliesNewSnackInterval_Immediately()
        {
            var go = new GameObject("Slow");
            try
            {
                var track = go.AddComponent<TrackSpawner>();
                track.Init(Type1(100, 100), 5, false);
                for (int i = 0; i < 10; i++) track.Advance(0.1, Speed, true);
                Assert.That(track.Items.Any(x => x.Kind == TrackItemKind.Snack), Is.False);

                track.ChangeLayout(Type1(1, 1));
                for (int i = 0; i < 20; i++) track.Advance(0.1, Speed, true);

                Assert.That(track.Items.Any(x => x.Kind == TrackItemKind.Snack), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void ChangeLayout_KeepsExistingItems()
        {
            Run(10);
            var before = _track.Items.ToList();
            Assert.That(before.Any(x => x.Kind != TrackItemKind.Marker), Is.True);

            _track.ChangeLayout(Type2());

            Assert.That(_track.Items, Is.EqualTo(before));
        }

        [Test]
        public void ChangeLayout_Invalid_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => _track.ChangeLayout(null));
            Assert.Throws<ArgumentOutOfRangeException>(() => _track.ChangeLayout(Type1(0, 1)));
        }

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

        /// <summary>새 스포너로 seconds 동안 dt씩 진행하며 간식이 놓인 worldDt 누적 시각을 모은다.</summary>
        private static List<double> SnackSpawnTimes(double min, double max, int seed, double seconds, double dt)
        {
            var go = new GameObject("RandomTrack");
            try
            {
                var track = go.AddComponent<TrackSpawner>();
                track.Init(Type1(min, max), seed, false);
                var seen = new HashSet<TrackItem>(track.Items.Where(i => i.Kind == TrackItemKind.Snack));
                var times = new List<double>();
                int steps = (int)Math.Round(seconds / dt);
                for (int i = 1; i <= steps; i++)
                {
                    track.Advance(dt, Speed, true);
                    foreach (var item in track.Items.Where(x => x.Kind == TrackItemKind.Snack && seen.Add(x)))
                        times.Add(i * dt);
                }
                return times;
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void SnackInterval_StaysWithinRange()
        {
            var times = SnackSpawnTimes(4, 6, 42, 300, 0.1);

            Assert.That(times.Count, Is.GreaterThan(40));
            var gaps = times.Zip(times.Skip(1), (a, b) => b - a).ToList();
            // 한 프레임(0.1초) 단위로 감지하므로 그만큼의 오차를 허용한다.
            Assert.That(gaps, Is.All.InRange(4 - 0.15, 6 + 0.15));
            Assert.That(gaps.Max() - gaps.Min(), Is.GreaterThan(0.5), "간격이 무작위로 달라져야 한다");
        }

        [Test]
        public void SnackInterval_SameSeed_SameSequence()
        {
            var a = SnackSpawnTimes(4, 6, 7, 120, 0.1);
            var b = SnackSpawnTimes(4, 6, 7, 120, 0.1);

            Assert.That(a, Is.Not.Empty);
            Assert.That(a, Is.EqualTo(b));
        }

        [Test]
        public void SnackInterval_InvalidRange_Throws()
        {
            var go = new GameObject("BadTrack");
            try
            {
                var track = go.AddComponent<TrackSpawner>();
                Assert.Throws<ArgumentOutOfRangeException>(() => track.Init(Type1(0, 5), 1, false));
                Assert.Throws<ArgumentOutOfRangeException>(() => track.Init(Type1(6, 5), 1, false));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
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
