using System;
using System.Collections.Generic;
using System.Linq;
using SugarDash.Domain.Config;
using SugarDash.Domain.Director;
using SugarDash.Domain.Events;
using SugarDash.Domain.Glucose;
using SugarDash.Domain.Hypo;
using SugarDash.Domain.Ports;
using SugarDash.Domain.Tests;
using NUnit.Framework;

namespace SugarDash.App.Tests
{
    public class RunControllerTests
    {
        private const double Eps = 1e-9;

        private sealed class NoRandom : IRandomSource
        {
            public double NextDouble() => throw new InvalidOperationException("급하강 확률 0에서는 난수를 읽지 않아야 한다");
        }

        private sealed class FakeLog : ILogRepository
        {
            public int Begun;
            public int Ended;

            public string BeginRun()
            {
                Begun++;
                return "run-" + Begun;
            }

            public void EndRun() => Ended++;
        }

        private sealed class Flag : IDisposable
        {
            private readonly IDisposable _inner;
            public bool Disposed;

            public Flag(IDisposable inner) => _inner = inner;

            public void Dispose()
            {
                Disposed = true;
                _inner.Dispose();
            }
        }

        /// <summary>
        /// RunController와 Bootstrap 팩토리를 흉내 낸 조립.
        /// 기본 설정: 달리기 감소 2/s, 기본 하강 0. 120에서 26초에 68로 저혈당이 발동한다.
        /// </summary>
        private sealed class Rig
        {
            public readonly DomainEventBus Bus = new DomainEventBus();
            public readonly FakeLog Log = new FakeLog();
            public readonly List<object> Events = new List<object>();
            public readonly List<Flag> Sessions = new List<Flag>();
            public readonly RunController Run;

            public Rig(Action<ConfigBuilder> configure = null)
            {
                var builder = new ConfigBuilder { RunDecayPerSec = 2 };
                configure?.Invoke(builder);
                var config = builder.Build();
                Assert.That(GameConfigValidator.Validate(config), Is.Empty, "테스트 설정도 불변식을 지켜야 한다");

                Collect<HypoEventTriggered>();
                Collect<StepAnswered>();
                Collect<HypoResolved>();
                Collect<RunEliminated>();
                Collect<TrackSnackEaten>();
                Collect<RunCompleted>();

                Run = new RunController(config.Runner, () =>
                {
                    var model = new GlucoseModel(config.Glucose);
                    var director = new EventDirector(config.Glucose, model, new NoRandom(), Bus);
                    var fsm = new HypoEventFsm(config, model, director, Bus);
                    var flag = new Flag(Bus.Subscribe<HypoEventTriggered>(fsm.Begin));
                    Sessions.Add(flag);
                    return new RunSession(model, director, fsm, flag);
                }, Log, Bus);
            }

            private void Collect<T>() => Bus.Subscribe<T>(e => Events.Add(e));

            public IReadOnlyList<T> All<T>() => Events.OfType<T>().ToList();

            public void Ticks(int count, double dt = 1)
            {
                for (int i = 0; i < count; i++) Run.Tick(dt);
            }

            /// <summary>주행을 시작해 저혈당 발동(26초)까지 진행한다.</summary>
            public Rig Triggered()
            {
                Run.StartRun();
                Ticks(26);
                Assert.That(Run.Phase, Is.EqualTo(HypoPhase.Recognize));
                return this;
            }
        }

        [Test]
        public void StartRun_OpensLogAndRuns()
        {
            var rig = new Rig();

            rig.Run.StartRun();

            Assert.That(rig.Run.State, Is.EqualTo(RunState.Running));
            Assert.That(rig.Log.Begun, Is.EqualTo(1));
            Assert.That(rig.Run.Actual, Is.EqualTo(120));
            Assert.Throws<InvalidOperationException>(() => rig.Run.StartRun());
        }

        [Test]
        public void Tick_AdvancesThreeClocksAndRunDecay()
        {
            var rig = new Rig();
            rig.Run.StartRun();

            rig.Ticks(10);

            Assert.That(rig.Run.WorldTime, Is.EqualTo(10).Within(Eps));
            Assert.That(rig.Run.Actual, Is.EqualTo(100).Within(Eps));
        }

        [Test]
        public void LastWorldDt_FollowsWorldClock()
        {
            var rig = new Rig().Triggered();
            Assert.That(rig.Run.LastWorldDt, Is.EqualTo(1), "1단계에서는 세계가 흐른다");

            rig.Run.PressMeter();
            rig.Run.Tick(1);
            Assert.That(rig.Run.LastWorldDt, Is.EqualTo(0), "2단계부터 세계가 멈춘다");

            rig.Run.Pause();
            rig.Run.Tick(1);
            Assert.That(rig.Run.LastWorldDt, Is.EqualTo(0));
        }

        [Test]
        public void RunCount_IncreasesOnEachStart()
        {
            var rig = new Rig();
            Assert.That(rig.Run.RunCount, Is.EqualTo(0));

            rig.Run.StartRun();
            rig.Run.EndRun();
            rig.Run.StartRun();

            Assert.That(rig.Run.RunCount, Is.EqualTo(2));
        }

        [Test]
        public void InvalidDt_Throws()
        {
            var rig = new Rig();
            rig.Run.StartRun();

            Assert.Throws<ArgumentOutOfRangeException>(() => rig.Run.Tick(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => rig.Run.Tick(double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => rig.Run.Tick(double.PositiveInfinity));
        }

        [Test]
        public void Idle_TickAndCommandsDoNothing()
        {
            var rig = new Rig();

            rig.Run.PressMeter();
            rig.Run.Tick(1);
            rig.Run.EndRun();

            Assert.That(rig.Run.State, Is.EqualTo(RunState.Idle));
            Assert.That(rig.Log.Begun, Is.EqualTo(0));
            Assert.That(rig.Log.Ended, Is.EqualTo(0));
        }

        [Test]
        public void Paused_StopsAllClocks_ThenResumeAppliesQueuedCommand()
        {
            var rig = new Rig().Triggered();
            rig.Run.Pause();

            rig.Run.PressMeter();
            rig.Ticks(20);

            Assert.That(rig.Run.State, Is.EqualTo(RunState.Paused));
            Assert.That(rig.Run.WorldTime, Is.EqualTo(26).Within(Eps));
            Assert.That(rig.Run.Actual, Is.EqualTo(68).Within(Eps));
            Assert.That(rig.All<StepAnswered>(), Is.Empty, "일시정지 중에는 판정하지 않는다");

            rig.Run.Resume();
            rig.Run.Tick(1);

            Assert.That(rig.All<StepAnswered>().Single().IsCorrect, Is.True);
            Assert.That(rig.Run.Phase, Is.EqualTo(HypoPhase.Respond));
        }

        [Test]
        public void Command_IsAppliedAtStartOfNextTick_BeforeTimeAdvances()
        {
            var rig = new Rig().Triggered();
            rig.Ticks(4);

            rig.Run.PressMeter();
            Assert.That(rig.Run.Phase, Is.EqualTo(HypoPhase.Recognize), "명령은 Tick 전까지 쌓아 둔다");

            // 이 프레임이 1단계 마감(5초)을 넘기지만, 명령은 시간이 흐르기 전에 처리된다.
            rig.Run.Tick(3);

            var answer = rig.All<StepAnswered>().Single();
            Assert.That(answer.IsCorrect, Is.True);
            Assert.That(answer.DecisionSec, Is.EqualTo(4).Within(Eps));
            Assert.That(rig.Run.Phase, Is.EqualTo(HypoPhase.Respond));
        }

        [Test]
        public void AnswerAfterDeadlineProcessed_IsNotAccepted()
        {
            var rig = new Rig().Triggered();
            rig.Ticks(5);
            Assert.That(rig.Run.Phase, Is.EqualTo(HypoPhase.GuardianPrompt));

            rig.Run.PressMeter();
            rig.Run.Tick(1);

            var answer = rig.All<StepAnswered>().Single();
            Assert.That(answer.ErrorCode, Is.EqualTo(HypoErrorCode.SMissed));
            Assert.That(rig.Run.Phase, Is.EqualTo(HypoPhase.GuardianPrompt));
        }

        [Test]
        public void WorldFrozen_StopsWorldTimeAndRunDecay()
        {
            var rig = new Rig().Triggered();
            rig.Run.PressMeter();
            rig.Run.Tick(1);
            Assert.That(rig.Run.IsWorldFrozen, Is.True);

            rig.Ticks(3);

            Assert.That(rig.Run.WorldTime, Is.EqualTo(26).Within(Eps));
            Assert.That(rig.Run.Actual, Is.EqualTo(68).Within(Eps), "기본 하강 0, 세계 정지 중 달리기 감소 없음");
        }

        [Test]
        public void PickUpSnack_RaisesGlucoseByTrackServings()
        {
            var rig = new Rig();
            rig.Run.StartRun();

            rig.Run.PickUpSnack("juice");
            rig.Ticks(3);

            var eaten = rig.All<TrackSnackEaten>().Single();
            Assert.That(eaten.SnackId, Is.EqualTo("juice"));
            Assert.That(eaten.Servings, Is.EqualTo(0.5));
            Assert.That(eaten.SimT, Is.EqualTo(0));
            // 120 − 달리기 2×3 + juice 30×0.5 (onset 1, absorb 2 → 3초에 전량)
            Assert.That(rig.Run.Actual, Is.EqualTo(129).Within(Eps));
        }

        [Test]
        public void PickUpSnack_DuringRecognize_OnlyAffectsGlucose()
        {
            var rig = new Rig().Triggered();

            rig.Run.PickUpSnack("juice");
            rig.Run.Tick(1);

            Assert.That(rig.All<TrackSnackEaten>(), Has.Count.EqualTo(1));
            Assert.That(rig.All<StepAnswered>(), Is.Empty);
            Assert.That(rig.Run.Phase, Is.EqualTo(HypoPhase.Recognize));
        }

        [Test]
        public void PickUpSnack_WhileWorldFrozen_IsIgnored()
        {
            var rig = new Rig().Triggered();
            rig.Run.PressMeter();
            rig.Run.Tick(1);

            rig.Run.PickUpSnack("juice");
            rig.Run.Tick(1);

            Assert.That(rig.All<TrackSnackEaten>(), Is.Empty);
        }

        [Test]
        public void GuardianPromptTimeout_Eliminates_AndStopsRun()
        {
            var rig = new Rig().Triggered();
            rig.Ticks(10);

            Assert.That(rig.Run.State, Is.EqualTo(RunState.Eliminated));
            Assert.That(rig.All<RunEliminated>().Single().Cause, Is.EqualTo(EliminationCause.PromptTimeout));

            double actual = rig.Run.Actual;
            rig.Run.PressHelp(HypoPhase.GuardianPrompt);
            rig.Ticks(5);
            Assert.That(rig.Run.Actual, Is.EqualTo(actual), "탈락 뒤에는 진행하지 않는다");
            Assert.That(rig.All<RunEliminated>(), Has.Count.EqualTo(1));
        }

        [Test]
        public void RunAgainInGuardianPrompt_Eliminates_AndDropsLaterCommands()
        {
            var rig = new Rig().Triggered();
            rig.Ticks(5);

            rig.Run.ChooseRunAgain(HypoPhase.GuardianPrompt);
            rig.Run.PickUpSnack("juice");
            rig.Run.Tick(1);

            Assert.That(rig.Run.State, Is.EqualTo(RunState.Eliminated));
            Assert.That(rig.All<RunEliminated>().Single().Cause, Is.EqualTo(EliminationCause.Declined));
            Assert.That(rig.All<TrackSnackEaten>(), Is.Empty);
        }

        [Test]
        public void EndRun_DisposesSessionAndClosesLog_ThenNewRunStartsFresh()
        {
            var rig = new Rig().Triggered();
            rig.Ticks(10);

            rig.Run.EndRun();

            Assert.That(rig.Run.State, Is.EqualTo(RunState.Idle));
            Assert.That(rig.Log.Ended, Is.EqualTo(1));
            Assert.That(rig.Sessions[0].Disposed, Is.True);

            rig.Run.StartRun();
            Assert.That(rig.Log.Begun, Is.EqualTo(2));
            Assert.That(rig.Run.Actual, Is.EqualTo(120));
            Assert.That(rig.Run.WorldTime, Is.EqualTo(0));
            Assert.That(rig.Run.Phase, Is.EqualTo(HypoPhase.Idle));
        }

        [Test]
        public void RunDuration_Completes()
        {
            var rig = new Rig(b => { b.RunDecayPerSec = 0; b.RunDurationSec = 10; });
            rig.Run.StartRun();

            rig.Ticks(9);
            Assert.That(rig.Run.State, Is.EqualTo(RunState.Running));
            rig.Run.Tick(1);

            Assert.That(rig.Run.State, Is.EqualTo(RunState.Completed));
            var completed = rig.All<RunCompleted>().Single();
            Assert.That(completed.WorldT, Is.EqualTo(10).Within(Eps));
            Assert.That(completed.SimT, Is.EqualTo(10).Within(Eps));

            rig.Ticks(3);
            Assert.That(rig.All<RunCompleted>(), Has.Count.EqualTo(1));
        }

        [Test]
        public void RunDuration_WaitsUntilHypoEventIsResolved()
        {
            var rig = new Rig(b => b.RunDurationSec = 27).Triggered();
            rig.Run.Tick(1);
            Assert.That(rig.Run.WorldTime, Is.EqualTo(27).Within(Eps));
            Assert.That(rig.Run.State, Is.EqualTo(RunState.Running), "1단계 중에는 끝내지 않는다");

            rig.Run.PressMeter();
            rig.Run.Tick(1);
            rig.Run.ChooseSnack("juice");
            rig.Run.Tick(1);
            rig.Run.ChooseDose(DoseLevel.Proper);
            rig.Run.Tick(1);
            rig.Run.ChooseRecheck();
            rig.Run.Tick(1);
            rig.Run.PressHelp(HypoPhase.Notify);
            rig.Run.Tick(1); // 재개 카운트다운 1초 → 유예 시간

            Assert.That(rig.Run.State, Is.EqualTo(RunState.Completed));
            Assert.That(rig.All<HypoResolved>().Single().AllCorrect, Is.True);
            int resolved = rig.Events.FindIndex(e => e is HypoResolved);
            int completed = rig.Events.FindIndex(e => e is RunCompleted);
            Assert.That(completed, Is.GreaterThan(resolved));
        }
    }
}
