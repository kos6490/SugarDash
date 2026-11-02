using System;
using System.Collections.Generic;
using System.Linq;
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

            public string BeginRun() => "run-" + ++Begun;

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

        private FakeBest _best;
        private FakeLog _log;
        private FakeQuery _query;
        private RunController _run;
        private GameFlowController _flow;

        // 달리기 감소 2/s: 26초에 저혈당 발동, 아무것도 안 하면 36초에 탈락.
        [SetUp]
        public void SetUp() => SetUpWith(new ConfigBuilder { RunDecayPerSec = 2 });

        private void SetUpWith(ConfigBuilder builder)
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
            bus.Subscribe<ObstacleHit>(e => _query.Published.Add(e));
            _run = new RunController(config.Runner, config.Sections, () =>
            {
                var model = new GlucoseModel(config.Glucose);
                var director = new EventDirector(config.Glucose, model, new NoRandom(), bus);
                var fsm = new HypoEventFsm(config, model, director, bus, () => _run.DistanceM);
                var scoring = new SugarDash.Domain.Scoring.ScoringService(config.Score);
                return new RunSession(model, director, fsm, scoring, bus.Subscribe<HypoEventTriggered>(fsm.Begin),
                    scoring.Subscribe(bus));
            }, _log, bus);
            _best = new FakeBest();
            _flow = new GameFlowController(_run, _query, config.Score, _best, "cfg-test");
        }

        /// <summary>GameBootstrapper처럼 RunController.Tick 뒤 GameFlowController.Tick을 부른다.</summary>
        private void Frames(int count)
        {
            for (int i = 0; i < count; i++)
            {
                _run.Tick(1);
                _flow.Tick();
            }
        }

        /// <summary>하트(기본 3)를 모두 잃게 해 탈락시킨다. 저혈당 발동(26초) 전에 끝난다.</summary>
        private void LoseAllHearts()
        {
            for (int i = 0; i < 3; i++)
            {
                _run.HitObstacle();
                Frames(1);
            }
        }

        [Test]
        public void Begin_StartsRunOnce()
        {
            _flow.Begin();
            _flow.Begin();

            Assert.That(_run.State, Is.EqualTo(RunState.Running));
            Assert.That(_log.Begun, Is.EqualTo(1));
            Assert.That(_flow.IsShowingResult, Is.False);
            Assert.That(_flow.CanRetry, Is.False);
        }

        [Test]
        public void HeartsLost_ClosesLogThenLoadsResult()
        {
            _flow.Begin();
            LoseAllHearts();

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
            _flow.Begin();

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
            _flow.Begin();
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
                _flow.Tick();
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
            _flow.Begin();
            Frames(3);

            _flow.Retry();

            Assert.That(_log.Begun, Is.EqualTo(1));
            Assert.That(_run.WorldTime, Is.EqualTo(3));
        }

        [Test]
        public void Retry_FromResult_StartsFreshRun()
        {
            _flow.Begin();
            LoseAllHearts();
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
            _flow.Begin();

            LoseAllHearts();

            Assert.That(_flow.IsShowingResult, Is.True);
            Assert.That(_flow.LastResult, Is.Null);
        }

        [Test]
        public void FirstRun_IsNewBestAndSaved()
        {
            _flow.Begin();
            LoseAllHearts();

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
            _flow.Begin();
            LoseAllHearts();

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
            _flow.Begin();
            LoseAllHearts();

            Assert.That(_flow.LastScore, Is.Null);
            Assert.That(_flow.IsNewBest, Is.False);
            Assert.That(_flow.BestScore, Is.EqualTo(5));
            Assert.That(_best.Saves, Is.EqualTo(0));
        }

        [Test]
        public void Retry_ClearsScores()
        {
            _flow.Begin();
            LoseAllHearts();

            _flow.Retry();

            Assert.That(_flow.LastScore, Is.Null);
            Assert.That(_flow.BestScore, Is.Null);
            Assert.That(_flow.IsNewBest, Is.False);
        }

        [Test]
        public void Shutdown_ClosesOpenRun()
        {
            _flow.Begin();

            _flow.Shutdown();
            _flow.Shutdown();

            Assert.That(_run.State, Is.EqualTo(RunState.Idle));
            Assert.That(_log.Ended, Is.EqualTo(1));
        }
    }
}
