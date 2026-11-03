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
            public string LastVersion;
            public SugarDash.Domain.Config.TrackType? LastType;

            public string BeginRun(string configVersion, SugarDash.Domain.Config.TrackType trackType)
            {
                Begun++;
                LastVersion = configVersion;
                LastType = trackType;
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
                Collect<GlucoseRangeEntered>();

                Run = new RunController(_ =>
                {
                    var model = new GlucoseModel(config.Glucose);
                    var director = new EventDirector(config.Glucose, model, new NoRandom(), Bus);
                    var fsm = new HypoEventFsm(config, model, director, Bus, () => Run.DistanceM);
                    var flag = new Flag(Bus.Subscribe<HypoEventTriggered>(fsm.Begin));
                    Sessions.Add(flag);
                    var scoring = new SugarDash.Domain.Scoring.ScoringService(config.Score);
                    return new RunSession(config, model, director, fsm, scoring, flag, scoring.Subscribe(Bus),
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
                Run.StartRun(SugarDash.Domain.Config.TrackType.Type1);
                Ticks(26);
                Assert.That(Run.Phase, Is.EqualTo(HypoPhase.Recognize));
                return this;
            }
        }

        [Test]
        public void StartRun_OpensLogAndRuns()
        {
            var rig = new Rig();

            rig.Run.StartRun(SugarDash.Domain.Config.TrackType.Type1);

            Assert.That(rig.Run.State, Is.EqualTo(RunState.Running));
            Assert.That(rig.Log.Begun, Is.EqualTo(1));
            Assert.That(rig.Run.Actual, Is.EqualTo(120));
            Assert.Throws<InvalidOperationException>(() => rig.Run.StartRun(SugarDash.Domain.Config.TrackType.Type1));
        }

        [Test]
        public void Score_GrowsWhileInRangeAndRunning()
        {
            var rig = new Rig(b => { b.Initial = 120; b.RunDecayPerSec = 0; b.BaseDecayPerSec = 0; });
            rig.Run.StartRun(SugarDash.Domain.Config.TrackType.Type1);
            for (int i = 0; i < 5; i++)
            {
                rig.Run.AdvanceDistance(10);
                rig.Run.Tick(1);
            }
            Assert.That(rig.Run.Score, Is.EqualTo(5 * 1 + 50 * 0.1).Within(Eps));
        }

        [Test]
        public void Tick_AdvancesThreeClocksAndRunDecay()
        {
            var rig = new Rig();
            rig.Run.StartRun(SugarDash.Domain.Config.TrackType.Type1);

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

            rig.Run.StartRun(SugarDash.Domain.Config.TrackType.Type1);
            rig.Run.EndRun();
            rig.Run.StartRun(SugarDash.Domain.Config.TrackType.Type1);

            Assert.That(rig.Run.RunCount, Is.EqualTo(2));
        }

        [Test]
        public void InvalidDt_Throws()
        {
            var rig = new Rig();
            rig.Run.StartRun(SugarDash.Domain.Config.TrackType.Type1);

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
            rig.Run.StartRun(SugarDash.Domain.Config.TrackType.Type1);

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

            rig.Run.StartRun(SugarDash.Domain.Config.TrackType.Type1);
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

            rig.Run.StartRun(SugarDash.Domain.Config.TrackType.Type1);

            var section = rig.All<SectionEntered>().Single();
            Assert.That(section.Section, Is.EqualTo("signal"));
            Assert.That(section.DistanceM, Is.EqualTo(0));
            Assert.That(rig.All<GlucoseRangeEntered>(), Has.Count.EqualTo(1));
            Assert.That(rig.Events.FindIndex(e => e is SectionEntered),
                Is.LessThan(rig.Events.FindIndex(e => e is GlucoseRangeEntered)));
            Assert.That(rig.Run.DistanceM, Is.EqualTo(0));
            Assert.That(rig.Run.CurrentSectionId, Is.EqualTo("signal"));

            rig.Run.Tick(1);
            Assert.That(rig.All<GlucoseRangeEntered>(), Has.Count.EqualTo(1), "범위 안팎이 바뀔 때만 낸다");
        }

        [Test]
        public void OneHit_Eliminates()
        {
            var rig = new Rig();
            rig.Run.StartRun(SugarDash.Domain.Config.TrackType.Type1);

            rig.Run.AdvanceDistance(7);
            rig.Run.HitObstacle();
            rig.Run.Tick(0.1);

            Assert.That(rig.Run.State, Is.EqualTo(RunState.Eliminated));
            var eliminated = rig.All<RunEliminated>().Single();
            Assert.That(eliminated.Cause, Is.EqualTo(EliminationCause.ObstacleHit));
            Assert.That(eliminated.DistanceM, Is.EqualTo(7));
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

            Assert.That(rig.Run.State, Is.EqualTo(RunState.Running));
            Assert.That(rig.All<RunEliminated>(), Is.Empty);
        }

        [Test]
        public void AdvanceDistance_InvalidMeters_Throws()
        {
            var rig = new Rig();
            rig.Run.StartRun(SugarDash.Domain.Config.TrackType.Type1);

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
            rig.Run.StartRun(SugarDash.Domain.Config.TrackType.Type1);

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
            rig.Run.StartRun(SugarDash.Domain.Config.TrackType.Type1);

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

        /// <summary>같은 Tick에서 장애물과 1단계 시간 초과 혈당 탈락이 함께 탈락 조건이 되면 탈락은 하나만 낸다(명령이 먼저 처리된다).</summary>
        [Test]
        public void ObstacleAndGlucoseSameTick_PublishesOneElimination()
        {
            var rig = new Rig(b => b.GlucoseLow = 65);
            rig.Run.StartRun(SugarDash.Domain.Config.TrackType.Type1);
            rig.Ticks(30); // 26초에 발동(68), 30초에 60: 아직 1단계 제한 시간 안
            Assert.That(rig.Run.Phase, Is.EqualTo(HypoPhase.Recognize), "1단계에서는 세계가 흐른다");
            Assert.That(rig.Run.Actual, Is.LessThan(65), "기준 아래여도 제한 시간 안에서는 탈락하지 않는다");
            Assert.That(rig.All<RunEliminated>(), Is.Empty);

            rig.Run.HitObstacle();
            rig.Run.Tick(1);
            rig.Ticks(3);

            Assert.That(rig.Run.State, Is.EqualTo(RunState.Eliminated));
            Assert.That(rig.All<RunEliminated>().Single().Cause, Is.EqualTo(EliminationCause.ObstacleHit));

            // 대조: 부딪히지 않으면 같은 Tick에서 혈당 탈락이 난다(두 원인이 모두 살아 있었다).
            var control = new Rig(b => b.GlucoseLow = 65);
            control.Run.StartRun(SugarDash.Domain.Config.TrackType.Type1);
            control.Ticks(30);
            control.Run.Tick(1); // 31초: 1단계 시간 초과
            Assert.That(control.All<RunEliminated>().Single().Cause, Is.EqualTo(EliminationCause.GlucoseLow));
        }

        [Test]
        public void CommandsAfterElimination_AreIgnored()
        {
            var rig = new Rig();
            rig.Run.StartRun(SugarDash.Domain.Config.TrackType.Type1);
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

            Assert.That(rig.Run.DistanceM, Is.EqualTo(3));
            Assert.That(rig.Run.Actual, Is.EqualTo(actual));
            Assert.That(rig.Events, Has.Count.EqualTo(eventCount), "탈락 뒤에는 새 이벤트가 없다");
        }

        /// <summary>장애물·거리가 같은 큐 안에서 탈락 뒤에 오면 버린다.</summary>
        [Test]
        public void CommandsQueuedBehindElimination_AreDropped()
        {
            var rig = new Rig();
            rig.Run.StartRun(SugarDash.Domain.Config.TrackType.Type1);

            rig.Run.HitObstacle();
            rig.Run.AdvanceDistance(5);
            rig.Run.HitObstacle();
            rig.Run.Tick(1);

            Assert.That(rig.All<RunEliminated>(), Has.Count.EqualTo(1));
            Assert.That(rig.Run.DistanceM, Is.EqualTo(0));
        }

        [Test]
        public void Run_DoesNotEndWithoutElimination()
        {
            var rig = new Rig(b => b.RunDecayPerSec = 0);
            rig.Run.StartRun(SugarDash.Domain.Config.TrackType.Type1);

            rig.Ticks(1000);

            Assert.That(rig.Run.State, Is.EqualTo(RunState.Running));
            Assert.That(rig.Run.WorldTime, Is.EqualTo(1000).Within(Eps));
        }

        [Test]
        public void EndRun_ClearsSection_AndNewRunResetsDistance()
        {
            var rig = new Rig();
            rig.Run.StartRun(SugarDash.Domain.Config.TrackType.Type1);
            rig.Run.AdvanceDistance(4);
            rig.Run.HitObstacle();
            rig.Run.Tick(1);

            rig.Run.EndRun();
            Assert.That(rig.Run.CurrentSectionId, Is.Null);
            rig.Run.StartRun(SugarDash.Domain.Config.TrackType.Type1);

            Assert.That(rig.Run.DistanceM, Is.EqualTo(0));
            Assert.That(rig.All<SectionEntered>(), Has.Count.EqualTo(2));
        }

        [Test]
        public void HyperEvent_FreezesWorld_AndCommandsReachHyperStep()
        {
            var rig = new Rig(b => b.HyperThreshold = 125); // 해제 수준 105
            rig.Run.StartRun(SugarDash.Domain.Config.TrackType.Type1);
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

        [Test]
        public void AnswerFeedbackAndCountdown_AreZeroWithoutSession()
        {
            var rig = new Rig(b => b.AnswerFeedbackSec = 1);

            Assert.That(rig.Run.IsShowingAnswerFeedback, Is.False);
            Assert.That(rig.Run.ResumeCountdownRemaining, Is.EqualTo(0));
        }

        [Test]
        public void AnswerFeedback_IgnoresCommandsUntilWindowEnds()
        {
            var rig = new Rig(b => b.AnswerFeedbackSec = 1).Triggered();
            rig.Run.PressMeter();
            rig.Run.Tick(0.5);
            Assert.That(rig.Run.Phase, Is.EqualTo(HypoPhase.Respond));
            Assert.That(rig.Run.IsShowingAnswerFeedback, Is.True);
            Assert.That(rig.Run.IsWorldFrozen, Is.True);

            rig.Run.ChooseSnack("juice");
            rig.Run.Tick(0.5);
            Assert.That(rig.Run.Phase, Is.EqualTo(HypoPhase.Respond), "문구 창 중에 고른 것은 버린다");
            Assert.That(rig.Run.IsShowingAnswerFeedback, Is.False);

            rig.Run.ChooseSnack("juice");
            rig.Run.Tick(0.1);
            Assert.That(rig.Run.Phase, Is.EqualTo(HypoPhase.Dose));
            Assert.That(rig.All<StepAnswered>().Last().DecisionSec, Is.EqualTo(0).Within(Eps));
        }

        [Test]
        public void ResumeCountdownRemaining_IsExposed()
        {
            var rig = new Rig(b => { b.AnswerFeedbackSec = 1; b.ResumeCountdownSec = 3; }).Triggered();
            void Step(Action command)
            {
                command();
                rig.Run.Tick(1); // 명령 처리 뒤 문구 창 1초
            }

            Step(rig.Run.PressMeter);
            Step(() => rig.Run.ChooseSnack("juice"));
            Step(() => rig.Run.ChooseDose(DoseLevel.Proper));
            Step(rig.Run.ChooseRecheck);
            Step(() => rig.Run.PressHelp(HypoPhase.Notify));
            Assert.That(rig.Run.Phase, Is.EqualTo(HypoPhase.ResumeCountdown));
            Assert.That(rig.Run.ResumeCountdownRemaining, Is.EqualTo(3).Within(Eps));

            rig.Run.Tick(1);
            Assert.That(rig.Run.ResumeCountdownRemaining, Is.EqualTo(2).Within(Eps));
        }

        /// <summary>2형 실제 설정(BuildType2)으로 세션을 만드는 조립. RunController.Tick까지 끝까지 도는지 확인하는 스모크이기도 하다.</summary>
        private sealed class Rig2
        {
            public readonly DomainEventBus Bus = new DomainEventBus();
            public readonly FakeLog Log = new FakeLog();
            public readonly List<object> Events = new List<object>();
            public readonly RunController Run;
            public readonly GameConfig Config;

            public Rig2(Action<ConfigBuilder> configure = null)
            {
                var builder = new ConfigBuilder { RunDecayPerSec = 2, BaseDecayPerSec = 0 };
                configure?.Invoke(builder);
                Config = builder.BuildType2();
                Assert.That(GameConfigValidator.Validate(Config), Is.Empty, "테스트 설정도 불변식을 지켜야 한다");

                Collect<FoodEaten>();
                Collect<TrackSnackEaten>();
                Collect<RestStarted>();
                Collect<RestEnded>();
                Collect<StepAnswered>();
                Collect<HyperEventTriggered>();
                Collect<LowEventTriggered>();

                Run = new RunController(type =>
                {
                    var config = Config;
                    var model = new GlucoseModel(config.Glucose);
                    var director = new EventDirector(config.Glucose, model, new NoRandom(), Bus, config.Type2);
                    var fsm = new HypoEventFsm(config, model, director, Bus, () => Run.DistanceM);
                    var scoring = new SugarDash.Domain.Scoring.ScoringService(config.Score);
                    return new RunSession(config, model, director, fsm, scoring, scoring.Subscribe(Bus),
                        Bus.Subscribe<LowEventTriggered>(fsm.BeginLow),
                        Bus.Subscribe<HyperEventTriggered>(fsm.BeginHyper));
                }, Log, Bus);
            }

            private void Collect<T>() => Bus.Subscribe<T>(e => Events.Add(e));

            public IReadOnlyList<T> All<T>() => Events.OfType<T>().ToList();

            public void Ticks(int count, double dt = 1)
            {
                for (int i = 0; i < count; i++) Run.Tick(dt);
            }

            /// <summary>높은 혈당이 유지되어 고혈당 tell이 시작될 때까지 진행한다.</summary>
            public void RunToHyperTell()
            {
                for (int i = 0; i < 20 && Run.Phase != HypoPhase.HyperNotify; i++) Run.Tick(1);
                Assert.That(Run.Phase, Is.EqualTo(HypoPhase.HyperNotify));
            }
        }

        private static void HighStart(ConfigBuilder b)
        {
            b.HyperThreshold = 125; // 주스 1인분(+30)을 먹으면 넘는다
            b.RunDecayPerSec = 0;
        }

        [Test]
        public void StartRun_Type2_UsesType2ConfigAndLogsTrackType()
        {
            var rig = new Rig2();

            rig.Run.StartRun(TrackType.Type2);

            Assert.That(rig.Log.LastVersion, Is.EqualTo(rig.Config.ConfigVersion));
            Assert.That(rig.Log.LastType, Is.EqualTo(TrackType.Type2));
            Assert.That(rig.Run.TrackType, Is.EqualTo(TrackType.Type2));
            rig.Ticks(3); // 실제 2형 설정으로 Tick이 끝까지 돈다
            Assert.That(rig.Run.State, Is.EqualTo(RunState.Running));
        }

        [Test]
        public void Type2_PickUpSnack_EatsOneServing_PublishesFoodEaten()
        {
            var rig = new Rig2(b => { b.RunDecayPerSec = 0; });
            rig.Run.StartRun(TrackType.Type2);

            rig.Run.PickUpSnack("juice");
            rig.Ticks(1);

            var eaten = rig.All<FoodEaten>().Single();
            Assert.That(eaten.FoodId, Is.EqualTo("juice"));
            Assert.That(eaten.Actual, Is.EqualTo(120).Within(Eps));
            Assert.That(rig.All<TrackSnackEaten>(), Is.Empty);
            rig.Ticks(3); // onset 1 + absorb 2 = 1인분 +30
            Assert.That(rig.Run.Actual, Is.EqualTo(150).Within(Eps));
        }

        [Test]
        public void Rest_StopsRunDecay_ForRestDuration()
        {
            var rig = new Rig2();
            rig.Run.StartRun(TrackType.Type2);

            rig.Run.StartRest();
            rig.Ticks(1);
            Assert.That(rig.Run.IsResting, Is.True);
            Assert.That(rig.All<RestStarted>(), Has.Count.EqualTo(1));
            rig.Ticks(2);
            Assert.That(rig.Run.Actual, Is.EqualTo(120).Within(Eps)); // 쉬는 3초 동안 기본 하강(0)만큼
            Assert.That(rig.Run.IsResting, Is.False);
            Assert.That(rig.All<RestEnded>(), Has.Count.EqualTo(1));

            rig.Ticks(1);
            Assert.That(rig.Run.Actual, Is.EqualTo(118).Within(Eps)); // 다시 달리기 하강
        }

        [Test]
        public void Rest_EndsWhenTellStarts()
        {
            var rig = new Rig2(b => { HighStart(b); b.RestDurationSec = 100; });
            rig.Run.StartRun(TrackType.Type2);
            rig.Run.PickUpSnack("juice");
            rig.Run.StartRest();
            rig.Ticks(1);
            Assert.That(rig.Run.IsResting, Is.True);

            rig.RunToHyperTell();

            Assert.That(rig.Run.IsResting, Is.False);
            Assert.That(rig.All<RestEnded>(), Has.Count.EqualTo(1));
        }

        [Test]
        public void StartRest_Type1_IsIgnored()
        {
            var rig = new Rig();
            rig.Run.StartRun(TrackType.Type1);

            rig.Run.StartRest();
            rig.Ticks(1);

            Assert.That(rig.Run.IsResting, Is.False);
            Assert.That(rig.Run.Actual, Is.EqualTo(118).Within(Eps));
        }

        [Test]
        public void StartRest_WhileFrozenOrResting_IsIgnored()
        {
            var rig = new Rig2(b => { HighStart(b); b.RestDurationSec = 100; });
            rig.Run.StartRun(TrackType.Type2);
            rig.Run.PickUpSnack("juice");
            rig.Run.StartRest();
            rig.Run.StartRest();
            rig.Ticks(1);
            Assert.That(rig.All<RestStarted>(), Has.Count.EqualTo(1));

            rig.RunToHyperTell();
            rig.Run.StartRest();
            rig.Ticks(1);

            Assert.That(rig.Run.IsResting, Is.False);
            Assert.That(rig.All<RestStarted>(), Has.Count.EqualTo(1));
        }

        [Test]
        public void ChooseJuice_ForwardsToFsm()
        {
            var rig = new Rig2(HighStart);
            rig.Run.StartRun(TrackType.Type2);
            rig.Run.PickUpSnack("juice");
            rig.RunToHyperTell();

            rig.Run.ChooseJuice();
            rig.Ticks(1);

            var answer = rig.All<StepAnswered>().Single();
            Assert.That(answer.ErrorCode, Is.EqualTo(HypoErrorCode.HyJuice));
            Assert.That(answer.ChoiceId, Is.EqualTo("juice"));
        }
    }
}
