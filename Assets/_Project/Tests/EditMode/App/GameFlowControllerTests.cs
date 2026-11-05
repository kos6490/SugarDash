using System;
using System.Collections.Generic;
using System.Linq;
using SugarDash.Domain.Config;
using SugarDash.Domain.Director;
using SugarDash.Domain.Events;
using SugarDash.Domain.Glucose;
using SugarDash.Domain.Hypo;
using SugarDash.Domain.Metrics;
using SugarDash.Domain.Ports;
using SugarDash.Domain.Records;
using SugarDash.Domain.Tests;
using NUnit.Framework;

namespace SugarDash.App.Tests
{
    public class GameFlowControllerTests
    {
        private sealed class NoRandom : IRandomSource
        {
            public double NextDouble() => throw new InvalidOperationException("급하강 확률 0에서는 난수를 읽지 않아야 한다");
        }

        private sealed class FakeLog : ILogRepository
        {
            public int Begun;
            public int Ended;

            public int BegunUnrecorded;

            public string BeginRun(string configVersion, TrackType trackType) => "run-" + ++Begun;

            public void BeginUnrecordedRun() => BegunUnrecorded++;

            public void EndRun() => Ended++;
        }

        /// <summary>버스에 발행된 이벤트를 그대로 돌려줘 기록 파일 왕복을 흉내 낸다.</summary>
        private sealed class FakeQuery : ILogQuery
        {
            private readonly FakeLog _log;
            public readonly List<object> Published = new List<object>();
            public bool ReturnsNothing;
            public bool OmitEliminated;
            public string RequestedRunId;
            public int EndedWhenQueried = -1;

            public FakeQuery(FakeLog log) => _log = log;

            public IReadOnlyList<string> ListRunIds() => throw new NotSupportedException();

            public RunRecord LoadRun(string runId)
            {
                RequestedRunId = runId;
                EndedWhenQueried = _log.Ended;
                if (ReturnsNothing) return null;
                var events = OmitEliminated
                    ? Published.Where(e => !(e is RunEliminated)).ToArray()
                    : Published.ToArray();
                return new RunRecord(runId, "test", null, null, events, 0);
            }
        }

        private sealed class FakeBest : IBestScoreStore
        {
            public double? Stored;
            public int Saves;
            public string LastConfigVersion;
            public double? Load(string configVersion) => Stored;

            public void Save(string configVersion, double score)
            {
                Saves++;
                LastConfigVersion = configVersion;
                Stored = score;
            }
        }

        private sealed class FakeSettings : ISettingsStore
        {
            public TrackType? Stored;
            public int Saves;
            public bool TutorialPending; // 가짜는 false로 시작해 기존 테스트가 일반 판을 기대하게 둔다.
            public readonly System.Collections.Generic.HashSet<TrackType> TutorialDone =
                new System.Collections.Generic.HashSet<TrackType>();
            public TrackType? LoadTrackType() => Stored;
            public bool LoadTutorialPending() => TutorialPending;
            public void SaveTutorialPending(bool pending) => TutorialPending = pending;
            public bool LoadTutorialDone(TrackType type) => TutorialDone.Contains(type);
            public void SaveTutorialDone(TrackType type) => TutorialDone.Add(type);

            public void SaveTrackType(TrackType type)
            {
                Saves++;
                Stored = type;
            }
        }

        private FakeBest _best;
        private FakeBest _best2;
        private FakeSettings _settings;
        private FakeLog _log;
        private FakeQuery _query;
        private RunController _run;
        private GameFlowController _flow;
        private TutorialController _tutorial;

        // 달리기 감소 2/s: 26초에 저혈당 발동, 아무것도 안 하면 36초에 탈락.
        [SetUp]
        public void SetUp() => SetUpWith(new ConfigBuilder { RunDecayPerSec = 2 });

        private const double PauseCountdownSec = 3;

        private void SetUpWith(ConfigBuilder builder, double eliminationSceneSec = 0)
        {
            var config = builder.Build();
            var bus = new DomainEventBus();
            _log = new FakeLog();
            _query = new FakeQuery(_log);
            bus.Subscribe<HypoEventTriggered>(e => _query.Published.Add(e));
            bus.Subscribe<StepAnswered>(e => _query.Published.Add(e));
            bus.Subscribe<RunEliminated>(e => _query.Published.Add(e));
            bus.Subscribe<GlucoseRangeEntered>(e => _query.Published.Add(e));
            bus.Subscribe<GlucoseRangeExited>(e => _query.Published.Add(e));
            bus.Subscribe<RecheckFastForward>(e => _query.Published.Add(e));
            bus.Subscribe<SectionEntered>(e => _query.Published.Add(e));
            var config2 = builder.BuildType2();
            _run = new RunController(type =>
            {
                if (type == TrackType.Type2)
                {
                    var model2 = new GlucoseModel(config2.Glucose);
                    var director2 = new EventDirector(config2.Glucose, model2, new NoRandom(), bus, config2.Type2);
                    var fsm2 = new HypoEventFsm(config2, model2, director2, bus, () => _run.DistanceM);
                    var scoring2 = new SugarDash.Domain.Scoring.ScoringService(config2.Score);
                    return new RunSession(config2, model2, director2, fsm2, scoring2, scoring2.Subscribe(bus),
                        bus.Subscribe<LowEventTriggered>(fsm2.BeginLow),
                        bus.Subscribe<HyperEventTriggered>(fsm2.BeginHyper));
                }
                var model = new GlucoseModel(config.Glucose);
                var director = new EventDirector(config.Glucose, model, new NoRandom(), bus);
                var fsm = new HypoEventFsm(config, model, director, bus, () => _run.DistanceM);
                var scoring = new SugarDash.Domain.Scoring.ScoringService(config.Score);
                return new RunSession(config, model, director, fsm, scoring, bus.Subscribe<HypoEventTriggered>(fsm.Begin),
                    scoring.Subscribe(bus), bus.Subscribe<HyperEventTriggered>(fsm.BeginHyper));
            }, _log, bus);
            _best = new FakeBest();
            _best2 = new FakeBest();
            _settings = new FakeSettings { Stored = TrackType.Type1 };
            _tutorial = new TutorialController(_run, 3, 1.5);
            _flow = new GameFlowController(_run, _query, _settings, Profiles(config.Score, config2.Score), _tutorial,
                PauseCountdownSec, eliminationSceneSec);
        }

        private IReadOnlyDictionary<TrackType, TrackTypeProfile> Profiles(
            SugarDash.Domain.Config.ScoreConfig score1, SugarDash.Domain.Config.ScoreConfig score2) => new Dictionary<TrackType, TrackTypeProfile>
        {
            { TrackType.Type1, new TrackTypeProfile(score1, _best, "cfg-test") },
            { TrackType.Type2, new TrackTypeProfile(score2, _best2, "cfg-test-2") },
        };

        /// <summary>저장된 유형(1형)으로 타이틀을 거쳐 주행을 시작한다.</summary>
        private void StartType1()
        {
            _flow.Begin();
            _flow.StartRun();
        }

        /// <summary>GameBootstrapper처럼 RunController.Tick 뒤 GameFlowController.Tick을 부른다.</summary>
        private void Frames(int count)
        {
            for (int i = 0; i < count; i++)
            {
                _run.Tick(1);
                _flow.Tick(1);
            }
        }

        /// <summary>장애물에 한 번 부딪혀 탈락시킨다. 저혈당 발동(26초) 전에 끝난다.</summary>
        private void HitObstacleOnce()
        {
            _run.HitObstacle();
            Frames(1);
        }

        [Test]
        public void Begin_NoSavedType_ShowsTypeSelect()
        {
            _settings.Stored = null;

            _flow.Begin();

            Assert.That(_flow.Screen, Is.EqualTo(FlowScreen.TypeSelect));
            Assert.That(_flow.CurrentType, Is.Null);
            Assert.That(_run.State, Is.EqualTo(RunState.Idle));
        }

        [Test]
        public void Begin_SavedType_ShowsTitle_DoesNotStartRun()
        {
            _settings.Stored = TrackType.Type2;

            _flow.Begin();

            Assert.That(_flow.Screen, Is.EqualTo(FlowScreen.Title));
            Assert.That(_flow.CurrentType, Is.EqualTo(TrackType.Type2));
            Assert.That(_run.State, Is.EqualTo(RunState.Idle));
            Assert.That(_log.Begun, Is.EqualTo(0));
        }

        [Test]
        public void SelectType_SavesAndGoesToTitle()
        {
            _settings.Stored = null;
            _flow.Begin();

            _flow.SelectType(TrackType.Type2);

            Assert.That(_settings.Stored, Is.EqualTo(TrackType.Type2));
            Assert.That(_flow.CurrentType, Is.EqualTo(TrackType.Type2));
            Assert.That(_flow.Screen, Is.EqualTo(FlowScreen.Title));
        }

        [Test]
        public void StartRun_FromTitle_StartsRunOnce()
        {
            _flow.Begin();
            _flow.StartRun();
            _flow.StartRun();

            Assert.That(_flow.Screen, Is.EqualTo(FlowScreen.Running));
            Assert.That(_run.State, Is.EqualTo(RunState.Running));
            Assert.That(_log.Begun, Is.EqualTo(1));
            Assert.That(_flow.IsShowingResult, Is.False);
            Assert.That(_flow.CanRetry, Is.False);
        }

        [Test]
        public void SelectTypeInSettings_NextRunUsesNewTypeProfile()
        {
            StartType1();
            HitObstacleOnce();
            Assert.That(_best.Saves, Is.EqualTo(1));
            _flow.BackToTitle();

            _flow.OpenSettings();
            Assert.That(_flow.Screen, Is.EqualTo(FlowScreen.Settings));
            _flow.SelectType(TrackType.Type2);
            Assert.That(_flow.Screen, Is.EqualTo(FlowScreen.Title));
            // 다른 유형은 튜토리얼 판으로 열리므로, 끝낸 적 있는 유형으로 두고 건너뛰어 일반 판을 연다.
            _settings.TutorialDone.Add(TrackType.Type2);
            _flow.StartRun();
            _flow.SkipTutorial();
            Assert.That(_run.TrackType, Is.EqualTo(TrackType.Type2));
            HitObstacleOnce();

            Assert.That(_flow.Screen, Is.EqualTo(FlowScreen.Result));
            Assert.That(_best2.Saves, Is.EqualTo(1));
            Assert.That(_best2.LastConfigVersion, Is.EqualTo("cfg-test-2"));
            Assert.That(_best.Saves, Is.EqualTo(1), "1형 저장소는 그대로");
        }

        [Test]
        public void CloseSettings_ReturnsToTitle()
        {
            _flow.Begin();
            _flow.OpenSettings();

            _flow.CloseSettings();

            Assert.That(_flow.Screen, Is.EqualTo(FlowScreen.Title));
        }

        [Test]
        public void BackToTitle_FromResult_ClearsResult()
        {
            StartType1();
            HitObstacleOnce();
            Assert.That(_flow.LastScore, Is.Not.Null);

            _flow.BackToTitle();

            Assert.That(_flow.Screen, Is.EqualTo(FlowScreen.Title));
            Assert.That(_flow.LastResult, Is.Null);
            Assert.That(_flow.LastScore, Is.Null);
            Assert.That(_flow.BestScore, Is.Null);
            Assert.That(_flow.IsNewBest, Is.False);
        }

        [Test]
        public void CommandsInWrongScreen_AreIgnored()
        {
            _flow.Begin();
            _flow.Retry();
            _flow.BackToTitle();
            _flow.CloseSettings();
            Assert.That(_flow.Screen, Is.EqualTo(FlowScreen.Title));
            Assert.That(_log.Begun, Is.EqualTo(0));

            _flow.StartRun();
            _flow.SelectType(TrackType.Type2);
            _flow.OpenSettings();
            _flow.StartRun();

            Assert.That(_flow.Screen, Is.EqualTo(FlowScreen.Running));
            Assert.That(_flow.CurrentType, Is.EqualTo(TrackType.Type1));
            Assert.That(_settings.Saves, Is.EqualTo(0));
            Assert.That(_log.Begun, Is.EqualTo(1));
        }

        [Test]
        public void Constructor_MissingProfile_Throws()
        {
            var only1 = new Dictionary<TrackType, TrackTypeProfile>
            {
                { TrackType.Type1, new TrackTypeProfile(new ConfigBuilder().Build().Score, _best, "v") },
            };

            Assert.Throws<ArgumentException>(() => new GameFlowController(_run, _query, _settings, only1, _tutorial, 3, 0));
        }

        [Test]
        public void ObstacleHit_ClosesLogThenLoadsResult()
        {
            StartType1();
            HitObstacleOnce();

            Assert.That(_flow.IsShowingResult, Is.True);
            Assert.That(_run.State, Is.EqualTo(RunState.Idle));
            Assert.That(_query.RequestedRunId, Is.EqualTo("run-1"));
            Assert.That(_query.EndedWhenQueried, Is.EqualTo(1), "기록을 닫은 뒤 읽는다");
            Assert.That(_flow.LastResult.Outcome, Is.EqualTo(RunOutcome.Eliminated));
            Assert.That(_flow.LastResult.Events, Is.Empty);

            Frames(3);
            Assert.That(_log.Ended, Is.EqualTo(1), "결과 화면에서는 다시 닫지 않는다");
        }

        [Test]
        public void Eliminated_ShowsResultWithFailedEvent()
        {
            var builder = new ConfigBuilder { RunDecayPerSec = 2 };
            SetUpWith(builder);
            StartType1();

            Frames(36);

            var result = _flow.LastResult;
            Assert.That(result.Outcome, Is.EqualTo(RunOutcome.Eliminated));
            Assert.That(result.Events, Has.Count.EqualTo(1));
            Assert.That(result.Events[0].AllCorrect, Is.False);
            Assert.That(result.Events[0].Steps[MetricStep.Recognize], Is.False);
        }

        /// <summary>
        /// 저혈당 대처(재확인 빨리감기 포함)를 마치고 알리기 대기 시간 초과로 탈락한다.
        /// 기록으로 다시 계산한 점수가 주행 중 누적 점수와 같아야 한다.
        /// </summary>
        [Test]
        public void LastScore_MatchesLiveScore_AfterCopingAndFastForward()
        {
            StartType1();
            for (int i = 0; i < 26; i++)
            {
                if (i % 5 == 0) _run.AdvanceDistance(10);
                Frames(1);
            }
            Assert.That(_run.Phase, Is.EqualTo(HypoPhase.Recognize), "저혈당이 발동한 상태여야 한다");

            _run.PressMeter();
            Frames(1);
            _run.ChooseSnack("juice");
            Frames(1);
            _run.ChooseDose(DoseLevel.Proper);
            Frames(1);
            _run.ChooseRecheck();
            Frames(1);
            Assert.That(_query.Published.OfType<RecheckFastForward>().Any(), Is.True, "빨리감기가 일어나야 한다");
            Assert.That(_query.Published.OfType<GlucoseRangeEntered>().Any(), Is.True);
            Assert.That(_run.Score, Is.GreaterThan(0));

            // 알리기를 누르지 않고 기다려 제한 시간 초과로 탈락시킨다.
            double liveScore = double.NaN;
            for (int i = 0; i < 60 && !_flow.IsShowingResult; i++)
            {
                _run.Tick(1);
                if (_run.State == RunState.Eliminated) liveScore = _run.Score;
                _flow.Tick(1);
            }

            Assert.That(_flow.IsShowingResult, Is.True);
            Assert.That(_query.Published.OfType<RunEliminated>().Count(), Is.EqualTo(1));
            Assert.That(double.IsNaN(liveScore), Is.False);
            Assert.That(_flow.LastScore, Is.Not.Null);
            Assert.That(_flow.LastScore.Value, Is.EqualTo(liveScore).Within(1e-9));
        }

        [Test]
        public void Retry_WhileRunning_IsIgnored()
        {
            StartType1();
            Frames(3);

            _flow.Retry();

            Assert.That(_log.Begun, Is.EqualTo(1));
            Assert.That(_run.WorldTime, Is.EqualTo(3));
        }

        [Test]
        public void Retry_FromResult_StartsFreshRun()
        {
            StartType1();
            HitObstacleOnce();
            Assert.That(_flow.CanRetry, Is.True);

            _flow.Retry();

            Assert.That(_flow.IsShowingResult, Is.False);
            Assert.That(_flow.LastResult, Is.Null);
            Assert.That(_log.Begun, Is.EqualTo(2));
            Assert.That(_run.State, Is.EqualTo(RunState.Running));
            Assert.That(_run.CurrentRunId, Is.EqualTo("run-2"));
        }

        [Test]
        public void MissingRecord_StillShowsResultScreen()
        {
            _query.ReturnsNothing = true;
            StartType1();

            HitObstacleOnce();

            Assert.That(_flow.IsShowingResult, Is.True);
            Assert.That(_flow.LastResult, Is.Null);
        }

        [Test]
        public void FirstRun_IsNewBestAndSaved()
        {
            StartType1();
            HitObstacleOnce();

            Assert.That(_flow.LastScore, Is.Not.Null);
            Assert.That(_flow.IsNewBest, Is.True);
            Assert.That(_flow.BestScore, Is.EqualTo(_flow.LastScore));
            Assert.That(_best.Saves, Is.EqualTo(1));
            Assert.That(_best.LastConfigVersion, Is.EqualTo("cfg-test"));
        }

        [Test]
        public void LowerScore_KeepsBest()
        {
            _best.Stored = 1e9;
            StartType1();
            HitObstacleOnce();

            Assert.That(_flow.LastScore, Is.Not.Null);
            Assert.That(_flow.IsNewBest, Is.False);
            Assert.That(_flow.BestScore, Is.EqualTo(1e9));
            Assert.That(_best.Saves, Is.EqualTo(0));
        }

        [Test]
        public void UnfinishedRun_DoesNotUpdateBest()
        {
            _query.OmitEliminated = true;
            _best.Stored = 5;
            StartType1();
            HitObstacleOnce();

            Assert.That(_flow.LastScore, Is.Null);
            Assert.That(_flow.IsNewBest, Is.False);
            Assert.That(_flow.BestScore, Is.EqualTo(5));
            Assert.That(_best.Saves, Is.EqualTo(0));
        }

        [Test]
        public void Retry_ClearsScores()
        {
            StartType1();
            HitObstacleOnce();

            _flow.Retry();

            Assert.That(_flow.LastScore, Is.Null);
            Assert.That(_flow.BestScore, Is.Null);
            Assert.That(_flow.IsNewBest, Is.False);
        }

        [Test]
        public void Shutdown_ClosesOpenRun()
        {
            StartType1();

            _flow.Shutdown();
            _flow.Shutdown();

            Assert.That(_run.State, Is.EqualTo(RunState.Idle));
            Assert.That(_log.Ended, Is.EqualTo(1));
        }

        /// <summary>RunController.Tick, TutorialController.Tick, GameFlowController.Tick 순서(GameBootstrapper와 같다).</summary>
        private void TFrames(int count)
        {
            for (int i = 0; i < count; i++)
            {
                _run.Tick(1);
                _tutorial.Tick(1);
                _flow.Tick(1);
            }
        }

        /// <summary>안내 구간을 끝까지 성공시켜 Done으로 만든다(TutorialControllerTests의 Advance와 같은 방식).</summary>
        private void DriveToDone()
        {
            for (int i = 0; i < 500 && _tutorial.Stage != TutorialStage.Done; i++)
            {
                var step = _tutorial.Step.Value;
                bool track = step <= TutorialStep.Rest;
                switch (_tutorial.Stage)
                {
                    case TutorialStage.Approach:
                        if (track)
                        {
                            _tutorial.TakePlacement(0);
                            _tutorial.NotifyTargetNear(0);
                        }
                        else
                        {
                            TFrames(1);
                        }
                        break;
                    case TutorialStage.Prompt:
                        _tutorial.Acknowledge();
                        break;
                    case TutorialStage.Trying:
                        if (_run.ResumeCountdownRemaining > 0 || _run.IsShowingAnswerFeedback)
                        {
                            TFrames(1);
                        }
                        else if (track)
                        {
                            _tutorial.ReportAttempt(new TutorialAttempt(false,
                                step == TutorialStep.Snack || step == TutorialStep.FoodRow, step == TutorialStep.Rest));
                        }
                        else
                        {
                            switch (_run.Phase)
                            {
                                case HypoPhase.Recognize: _run.PressMeter(); break;
                                case HypoPhase.Respond: _run.ChooseSnack("juice"); break;
                                case HypoPhase.Dose: _run.ChooseDose(DoseLevel.Proper); break;
                                case HypoPhase.Remeasure: _run.ChooseRecheck(); break;
                                case HypoPhase.Notify:
                                case HypoPhase.HyperNotify:
                                case HypoPhase.LowNotify:
                                    _run.PressHelp(_run.Phase);
                                    break;
                            }
                            TFrames(1);
                        }
                        break;
                    default:
                        TFrames(1);
                        break;
                }
            }
            Assert.That(_tutorial.Stage, Is.EqualTo(TutorialStage.Done), "안내 구간이 끝나야 한다 " + _tutorial.Step + " " + _run.Phase + " " + _run.Actual);
        }

        [Test]
        public void FirstTypeSelect_StartsTutorialRun()
        {
            _settings.Stored = null;
            _flow.Begin();

            _flow.SelectType(TrackType.Type1);
            _flow.StartRun();

            Assert.That(_flow.Screen, Is.EqualTo(FlowScreen.Running));
            Assert.That(_log.Begun, Is.EqualTo(0));
            Assert.That(_log.BegunUnrecorded, Is.EqualTo(1));
            Assert.That(_run.IsRecorded, Is.False);
            Assert.That(_tutorial.IsActive, Is.True);
            Assert.That(_tutorial.CanSkip, Is.False);
        }

        [Test]
        public void SameTypeReselect_DoesNotSetPending()
        {
            _flow.Begin();
            _flow.OpenSettings();
            _flow.SelectType(TrackType.Type1);
            _flow.StartRun();

            Assert.That(_settings.TutorialPending, Is.False);
            Assert.That(_log.Begun, Is.EqualTo(1));
            Assert.That(_tutorial.IsActive, Is.False);
        }

        [Test]
        public void DifferentTypeReselect_StartsTutorialRun()
        {
            _flow.Begin();
            _flow.OpenSettings();
            _flow.SelectType(TrackType.Type2);
            _flow.StartRun();

            Assert.That(_settings.TutorialPending, Is.True);
            Assert.That(_log.Begun, Is.EqualTo(0));
            Assert.That(_tutorial.IsActive, Is.True);
            Assert.That(_tutorial.Type, Is.EqualTo(TrackType.Type2));
        }

        [Test]
        public void GuidedComplete_SavesDoneAndClearsPending()
        {
            _settings.TutorialPending = true;
            StartType1();

            DriveToDone();

            Assert.That(_settings.TutorialDone, Contains.Item(TrackType.Type1));
            Assert.That(_settings.TutorialPending, Is.False);
            Assert.That(_settings.TutorialDone, Has.No.Member(TrackType.Type2));
        }

        [Test]
        public void StartTutorial_FromSettings()
        {
            _flow.Begin();
            _flow.StartTutorial();
            Assert.That(_flow.Screen, Is.EqualTo(FlowScreen.Title), "타이틀에서는 무시한다");
            Assert.That(_log.BegunUnrecorded, Is.EqualTo(0));

            _flow.OpenSettings();
            _flow.StartTutorial();

            Assert.That(_flow.Screen, Is.EqualTo(FlowScreen.Running));
            Assert.That(_log.BegunUnrecorded, Is.EqualTo(1));
            Assert.That(_log.Begun, Is.EqualTo(0));
            Assert.That(_tutorial.IsActive, Is.True);
            Assert.That(_tutorial.Type, Is.EqualTo(TrackType.Type1));
        }

        [Test]
        public void SkipTutorial_IgnoredWhenNotDoneOrAfterGuided()
        {
            // 끝낸 적 없는 유형: 무시한다.
            _settings.TutorialPending = true;
            StartType1();
            _flow.SkipTutorial();
            Assert.That(_log.Begun, Is.EqualTo(0));
            Assert.That(_log.BegunUnrecorded, Is.EqualTo(1));
            Assert.That(_tutorial.IsGuided, Is.True);
            Assert.That(_settings.TutorialPending, Is.True);

            // 끝낸 적 있는 유형: 기록하는 일반 판이 새로 열린다.
            SetUp();
            _settings.TutorialPending = true;
            _settings.TutorialDone.Add(TrackType.Type1);
            StartType1();
            Assert.That(_tutorial.CanSkip, Is.True);
            _flow.SkipTutorial();
            Assert.That(_log.Begun, Is.EqualTo(1));
            Assert.That(_run.IsRecorded, Is.True);
            Assert.That(_tutorial.IsActive, Is.False);
            Assert.That(_settings.TutorialPending, Is.False);
            Assert.That(_flow.Screen, Is.EqualTo(FlowScreen.Running));
            _flow.SkipTutorial();
            Assert.That(_log.Begun, Is.EqualTo(1), "일반 판에서는 무시한다");

            // 안내 구간을 마친 뒤(Done)에는 무시한다.
            SetUp();
            _settings.TutorialPending = true;
            _settings.TutorialDone.Add(TrackType.Type1);
            StartType1();
            DriveToDone();
            _flow.SkipTutorial();
            Assert.That(_log.Begun, Is.EqualTo(0));
            Assert.That(_log.BegunUnrecorded, Is.EqualTo(1));
            Assert.That(_tutorial.IsActive, Is.True);
        }

        [Test]
        public void TutorialResult_RetryStartsRecordedRun_BestUntouched()
        {
            _settings.TutorialPending = true;
            StartType1();
            DriveToDone();
            TFrames(10); // 재개 카운트다운과 유예가 지나야 장애물이 닿는다.

            _run.HitObstacle();
            TFrames(1);

            Assert.That(_flow.IsShowingResult, Is.True);
            Assert.That(_flow.IsTutorialResult, Is.True);
            Assert.That(_flow.LastScore, Is.Null);
            Assert.That(_flow.BestScore, Is.Null);
            Assert.That(_flow.LastResult, Is.Null);
            Assert.That(_flow.IsNewBest, Is.False);
            Assert.That(_best.Saves, Is.EqualTo(0));
            Assert.That(_query.RequestedRunId, Is.Null);
            Assert.That(_tutorial.IsActive, Is.False);

            _flow.Retry();

            Assert.That(_flow.IsTutorialResult, Is.False);
            Assert.That(_flow.Screen, Is.EqualTo(FlowScreen.Running));
            Assert.That(_log.Begun, Is.EqualTo(1));
            Assert.That(_run.IsRecorded, Is.True);
            Assert.That(_best.Saves, Is.EqualTo(0));
        }

        [Test]
        public void Shutdown_StopsTutorial()
        {
            _settings.TutorialPending = true;
            StartType1();

            _flow.Shutdown();

            Assert.That(_tutorial.IsActive, Is.False);
        }

        // 일시정지 메뉴

        [Test]
        public void OpenPauseMenu_WhileRunning_PausesRunAndHidesButton()
        {
            StartType1();
            Frames(2);
            Assert.That(_flow.CanPause, Is.True);

            _flow.OpenPauseMenu();

            Assert.That(_flow.IsPauseMenuOpen, Is.True);
            Assert.That(_flow.CanPause, Is.False);
            Assert.That(_run.State, Is.EqualTo(RunState.Paused));
            Frames(5);
            Assert.That(_run.WorldTime, Is.EqualTo(2).Within(1e-9), "메뉴가 떠 있는 동안 시계가 멈춘다");
        }

        [Test]
        public void PauseMenu_OutsideRun_IsIgnored()
        {
            _flow.Begin();

            _flow.OpenPauseMenu(true);

            Assert.That(_flow.IsPauseMenuOpen, Is.False);
            Assert.That(_flow.CanPause, Is.False);
        }

        [Test]
        public void CanPause_FalseWhileChoicePanelUp_ForceStillOpens()
        {
            StartType1();
            Frames(26);
            Assert.That(_run.Phase, Is.EqualTo(HypoPhase.Recognize));
            Assert.That(_flow.CanPause, Is.True, "1단계는 세계가 달린다");
            _run.PressMeter();
            Frames(1);
            Assert.That(_run.Phase, Is.EqualTo(HypoPhase.Respond));

            Assert.That(_flow.CanPause, Is.False);
            _flow.OpenPauseMenu();
            Assert.That(_flow.IsPauseMenuOpen, Is.False, "버튼이 숨은 동안은 열리지 않는다");

            _flow.OpenPauseMenu(true);
            Assert.That(_flow.IsPauseMenuOpen, Is.True, "앱에서 나가면 패널 중에도 연다");
            Assert.That(_run.State, Is.EqualTo(RunState.Paused));
        }

        [Test]
        public void ContinueFromPause_WorldWasRunning_CountsDown()
        {
            StartType1();
            Frames(2);
            _flow.OpenPauseMenu();

            _flow.ContinueFromPause();

            Assert.That(_flow.IsPauseMenuOpen, Is.False);
            Assert.That(_run.State, Is.EqualTo(RunState.Running));
            Assert.That(_run.ResumeCountdownRemaining, Is.EqualTo(PauseCountdownSec).Within(1e-9));
            Assert.That(_flow.CanPause, Is.False, "3-2-1 동안 버튼을 숨긴다");
            Frames(3);
            Assert.That(_run.WorldTime, Is.EqualTo(2).Within(1e-9));
            Assert.That(_flow.CanPause, Is.True);
        }

        [Test]
        public void ContinueFromPause_PanelWasUp_ResumesWithoutCountdown()
        {
            StartType1();
            Frames(26);
            _run.PressMeter();
            Frames(1);
            _flow.OpenPauseMenu(true);

            _flow.ContinueFromPause();

            Assert.That(_run.State, Is.EqualTo(RunState.Running));
            Assert.That(_run.ResumeCountdownRemaining, Is.EqualTo(0));
            Assert.That(_run.Phase, Is.EqualTo(HypoPhase.Respond));
        }

        [Test]
        public void QuitToTitle_EndsRunWithoutResultOrBest()
        {
            StartType1();
            Frames(2);
            _flow.OpenPauseMenu();

            _flow.QuitToTitle();

            Assert.That(_flow.Screen, Is.EqualTo(FlowScreen.Title));
            Assert.That(_flow.IsPauseMenuOpen, Is.False);
            Assert.That(_run.State, Is.EqualTo(RunState.Idle));
            Assert.That(_log.Ended, Is.EqualTo(1));
            Assert.That(_query.RequestedRunId, Is.Null, "결과를 계산하지 않는다");
            Assert.That(_best.Saves, Is.EqualTo(0));
            Assert.That(_flow.LastScore, Is.Null);
        }

        [Test]
        public void QuitToTitle_MenuClosed_IsIgnored()
        {
            StartType1();

            _flow.QuitToTitle();
            _flow.ContinueFromPause();

            Assert.That(_flow.Screen, Is.EqualTo(FlowScreen.Running));
            Assert.That(_run.State, Is.EqualTo(RunState.Running));
        }

        [Test]
        public void QuitToTitle_TutorialRun_KeepsTutorialPending()
        {
            _settings.TutorialPending = true;
            StartType1();
            Assert.That(_tutorial.IsActive, Is.True);
            _flow.OpenPauseMenu(true);

            _flow.QuitToTitle();

            Assert.That(_tutorial.IsActive, Is.False);
            Assert.That(_settings.TutorialPending, Is.True, "다음 시작하기도 튜토리얼 판이다");
            Assert.That(_flow.Screen, Is.EqualTo(FlowScreen.Title));
        }

        [Test]
        public void SkipTutorial_WithPauseMenuOpen_IsIgnored()
        {
            _settings.TutorialPending = true;
            _settings.TutorialDone.Add(TrackType.Type1);
            StartType1();
            _flow.OpenPauseMenu(true);

            _flow.SkipTutorial();

            Assert.That(_log.Begun, Is.EqualTo(0));
            Assert.That(_tutorial.IsActive, Is.True);
            Assert.That(_flow.IsPauseMenuOpen, Is.True);
        }

        [Test]
        public void Shutdown_WithPauseMenuOpen_ClosesMenuAndRun()
        {
            StartType1();
            _flow.OpenPauseMenu();

            _flow.Shutdown();

            Assert.That(_flow.IsPauseMenuOpen, Is.False);
            Assert.That(_log.Ended, Is.EqualTo(1));
        }

        // 탈락 장면

        [Test]
        public void Elimination_ShowsSceneThenResult()
        {
            SetUpWith(new ConfigBuilder { RunDecayPerSec = 2 }, 2.5);
            StartType1();

            HitObstacleOnce();

            Assert.That(_flow.Screen, Is.EqualTo(FlowScreen.Elimination));
            Assert.That(_flow.IsShowingElimination, Is.True);
            Assert.That(_flow.EliminatedBy, Is.EqualTo(EliminationCause.ObstacleHit));
            Assert.That(_run.State, Is.EqualTo(RunState.Eliminated), "장면 동안 주행을 열어 둔다");
            Assert.That(_log.Ended, Is.EqualTo(0));
            Assert.That(_flow.CanPause, Is.False);

            Frames(2);
            Assert.That(_flow.Screen, Is.EqualTo(FlowScreen.Elimination));
            Frames(1);
            Assert.That(_flow.IsShowingResult, Is.True);
            Assert.That(_flow.EliminatedBy, Is.Null);
            Assert.That(_log.Ended, Is.EqualTo(1));
            Assert.That(_flow.LastScore, Is.Not.Null);
        }

        [Test]
        public void Elimination_PauseMenuAndRetryIgnored()
        {
            SetUpWith(new ConfigBuilder { RunDecayPerSec = 2 }, 2.5);
            StartType1();
            HitObstacleOnce();

            _flow.OpenPauseMenu(true);
            _flow.Retry();
            _flow.BackToTitle();

            Assert.That(_flow.IsPauseMenuOpen, Is.False);
            Assert.That(_flow.Screen, Is.EqualTo(FlowScreen.Elimination));
        }

        [Test]
        public void Elimination_TutorialRun_ShowsSceneThenTutorialResult()
        {
            SetUpWith(new ConfigBuilder { RunDecayPerSec = 2 }, 2.5);
            _settings.TutorialPending = true;
            StartType1();
            DriveToDone();
            TFrames(10); // 재개 카운트다운과 유예가 지나야 장애물이 닿는다.
            _run.HitObstacle();
            TFrames(1);

            Assert.That(_flow.Screen, Is.EqualTo(FlowScreen.Elimination));
            TFrames(3);
            Assert.That(_flow.IsShowingResult, Is.True);
            Assert.That(_flow.IsTutorialResult, Is.True);
        }

        [Test]
        public void Shutdown_DuringEliminationScene_ClosesLog()
        {
            SetUpWith(new ConfigBuilder { RunDecayPerSec = 2 }, 2.5);
            StartType1();
            HitObstacleOnce();

            _flow.Shutdown();

            Assert.That(_log.Ended, Is.EqualTo(1));
        }
    }
}
