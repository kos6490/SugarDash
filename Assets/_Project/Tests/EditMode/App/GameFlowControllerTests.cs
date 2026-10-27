using System;
using SugarDash.Domain.Director;
using SugarDash.Domain.Events;
using SugarDash.Domain.Glucose;
using SugarDash.Domain.Hypo;
using SugarDash.Domain.Ports;
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

        private FakeLog _log;
        private RunController _run;
        private GameFlowController _flow;

        // 달리기 감소 2/s: 26초에 저혈당 발동, 아무것도 안 하면 36초에 탈락.
        [SetUp]
        public void SetUp() => SetUpWith(new ConfigBuilder { RunDecayPerSec = 2, RunDurationSec = 10 });

        private void Ticks(int count)
        {
            for (int i = 0; i < count; i++) _run.Tick(1);
        }

        [Test]
        public void Begin_StartsRunOnce()
        {
            _flow.Begin();
            _flow.Begin();

            Assert.That(_run.State, Is.EqualTo(RunState.Running));
            Assert.That(_log.Begun, Is.EqualTo(1));
            Assert.That(_flow.CanRetry, Is.False);
        }

        [Test]
        public void Retry_WhileRunning_IsIgnored()
        {
            _flow.Begin();
            Ticks(3);

            _flow.Retry();

            Assert.That(_log.Begun, Is.EqualTo(1));
            Assert.That(_run.WorldTime, Is.EqualTo(3));
        }

        [Test]
        public void Retry_AfterCompleted_ClosesLogAndStartsFreshRun()
        {
            _flow.Begin();
            Ticks(10);
            Assert.That(_flow.CanRetry, Is.True);

            _flow.Retry();

            Assert.That(_log.Ended, Is.EqualTo(1));
            Assert.That(_log.Begun, Is.EqualTo(2));
            Assert.That(_run.State, Is.EqualTo(RunState.Running));
            Assert.That(_run.WorldTime, Is.EqualTo(0));
        }

        [Test]
        public void Retry_AfterEliminated_StartsFreshRun()
        {
            var builder = new ConfigBuilder { RunDecayPerSec = 2 };
            Assert.That(builder.RunDurationSec, Is.GreaterThan(36), "탈락 전에 완주하지 않아야 한다");
            SetUpWith(builder);
            _flow.Begin();
            Ticks(36);
            Assert.That(_run.State, Is.EqualTo(RunState.Eliminated));

            _flow.Retry();

            Assert.That(_run.State, Is.EqualTo(RunState.Running));
            Assert.That(_run.Phase, Is.EqualTo(HypoPhase.Idle));
            Assert.That(_run.Actual, Is.EqualTo(120));
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

        private void SetUpWith(ConfigBuilder builder)
        {
            var config = builder.Build();
            var bus = new DomainEventBus();
            _log = new FakeLog();
            _run = new RunController(config.Runner, () =>
            {
                var model = new GlucoseModel(config.Glucose);
                var director = new EventDirector(config.Glucose, model, new NoRandom(), bus);
                var fsm = new HypoEventFsm(config, model, director, bus);
                return new RunSession(model, director, fsm, bus.Subscribe<HypoEventTriggered>(fsm.Begin));
            }, _log, bus);
            _flow = new GameFlowController(_run);
        }
    }
}
