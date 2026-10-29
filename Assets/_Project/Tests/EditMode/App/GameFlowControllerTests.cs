using System;
using System.Collections.Generic;
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
            public string RequestedRunId;
            public int EndedWhenQueried = -1;

            public FakeQuery(FakeLog log) => _log = log;

            public IReadOnlyList<string> ListRunIds() => throw new NotSupportedException();

            public RunRecord LoadRun(string runId)
            {
                RequestedRunId = runId;
                EndedWhenQueried = _log.Ended;
                return ReturnsNothing ? null : new RunRecord(runId, "test", null, null, Published.ToArray(), 0);
            }
        }

        private FakeLog _log;
        private FakeQuery _query;
        private RunController _run;
        private GameFlowController _flow;

        // 달리기 감소 2/s: 26초에 저혈당 발동, 아무것도 안 하면 36초에 탈락.
        [SetUp]
        public void SetUp() => SetUpWith(new ConfigBuilder { RunDecayPerSec = 2, RunDurationSec = 10 });

        private void SetUpWith(ConfigBuilder builder)
        {
            var config = builder.Build();
            var bus = new DomainEventBus();
            _log = new FakeLog();
            _query = new FakeQuery(_log);
            bus.Subscribe<HypoEventTriggered>(e => _query.Published.Add(e));
            bus.Subscribe<StepAnswered>(e => _query.Published.Add(e));
            bus.Subscribe<RunEliminated>(e => _query.Published.Add(e));
            bus.Subscribe<RunCompleted>(e => _query.Published.Add(e));
            _run = new RunController(config.Runner, () =>
            {
                var model = new GlucoseModel(config.Glucose);
                var director = new EventDirector(config.Glucose, model, new NoRandom(), bus);
                var fsm = new HypoEventFsm(config, model, director, bus);
                return new RunSession(model, director, fsm, bus.Subscribe<HypoEventTriggered>(fsm.Begin));
            }, _log, bus);
            _flow = new GameFlowController(_run, _query);
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
        public void Completed_ClosesLogThenLoadsResult()
        {
            _flow.Begin();
            Frames(10);

            Assert.That(_flow.IsShowingResult, Is.True);
            Assert.That(_run.State, Is.EqualTo(RunState.Idle));
            Assert.That(_query.RequestedRunId, Is.EqualTo("run-1"));
            Assert.That(_query.EndedWhenQueried, Is.EqualTo(1), "기록을 닫은 뒤 읽는다");
            Assert.That(_flow.LastResult.Outcome, Is.EqualTo(RunOutcome.Completed));
            Assert.That(_flow.LastResult.Events, Is.Empty);

            Frames(3);
            Assert.That(_log.Ended, Is.EqualTo(1), "결과 화면에서는 다시 닫지 않는다");
        }

        [Test]
        public void Eliminated_ShowsResultWithFailedEvent()
        {
            var builder = new ConfigBuilder { RunDecayPerSec = 2 };
            Assert.That(builder.RunDurationSec, Is.GreaterThan(36), "탈락 전에 완주하지 않아야 한다");
            SetUpWith(builder);
            _flow.Begin();

            Frames(36);

            var result = _flow.LastResult;
            Assert.That(result.Outcome, Is.EqualTo(RunOutcome.Eliminated));
            Assert.That(result.Events, Has.Count.EqualTo(1));
            Assert.That(result.Events[0].AllCorrect, Is.False);
            Assert.That(result.Events[0].Steps[MetricStep.Recognize], Is.False);
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
            Frames(10);
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

            Frames(10);

            Assert.That(_flow.IsShowingResult, Is.True);
            Assert.That(_flow.LastResult, Is.Null);
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
