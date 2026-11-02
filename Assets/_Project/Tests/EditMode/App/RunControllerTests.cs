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
                Collect<HyperResolved>();
                Collect<RunEliminated>();
                Collect<TrackSnackEaten>();
                Collect<SectionEntered>();
                Collect<ObstacleHit>();
                Collect<GlucoseRangeEntered>();

                Run = new RunController(config.Runner, config.Sections, () =>
                {
                    var model = new GlucoseModel(config.Glucose);
                    var director = new EventDirector(config.Glucose, model, new NoRandom(), Bus);
                    var fsm = new HypoEventFsm(config, model, director, Bus, () => Run.DistanceM);
                    var flag = new Flag(Bus.Subscribe<HypoEventTriggered>(fsm.Begin));
                    Sessions.Add(flag);
                    return new RunSession(model, director, fsm, flag,
                        Bus.Subscribe<HyperEventTriggered>(fsm.BeginHyper));
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
        public void StartRun_PublishesFirstSectionAndRange()
        {
            var rig = new Rig();
            Assert.That(rig.Run.CurrentSectionId, Is.Null, "주행이 없으면 구간도 없다");

            rig.Run.StartRun();

            var section = rig.All<SectionEntered>().Single();
            Assert.That(section.Section, Is.EqualTo("signal"));
            Assert.That(section.DistanceM, Is.EqualTo(0));
            Assert.That(rig.All<GlucoseRangeEntered>(), Has.Count.EqualTo(1));
            Assert.That(rig.Events.FindIndex(e => e is SectionEntered),
                Is.LessThan(rig.Events.FindIndex(e => e is GlucoseRangeEntered)));
            Assert.That(rig.Run.Hearts, Is.EqualTo(3));
            Assert.That(rig.Run.DistanceM, Is.EqualTo(0));
            Assert.That(rig.Run.CurrentSectionId, Is.EqualTo("signal"));

            rig.Run.Tick(1);
            Assert.That(rig.All<GlucoseRangeEntered>(), Has.Count.EqualTo(1), "범위 안팎이 바뀔 때만 낸다");
        }

        [Test]
        public void ThreeHits_Eliminates()
        {
            var rig = new Rig();
            rig.Run.StartRun();

            for (int i = 0; i < 3; i++)
            {
                rig.Run.HitObstacle();
                rig.Run.Tick(0.1);
            }

            Assert.That(rig.All<ObstacleHit>().Select(h => h.HeartsLeft), Is.EqualTo(new[] { 2, 1, 0 }));
            Assert.That(rig.Run.Hearts, Is.EqualTo(0));
            Assert.That(rig.Run.State, Is.EqualTo(RunState.Eliminated));
            var eliminated = rig.All<RunEliminated>().Single();
            Assert.That(eliminated.Cause, Is.EqualTo(EliminationCause.HeartsLost));
            Assert.That(eliminated.Step, Is.Null);
            Assert.That(eliminated.Reason, Is.EqualTo(HypoErrorCode.None));
        }

        [Test]
        public void HitWhileWorldFrozen_IsIgnored()
        {
            var rig = new Rig().Triggered();
            rig.Run.PressMeter();
            rig.Run.Tick(1);
            Assert.That(rig.Run.IsWorldFrozen, Is.True);

            rig.Run.HitObstacle();
            rig.Run.Tick(1);

            Assert.That(rig.Run.Hearts, Is.EqualTo(3));
            Assert.That(rig.All<ObstacleHit>(), Is.Empty);
        }

        [Test]
        public void ObstacleHit_CarriesDistance()
        {
            var rig = new Rig();
            rig.Run.StartRun();

            rig.Run.AdvanceDistance(7);
            rig.Run.HitObstacle();
            rig.Run.Tick(1);

            var hit = rig.All<ObstacleHit>().Single();
            Assert.That(hit.HeartsLeft, Is.EqualTo(2));
            Assert.That(hit.DistanceM, Is.EqualTo(7));
            Assert.That(hit.SimT, Is.EqualTo(0), "명령은 시간이 흐르기 전에 처리된다");
        }

        [Test]
        public void AdvanceDistance_InvalidMeters_Throws()
        {
            var rig = new Rig();
            rig.Run.StartRun();

            Assert.Throws<ArgumentOutOfRangeException>(() => rig.Run.AdvanceDistance(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => rig.Run.AdvanceDistance(double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => rig.Run.AdvanceDistance(double.PositiveInfinity));
        }

        [Test]
        public void AdvanceDistance_WhileWorldFrozen_IsIgnored()
        {
            var rig = new Rig().Triggered();
            rig.Run.PressMeter();
            rig.Run.Tick(1);
            Assert.That(rig.Run.IsWorldFrozen, Is.True);

            rig.Run.AdvanceDistance(5);
            rig.Run.Tick(1);

            Assert.That(rig.Run.DistanceM, Is.EqualTo(0));
        }

        [Test]
        public void Sections_AdvanceByDistance()
        {
            var rig = new Rig(b => b.Sections = new List<SectionConfig>
            {
                new SectionConfig("signal", 10), new SectionConfig("condition", null),
            });
            rig.Run.StartRun();

            rig.Run.AdvanceDistance(6);
            rig.Run.Tick(1);
            Assert.That(rig.Run.CurrentSectionId, Is.EqualTo("signal"));
            rig.Run.AdvanceDistance(6);
            rig.Run.Tick(1);

            var sections = rig.All<SectionEntered>();
            Assert.That(sections.Select(e => e.Section), Is.EqualTo(new[] { "signal", "condition" }));
            Assert.That(sections[0].DistanceM, Is.EqualTo(0));
            Assert.That(sections[1].DistanceM, Is.EqualTo(12));
            Assert.That(rig.Run.DistanceM, Is.EqualTo(12));
            Assert.That(rig.Run.CurrentSectionId, Is.EqualTo("condition"));
        }

        [Test]
        public void LargeDistance_EntersEachSectionOnce()
        {
            var rig = new Rig(b => b.Sections = new List<SectionConfig>
            {
                new SectionConfig("signal", 10), new SectionConfig("condition", 10), new SectionConfig("complex", null),
            });
            rig.Run.StartRun();

            rig.Run.AdvanceDistance(100);
            rig.Run.Tick(1);

            Assert.That(rig.All<SectionEntered>().Select(e => e.Section),
                Is.EqualTo(new[] { "signal", "condition", "complex" }));
            Assert.That(rig.All<SectionEntered>().Skip(1).Select(e => e.DistanceM), Is.EqualTo(new[] { 100.0, 100.0 }));
            Assert.That(rig.Run.CurrentSectionId, Is.EqualTo("complex"));

            rig.Run.AdvanceDistance(100);
            rig.Run.Tick(1);

            Assert.That(rig.All<SectionEntered>(), Has.Count.EqualTo(3), "마지막 구간에서 멈춘다");
            Assert.That(rig.Run.DistanceM, Is.EqualTo(200));
        }

        /// <summary>같은 Tick에서 하트와 혈당이 함께 탈락 조건이 되면 탈락은 하나만 낸다(명령이 먼저 처리된다).</summary>
        [Test]
        public void HeartsAndGlucoseSameTick_PublishesOneElimination()
        {
            var rig = new Rig(b => { b.Hearts = 1; b.GlucoseLow = 65; });
            rig.Run.StartRun();
            rig.Ticks(27); // 26초에 발동(68), 27초에 66
            Assert.That(rig.Run.Phase, Is.EqualTo(HypoPhase.Recognize), "1단계에서는 세계가 흐른다");
            Assert.That(rig.Run.Actual, Is.EqualTo(66).Within(Eps));
            Assert.That(rig.Run.Actual - 2, Is.LessThan(65), "다음 Tick 감소로 안전 범위를 벗어나는 상황이어야 한다");

            rig.Run.HitObstacle();
            rig.Run.Tick(1);
            rig.Ticks(3);

            Assert.That(rig.Run.State, Is.EqualTo(RunState.Eliminated));
            Assert.That(rig.All<RunEliminated>().Single().Cause, Is.EqualTo(EliminationCause.HeartsLost));
        }

        [Test]
        public void CommandsAfterElimination_AreIgnored()
        {
            var rig = new Rig(b => b.Hearts = 1);
            rig.Run.StartRun();
            rig.Run.AdvanceDistance(3);
            rig.Run.HitObstacle();
            rig.Run.Tick(1);
            Assert.That(rig.Run.State, Is.EqualTo(RunState.Eliminated));
            int eventCount = rig.Events.Count;
            double actual = rig.Run.Actual;

            rig.Run.HitObstacle();
            rig.Run.AdvanceDistance(5);
            rig.Run.PickUpSnack("juice");
            rig.Ticks(3);

            Assert.That(rig.Run.Hearts, Is.EqualTo(0));
            Assert.That(rig.Run.DistanceM, Is.EqualTo(3));
            Assert.That(rig.Run.Actual, Is.EqualTo(actual));
            Assert.That(rig.Events, Has.Count.EqualTo(eventCount), "탈락 뒤에는 새 이벤트가 없다");
        }

        /// <summary>하트·거리가 같은 큐 안에서 탈락 뒤에 오면 버린다.</summary>
        [Test]
        public void CommandsQueuedBehindElimination_AreDropped()
        {
            var rig = new Rig(b => b.Hearts = 1);
            rig.Run.StartRun();

            rig.Run.HitObstacle();
            rig.Run.AdvanceDistance(5);
            rig.Run.HitObstacle();
            rig.Run.Tick(1);

            Assert.That(rig.All<ObstacleHit>(), Has.Count.EqualTo(1));
            Assert.That(rig.Run.DistanceM, Is.EqualTo(0));
            Assert.That(rig.Run.Hearts, Is.EqualTo(0));
        }

        [Test]
        public void Run_DoesNotEndWithoutElimination()
        {
            var rig = new Rig(b => b.RunDecayPerSec = 0);
            rig.Run.StartRun();

            rig.Ticks(1000);

            Assert.That(rig.Run.State, Is.EqualTo(RunState.Running));
            Assert.That(rig.Run.WorldTime, Is.EqualTo(1000).Within(Eps));
        }

        [Test]
        public void EndRun_ClearsSection_AndNewRunResetsHeartsAndDistance()
        {
            var rig = new Rig();
            rig.Run.StartRun();
            rig.Run.AdvanceDistance(4);
            rig.Run.HitObstacle();
            rig.Run.Tick(1);

            rig.Run.EndRun();
            Assert.That(rig.Run.CurrentSectionId, Is.Null);
            rig.Run.StartRun();

            Assert.That(rig.Run.Hearts, Is.EqualTo(3));
            Assert.That(rig.Run.DistanceM, Is.EqualTo(0));
            Assert.That(rig.All<SectionEntered>(), Has.Count.EqualTo(2));
        }

        [Test]
        public void HyperEvent_FreezesWorld_AndCommandsReachHyperStep()
        {
            var rig = new Rig(b => b.HyperThreshold = 125); // 해제 수준 105
            rig.Run.StartRun();
            rig.Run.PickUpSnack("juice");
            rig.Run.PickUpSnack("juice"); // 0.5 × 2 = 1인분(+30)

            rig.Ticks(2); // 118 → 131
            Assert.That(rig.Run.Phase, Is.EqualTo(HypoPhase.HyperNotify));
            Assert.That(rig.Run.IsWorldFrozen, Is.True);

            rig.Ticks(1);
            Assert.That(rig.Run.WorldTime, Is.EqualTo(2).Within(Eps));
            Assert.That(rig.Run.LastWorldDt, Is.EqualTo(0));

            rig.Run.ChooseKeepRunning();
            rig.Ticks(1);
            Assert.That(rig.Run.Phase, Is.EqualTo(HypoPhase.GuardianPrompt));

            rig.Run.PressHelp(HypoPhase.GuardianPrompt);
            rig.Ticks(1); // 같은 Tick에서 카운트다운(1초)도 끝난다
            Assert.That(rig.Run.Phase, Is.EqualTo(HypoPhase.Grace));
            Assert.That(rig.All<HyperResolved>().Single().AllCorrect, Is.False);
            Assert.That(rig.All<StepAnswered>().Single().ErrorCode, Is.EqualTo(HypoErrorCode.HyKeepRunning));
        }
    }
}
