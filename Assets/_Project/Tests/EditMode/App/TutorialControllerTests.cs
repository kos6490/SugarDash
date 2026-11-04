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
    public class TutorialControllerTests
    {
        private const double Eps = 1e-9;
        private const double GlucoseMoveSec = 3;
        private const double RetryTextSec = 1.5;

        private static readonly TutorialStep[] Type1Order =
        {
            TutorialStep.MoveLeft, TutorialStep.MoveRight, TutorialStep.Jump, TutorialStep.Slide, TutorialStep.Snack,
            TutorialStep.HypoRecognize, TutorialStep.HypoRespond, TutorialStep.HypoDose, TutorialStep.HypoRemeasure,
            TutorialStep.HypoNotify, TutorialStep.HyperNotify, TutorialStep.Finish,
        };

        private static readonly TutorialStep[] Type2Order =
        {
            TutorialStep.MoveLeft, TutorialStep.MoveRight, TutorialStep.Jump, TutorialStep.Slide, TutorialStep.FoodRow,
            TutorialStep.Rest, TutorialStep.HighNotice, TutorialStep.HighTell, TutorialStep.LowNotice,
            TutorialStep.LowTell, TutorialStep.Finish,
        };

        private sealed class NoRandom : IRandomSource
        {
            public double NextDouble() => throw new InvalidOperationException("급하강 확률 0에서는 난수를 읽지 않아야 한다");
        }

        private sealed class FakeLog : ILogRepository
        {
            public string BeginRun(string configVersion, TrackType trackType) => "run";

            public void BeginUnrecordedRun()
            {
            }

            public void EndRun()
            {
            }
        }

        /// <summary>
        /// RunControllerTests의 Rig처럼 조립한다. 1형은 저혈당·고혈당, 2형은 BuildType2와 BeginLow·BeginHyper를 구독한다.
        /// 기본 설정: 달리기 감소 2/s, 기본 하강 0. 목표값은 낮음 55, 높음 275.
        /// </summary>
        private sealed class Rig
        {
            public readonly DomainEventBus Bus = new DomainEventBus();
            public readonly List<object> Events = new List<object>();
            public readonly RunController Run;
            public readonly TutorialController Tutorial;
            public readonly GameConfig Config;
            public readonly TrackType Type;
            public readonly List<TutorialStep> Visited = new List<TutorialStep>();
            public EventDirector Director;
            public int Completed;

            public Rig(TrackType type, Action<ConfigBuilder> configure = null)
            {
                Type = type;
                var builder = new ConfigBuilder { RunDecayPerSec = 2, BaseDecayPerSec = 0 };
                configure?.Invoke(builder);
                Config = type == TrackType.Type2 ? builder.BuildType2() : builder.Build();
                Assert.That(GameConfigValidator.Validate(Config), Is.Empty, "테스트 설정도 불변식을 지켜야 한다");

                Collect<HypoEventTriggered>();
                Collect<HyperEventTriggered>();
                Collect<LowEventTriggered>();
                Collect<NoticeStarted>();
                Collect<StepAnswered>();
                Collect<PracticeRetried>();
                Collect<RunEliminated>();

                Run = new RunController(_ =>
                {
                    var model = new GlucoseModel(Config.Glucose);
                    var director = type == TrackType.Type2
                        ? new EventDirector(Config.Glucose, model, new NoRandom(), Bus, Config.Type2)
                        : new EventDirector(Config.Glucose, model, new NoRandom(), Bus);
                    Director = director;
                    var fsm = new HypoEventFsm(Config, model, director, Bus, () => Run.DistanceM);
                    var scoring = new SugarDash.Domain.Scoring.ScoringService(Config.Score);
                    var first = type == TrackType.Type2
                        ? Bus.Subscribe<LowEventTriggered>(fsm.BeginLow)
                        : Bus.Subscribe<HypoEventTriggered>(fsm.Begin);
                    return new RunSession(Config, model, director, fsm, scoring, first, scoring.Subscribe(Bus),
                        Bus.Subscribe<HyperEventTriggered>(fsm.BeginHyper));
                }, new FakeLog(), Bus);
                Tutorial = new TutorialController(Run, GlucoseMoveSec, RetryTextSec);
            }

            private void Collect<T>() => Bus.Subscribe<T>(e => Events.Add(e));

            public IReadOnlyList<T> All<T>() => Events.OfType<T>().ToList();

            public Rig Begin(bool canSkip = false)
            {
                Run.StartRun(Type, false);
                Tutorial.Begin(Type, canSkip, () => Completed++);
                Note();
                return this;
            }

            /// <summary>GameBootstrapper의 Update 순서처럼 주행을 먼저, 튜토리얼을 뒤에 진행한다.</summary>
            public void Frame(double dt = 1)
            {
                Run.Tick(dt);
                Tutorial.Tick(dt);
                Note();
            }

            public void Note()
            {
                var step = Tutorial.Step;
                if (step.HasValue && (Visited.Count == 0 || Visited[Visited.Count - 1] != step.Value))
                    Visited.Add(step.Value);
            }

            /// <summary>지금 안내를 성공시키는 입력을 한 번 넣는다.</summary>
            public void Advance()
            {
                var step = Tutorial.Step.Value;
                switch (Tutorial.Stage)
                {
                    case TutorialStage.Approach:
                        if (IsTrack(step))
                        {
                            Assert.That(Tutorial.TakePlacement(0), Is.Not.Null);
                            Tutorial.NotifyTargetNear();
                        }
                        else
                        {
                            Frame();
                        }
                        break;
                    case TutorialStage.Prompt:
                        Tutorial.Acknowledge();
                        break;
                    case TutorialStage.Trying:
                        if (Run.ResumeCountdownRemaining > 0 || Run.IsShowingAnswerFeedback)
                        {
                            Frame();
                        }
                        else if (IsTrack(step))
                        {
                            Tutorial.ReportAttempt(Success(step));
                        }
                        else
                        {
                            Answer();
                            Frame();
                        }
                        Note();
                        break;
                    default:
                        Frame();
                        break;
                }
                Note();
            }

            public void AdvanceUntil(Func<bool> done, int max = 500)
            {
                for (int i = 0; i < max && !done(); i++) Advance();
                Assert.That(done(), Is.True, $"도달하지 못했다: {Tutorial.Step} {Tutorial.Stage} {Run.Phase}");
            }

            public void AdvanceToTrying(TutorialStep step) =>
                AdvanceUntil(() => Tutorial.Step == step && Tutorial.Stage == TutorialStage.Trying &&
                                   Run.ResumeCountdownRemaining <= 0 && !Run.IsShowingAnswerFeedback);

            private void Answer()
            {
                switch (Run.Phase)
                {
                    case HypoPhase.Recognize: Run.PressMeter(); break;
                    case HypoPhase.Respond: Run.ChooseSnack("juice"); break;
                    case HypoPhase.Dose: Run.ChooseDose(DoseLevel.Proper); break;
                    case HypoPhase.Remeasure: Run.ChooseRecheck(); break;
                    case HypoPhase.Notify:
                    case HypoPhase.HyperNotify:
                    case HypoPhase.LowNotify:
                        Run.PressHelp(Run.Phase);
                        break;
                }
            }

            private static bool IsTrack(TutorialStep step) => step <= TutorialStep.Rest;

            private static TutorialAttempt Success(TutorialStep step)
            {
                switch (step)
                {
                    case TutorialStep.Snack:
                    case TutorialStep.FoodRow:
                        return new TutorialAttempt(false, true, false);
                    case TutorialStep.Rest:
                        return new TutorialAttempt(false, false, true);
                    default:
                        return new TutorialAttempt(false, false, false);
                }
            }
        }

        [Test]
        public void Type1_StepOrder()
        {
            var rig = new Rig(TrackType.Type1).Begin();

            rig.AdvanceUntil(() => rig.Tutorial.Stage == TutorialStage.Done);

            Assert.That(rig.Visited, Is.EqualTo(Type1Order));
            Assert.That(rig.Completed, Is.EqualTo(1));
            Assert.That(rig.Tutorial.IsGuided, Is.False);
            Assert.That(rig.Tutorial.IsActive, Is.True);
            Assert.That(rig.Run.IsTutorialGuided, Is.False);
            Assert.That(rig.All<StepAnswered>().All(a => a.IsCorrect), Is.True);
        }

        [Test]
        public void Type2_StepOrder()
        {
            var rig = new Rig(TrackType.Type2).Begin();

            rig.AdvanceUntil(() => rig.Tutorial.Stage == TutorialStage.Done);

            Assert.That(rig.Visited, Is.EqualTo(Type2Order));
            Assert.That(rig.Completed, Is.EqualTo(1));
            Assert.That(rig.All<NoticeStarted>().Select(n => n.Kind),
                Is.EqualTo(new[] { NoticeKind.High, NoticeKind.Low }));
        }

        [Test]
        public void Begin_GuidedGateNoneFirstStepApproach()
        {
            var rig = new Rig(TrackType.Type1).Begin(canSkip: true);

            Assert.That(rig.Tutorial.IsActive, Is.True);
            Assert.That(rig.Tutorial.IsGuided, Is.True);
            Assert.That(rig.Tutorial.CanSkip, Is.True);
            Assert.That(rig.Tutorial.Type, Is.EqualTo(TrackType.Type1));
            Assert.That(rig.Tutorial.Step, Is.EqualTo(TutorialStep.MoveLeft));
            Assert.That(rig.Tutorial.Stage, Is.EqualTo(TutorialStage.Approach));
            Assert.That(rig.Run.IsTutorialGuided, Is.True);
            Assert.That(rig.Director.Gate, Is.EqualTo(TriggerGate.None));

            rig.Tutorial.Stop();
            Assert.That(rig.Tutorial.IsActive, Is.False);
            Assert.That(rig.Tutorial.IsGuided, Is.False);
            Assert.That(rig.Tutorial.Stage, Is.EqualTo(TutorialStage.Inactive));
            Assert.That(rig.Tutorial.Step, Is.Null);
        }

        private static TutorialPlacement PlacementAt(TrackType type, TutorialStep step, int lane)
        {
            var rig = new Rig(type).Begin();
            rig.AdvanceUntil(() => rig.Tutorial.Step == step);
            var placement = rig.Tutorial.TakePlacement(lane);
            Assert.That(rig.Tutorial.TakePlacement(lane), Is.Null, "배치는 한 번만 돌려준다");
            return placement;
        }

        [TestCase(0, -1, new[] { 0, 1 })]
        [TestCase(-1, 0, new[] { -1, 1 })]
        [TestCase(1, 0, new[] { -1, 1 })]
        public void Placement_Lanes_MoveLeft(int p, int open, int[] blocked)
        {
            var placement = PlacementAt(TrackType.Type1, TutorialStep.MoveLeft, p);
            Assert.That(placement.Kind, Is.EqualTo(TutorialObject.LaneBlock));
            Assert.That(placement.Lanes, Is.EquivalentTo(blocked));
            Assert.That(placement.Lanes, Has.No.Member(open));
        }

        [TestCase(0, new[] { -1, 0 })]
        [TestCase(-1, new[] { -1, 1 })]
        [TestCase(1, new[] { -1, 1 })]
        public void Placement_Lanes_MoveRight(int p, int[] blocked)
        {
            var placement = PlacementAt(TrackType.Type1, TutorialStep.MoveRight, p);
            Assert.That(placement.Kind, Is.EqualTo(TutorialObject.LaneBlock));
            Assert.That(placement.Lanes, Is.EquivalentTo(blocked));
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(1)]
        public void Placement_Lanes_JumpSlideAllLanes(int p)
        {
            var jump = PlacementAt(TrackType.Type1, TutorialStep.Jump, p);
            Assert.That(jump.Kind, Is.EqualTo(TutorialObject.LowObstacle));
            Assert.That(jump.Lanes, Is.EquivalentTo(new[] { -1, 0, 1 }));

            var slide = PlacementAt(TrackType.Type1, TutorialStep.Slide, p);
            Assert.That(slide.Kind, Is.EqualTo(TutorialObject.HighObstacle));
            Assert.That(slide.Lanes, Is.EquivalentTo(new[] { -1, 0, 1 }));
        }

        [TestCase(0, 1)]
        [TestCase(-1, 0)]
        [TestCase(1, 0)]
        public void Placement_Lanes_SnackRestOneLane(int p, int lane)
        {
            var snack = PlacementAt(TrackType.Type1, TutorialStep.Snack, p);
            Assert.That(snack.Kind, Is.EqualTo(TutorialObject.Snack));
            Assert.That(snack.Lanes, Is.EqualTo(new[] { lane }));

            var rest = PlacementAt(TrackType.Type2, TutorialStep.Rest, p);
            Assert.That(rest.Kind, Is.EqualTo(TutorialObject.RestSpot));
            Assert.That(rest.Lanes, Is.EqualTo(new[] { lane }));
        }

        [Test]
        public void Placement_FoodRow_LanesLeftToSpawner()
        {
            var row = PlacementAt(TrackType.Type2, TutorialStep.FoodRow, 0);
            Assert.That(row.Kind, Is.EqualTo(TutorialObject.FoodRow));
            Assert.That(row.Lanes, Is.Empty);
        }

        [Test]
        public void TrackStep_FailRetriesSameStep()
        {
            var rig = new Rig(TrackType.Type1).Begin();
            rig.AdvanceToTrying(TutorialStep.Jump);

            rig.Tutorial.ReportAttempt(new TutorialAttempt(true, false, false));
            Assert.That(rig.Tutorial.Stage, Is.EqualTo(TutorialStage.Retry));
            Assert.That(rig.Tutorial.Step, Is.EqualTo(TutorialStep.Jump));
            Assert.That(rig.Run.IsTutorialHeld, Is.True);

            rig.Frame(1);
            Assert.That(rig.Tutorial.Stage, Is.EqualTo(TutorialStage.Retry));

            rig.Frame(1);
            Assert.That(rig.Tutorial.Stage, Is.EqualTo(TutorialStage.Approach));
            Assert.That(rig.Tutorial.Step, Is.EqualTo(TutorialStep.Jump));
            Assert.That(rig.Run.IsTutorialHeld, Is.False);
            Assert.That(rig.Run.ResumeCountdownRemaining, Is.EqualTo(0));
            var again = rig.Tutorial.TakePlacement(0);
            Assert.That(again.Kind, Is.EqualTo(TutorialObject.LowObstacle));
            Assert.That(again.Lanes, Is.EquivalentTo(new[] { -1, 0, 1 }));
        }

        [Test]
        public void TrackStep_ItemMissed_Retries()
        {
            var rig = new Rig(TrackType.Type2).Begin();
            rig.AdvanceToTrying(TutorialStep.FoodRow);

            rig.Tutorial.ReportAttempt(new TutorialAttempt(false, false, false));

            Assert.That(rig.Tutorial.Stage, Is.EqualTo(TutorialStage.Retry));
            Assert.That(rig.Tutorial.Step, Is.EqualTo(TutorialStep.FoodRow));
        }

        [Test]
        public void TrackStep_AcknowledgeStartsCountdown()
        {
            var rig = new Rig(TrackType.Type1, b => b.ResumeCountdownSec = 2).Begin();
            rig.Tutorial.TakePlacement(0);
            rig.Tutorial.NotifyTargetNear();

            rig.Tutorial.Acknowledge();

            Assert.That(rig.Tutorial.Stage, Is.EqualTo(TutorialStage.Trying));
            Assert.That(rig.Run.IsTutorialHeld, Is.False);
            Assert.That(rig.Run.ResumeCountdownRemaining, Is.EqualTo(2).Within(Eps));
        }

        [Test]
        public void Prompt_HoldsUntilAcknowledge()
        {
            var rig = new Rig(TrackType.Type1).Begin();
            rig.Tutorial.TakePlacement(0);
            rig.Tutorial.NotifyTargetNear();
            Assert.That(rig.Tutorial.Stage, Is.EqualTo(TutorialStage.Prompt));

            for (int i = 0; i < 100; i++) rig.Frame(1);

            Assert.That(rig.Tutorial.Stage, Is.EqualTo(TutorialStage.Prompt));
            Assert.That(rig.Run.IsTutorialHeld, Is.True);
            Assert.That(rig.Run.WorldTime, Is.EqualTo(0));
        }

        [Test]
        public void GlucoseStep_RetriesMoveUntilTriggered()
        {
            // 고혈당 목표 (250 + 262) / 2 = 256. 기본 하강 0.5/s + 달리기 2/s로 3초 이동하면 7.5가 깎여
            // 248.5에서 끝나 기준(250)에 못 미친다. 급하강량은 불변식(최악의 처치 시점 혈당)을 맞추려고 줄인다.
            var rig = new Rig(TrackType.Type1, b =>
            {
                b.BaseDecayPerSec = 0.5;
                b.GlucoseHigh = 262;
                b.SuddenDropAmount = 10;
            }).Begin();
            rig.AdvanceUntil(() => rig.Tutorial.Step == TutorialStep.HyperNotify);
            Assert.That(rig.Tutorial.Stage, Is.EqualTo(TutorialStage.Approach));
            Assert.That(rig.Run.IsGlucoseMoveActive, Is.True);

            for (int i = 0; i < 3; i++) rig.Frame(1);
            Assert.That(rig.Run.Phase, Is.EqualTo(HypoPhase.Idle), "첫 이동만으로는 발동하지 않는 설정이어야 한다");
            Assert.That(rig.Run.Actual, Is.LessThan(rig.Config.Glucose.Levels.HyperThreshold));
            Assert.That(rig.Run.IsGlucoseMoveActive, Is.True, "이동이 끝나면 다시 건다");

            for (int i = 0; i < 20 && rig.Tutorial.Stage == TutorialStage.Approach; i++) rig.Frame(1);

            Assert.That(rig.Tutorial.Stage, Is.EqualTo(TutorialStage.Prompt));
            Assert.That(rig.Tutorial.Step, Is.EqualTo(TutorialStep.HyperNotify));
            Assert.That(rig.Run.Phase, Is.EqualTo(HypoPhase.HyperNotify));
            Assert.That(rig.Run.IsTutorialHeld, Is.True);
            Assert.That(rig.Run.Actual, Is.LessThanOrEqualTo(rig.Config.Elimination.GlucoseHigh));
        }

        [Test]
        public void GlucoseStep_Type1Low_SetsGateAndMovesToLowTarget()
        {
            var rig = new Rig(TrackType.Type1, b => b.RunDecayPerSec = 0).Begin();
            rig.AdvanceUntil(() => rig.Tutorial.Step == TutorialStep.HypoRecognize);

            Assert.That(rig.Director.Gate, Is.EqualTo(TriggerGate.LowOnly));
            Assert.That(rig.Run.IsGlucoseMoveActive, Is.True);
            Assert.That(TutorialTargets.Low(rig.Config), Is.EqualTo(55).Within(Eps));
            Assert.That(TutorialTargets.High(rig.Config), Is.EqualTo(275).Within(Eps));

            rig.AdvanceUntil(() => rig.Tutorial.Stage == TutorialStage.Prompt);
            Assert.That(rig.Run.Phase, Is.EqualTo(HypoPhase.Recognize));
            Assert.That(rig.Run.IsTutorialHeld, Is.True);

            rig.Tutorial.Acknowledge();
            Assert.That(rig.Run.ResumeCountdownRemaining, Is.EqualTo(rig.Config.Hypo.ResumeCountdownSec).Within(Eps));
        }

        [Test]
        public void PanelStep_AcknowledgeReleasesWithoutCountdown()
        {
            var rig = new Rig(TrackType.Type1).Begin();
            rig.AdvanceUntil(() => rig.Tutorial.Step == TutorialStep.HypoRespond);
            Assert.That(rig.Tutorial.Stage, Is.EqualTo(TutorialStage.Prompt));
            Assert.That(rig.Run.IsTutorialHeld, Is.True);

            rig.Tutorial.Acknowledge();

            Assert.That(rig.Tutorial.Stage, Is.EqualTo(TutorialStage.Trying));
            Assert.That(rig.Run.IsTutorialHeld, Is.False);
            Assert.That(rig.Run.ResumeCountdownRemaining, Is.EqualTo(0));
        }

        [Test]
        public void PanelStep_WrongAnswerReturnsToPrompt()
        {
            var rig = new Rig(TrackType.Type1, b => b.AnswerFeedbackSec = 1).Begin();
            rig.AdvanceToTrying(TutorialStep.HypoRespond);

            rig.Run.ChooseSnack("cracker");
            rig.Frame(0.5);
            Assert.That(rig.Run.IsShowingAnswerFeedback, Is.True);
            Assert.That(rig.Tutorial.Stage, Is.EqualTo(TutorialStage.Trying), "결과 문구 창이 닫힐 때까지 기다린다");

            rig.Frame(0.5);
            Assert.That(rig.Run.IsShowingAnswerFeedback, Is.False);
            Assert.That(rig.Tutorial.Stage, Is.EqualTo(TutorialStage.Prompt));
            Assert.That(rig.Tutorial.Step, Is.EqualTo(TutorialStep.HypoRespond));
            Assert.That(rig.Run.Phase, Is.EqualTo(HypoPhase.Respond));
            Assert.That(rig.Run.IsTutorialHeld, Is.True);
            Assert.That(rig.All<PracticeRetried>(), Has.Count.EqualTo(1));
            Assert.That(rig.All<StepAnswered>().Any(a => !a.IsCorrect), Is.False);

            // 다시 고르면 다음 안내로 간다.
            rig.Tutorial.Acknowledge();
            rig.Run.ChooseSnack("juice");
            rig.Frame(1);
            Assert.That(rig.Tutorial.Step, Is.EqualTo(TutorialStep.HypoDose));
            Assert.That(rig.Tutorial.Stage, Is.EqualTo(TutorialStage.Prompt));
        }

        [Test]
        public void TellStep_WrongAnswerShowsRetryThenPrompt()
        {
            var rig = new Rig(TrackType.Type2).Begin();
            rig.AdvanceToTrying(TutorialStep.HighTell);

            rig.Run.ChooseJuice();
            rig.Frame(0.5);
            Assert.That(rig.Tutorial.Stage, Is.EqualTo(TutorialStage.Retry));
            Assert.That(rig.Tutorial.Step, Is.EqualTo(TutorialStep.HighTell));
            Assert.That(rig.Run.IsTutorialHeld, Is.True);

            rig.Frame(1);
            Assert.That(rig.Tutorial.Stage, Is.EqualTo(TutorialStage.Retry), "retryTextSec(1.5) 동안 문구를 보여 준다");

            rig.Frame(0.5);
            Assert.That(rig.Tutorial.Stage, Is.EqualTo(TutorialStage.Prompt));
            Assert.That(rig.Tutorial.Step, Is.EqualTo(TutorialStep.HighTell));
            Assert.That(rig.Run.Phase, Is.EqualTo(HypoPhase.HyperNotify));
            Assert.That(rig.Run.IsTutorialHeld, Is.True);

            rig.Tutorial.Acknowledge();
            Assert.That(rig.Run.ResumeCountdownRemaining, Is.EqualTo(0));
        }

        [Test]
        public void Type1_HyperNotify_WrongAnswerShowsRetry()
        {
            var rig = new Rig(TrackType.Type1).Begin();
            rig.AdvanceToTrying(TutorialStep.HyperNotify);

            rig.Run.ChooseKeepRunning();
            rig.Frame(1);

            Assert.That(rig.Tutorial.Stage, Is.EqualTo(TutorialStage.Retry));
            Assert.That(rig.Tutorial.Step, Is.EqualTo(TutorialStep.HyperNotify));
        }

        [Test]
        public void Guided_NoNaturalTrigger()
        {
            var rig = new Rig(TrackType.Type1).Begin();
            rig.AdvanceUntil(() => rig.Tutorial.Step == TutorialStep.Snack);

            // 달리기 감소 2/s로 120에서 30초면 60, 임계값 70 아래다.
            for (int i = 0; i < 30; i++) rig.Frame(1);

            Assert.That(rig.Run.Actual, Is.LessThan(rig.Config.Glucose.Levels.HypoThreshold));
            Assert.That(rig.All<HypoEventTriggered>(), Is.Empty);
            Assert.That(rig.Run.Phase, Is.EqualTo(HypoPhase.Idle));
        }

        [Test]
        public void Finish_ResetsGlucoseAndRestoresNormalRules()
        {
            var rig = new Rig(TrackType.Type1).Begin();
            rig.AdvanceUntil(() => rig.Tutorial.Step == TutorialStep.Finish);
            Assert.That(rig.Tutorial.Stage, Is.EqualTo(TutorialStage.Prompt));
            Assert.That(rig.Run.IsTutorialHeld, Is.True);
            Assert.That(rig.Run.Actual, Is.EqualTo(rig.Config.Glucose.Levels.Initial).Within(Eps));
            Assert.That(rig.Run.Displayed, Is.EqualTo(rig.Config.Glucose.Levels.Initial).Within(Eps));
            Assert.That(rig.Completed, Is.EqualTo(0));

            // 설명 창이 뜨기 전 프레임에 쌓인 충돌은 안내 중 충돌이라 탈락시키지 않는다.
            rig.Run.HitObstacle();
            rig.Tutorial.Acknowledge();

            Assert.That(rig.Tutorial.Stage, Is.EqualTo(TutorialStage.Done));
            Assert.That(rig.Completed, Is.EqualTo(1));
            Assert.That(rig.Director.Gate, Is.EqualTo(TriggerGate.All));
            Assert.That(rig.Run.IsTutorialGuided, Is.False);
            Assert.That(rig.Run.ResumeCountdownRemaining, Is.EqualTo(rig.Config.Hypo.ResumeCountdownSec).Within(Eps));

            rig.Frame(1); // 3-2-1
            rig.Frame(1); // 쌓인 충돌 처리
            Assert.That(rig.Run.State, Is.EqualTo(RunState.Running));
            Assert.That(rig.Run.Actual, Is.EqualTo(rig.Config.Glucose.Levels.Initial - 2).Within(Eps));

            rig.Run.HitObstacle();
            rig.Frame(1);
            Assert.That(rig.Run.State, Is.EqualTo(RunState.Eliminated));
            Assert.That(rig.Tutorial.Stage, Is.EqualTo(TutorialStage.Done));
        }
    }
}
