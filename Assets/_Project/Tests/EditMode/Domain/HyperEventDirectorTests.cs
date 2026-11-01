using System;
using System.Collections.Generic;
using SugarDash.Domain.Director;
using SugarDash.Domain.Events;
using SugarDash.Domain.Glucose;
using SugarDash.Domain.Ports;
using NUnit.Framework;

namespace SugarDash.Domain.Tests
{
    /// <summary>EventDirector의 고혈당 발동·재무장 판정(docs/hyper-event.md 2절).</summary>
    public class HyperEventDirectorTests
    {
        private sealed class FakeRandom : IRandomSource
        {
            private readonly Queue<double> _values;
            public FakeRandom(params double[] values) => _values = new Queue<double>(values);
            public double NextDouble() =>
                _values.Count > 0 ? _values.Dequeue() : throw new InvalidOperationException("난수가 더 필요하지 않아야 한다");
        }

        private GlucoseModel _model;
        private EventDirector _director;
        private List<HypoEventTriggered> _hypos;
        private List<HyperEventTriggered> _hypers;
        private List<SuddenDropStarted> _drops;

        // 기본: 고혈당 임계값 250, 해제 여유분 20(해제 수준 230). 주스 1인분 = +30, 1초 뒤부터 2초 동안 흡수.
        private void Setup(Action<ConfigBuilder> configure, params double[] randoms)
        {
            var builder = new ConfigBuilder();
            configure?.Invoke(builder);
            var config = builder.BuildGlucose();
            var bus = new DomainEventBus();
            _hypos = new List<HypoEventTriggered>();
            _hypers = new List<HyperEventTriggered>();
            _drops = new List<SuddenDropStarted>();
            bus.Subscribe<HypoEventTriggered>(_hypos.Add);
            bus.Subscribe<HyperEventTriggered>(_hypers.Add);
            bus.Subscribe<SuddenDropStarted>(_drops.Add);
            _model = new GlucoseModel(config);
            _director = new EventDirector(config, _model, new FakeRandom(randoms), bus);
        }

        private void Run(int seconds)
        {
            for (int i = 0; i < seconds; i++)
            {
                _model.Tick(1, 1, 1, 0);
                _director.Evaluate(1);
            }
        }

        [Test]
        public void AtOrAboveThreshold_TriggersOnce()
        {
            Setup(null);
            _model.EatSnack("juice", 5); // +150

            Run(2); // 195
            Assert.That(_hypers, Is.Empty);

            Run(1); // 270
            Assert.That(_hypers, Has.Count.EqualTo(1));
            Assert.That(_hypers[0].EventId, Is.EqualTo(1));
            Assert.That(_hypers[0].Actual, Is.EqualTo(270).Within(1e-9));
            Assert.That(_hypers[0].SimT, Is.EqualTo(3).Within(1e-9));
            Assert.That(_director.IsHyperEventActive, Is.True);

            Run(3);
            Assert.That(_hypers, Has.Count.EqualTo(1));
        }

        [Test]
        public void AfterEnded_RearmsOnlyBelowReleaseLevel()
        {
            Setup(b => b.BaseDecayPerSec = 1);
            _model.EatSnack("juice", 5);
            Run(3); // 267
            _director.NotifyHyperEventEnded();
            _director.NotifyGraceEnded();
            Assert.That(_director.IsHyperEventActive, Is.False);

            Run(10); // 257, 임계값 이상이지만 해제 수준 아래로 내려온 적이 없다
            Assert.That(_hypers, Has.Count.EqualTo(1), "해제 수준 아래로 내려오기 전에는 다시 발동하지 않는다");

            Run(28); // 229, 해제 수준(230) 아래 → 재무장
            _model.EatSnack("juice", 1);
            Run(3); // 228 → 242 → 256
            Assert.That(_hypers, Has.Count.EqualTo(2));
            Assert.That(_hypers[1].EventId, Is.EqualTo(2));
        }

        [Test]
        public void DuringHypoEvent_DoesNotTrigger()
        {
            Setup(b => { b.Initial = 72; b.BaseDecayPerSec = 1; });
            Run(3); // 69, 저혈당 발동
            _model.EatSnack("juice", 7); // +210

            Run(3); // 276

            Assert.That(_director.IsHypoEventActive, Is.True);
            Assert.That(_hypers, Is.Empty);
        }

        [Test]
        public void DuringGrace_IsDeferredUntilGraceEnds_AndSharesEventNumbers()
        {
            Setup(b => { b.Initial = 72; b.BaseDecayPerSec = 1; });
            Run(3);
            _director.NotifyHypoEventEnded();
            _model.EatSnack("juice", 7);

            Run(3); // 276, 유예 중
            Assert.That(_hypers, Is.Empty);
            Assert.That(_director.IsHyperTriggerPending, Is.False);

            _director.NotifyGraceEnded();
            Assert.That(_director.IsHyperTriggerPending, Is.True, "다음 판정에서 발동한다");
            Run(1);
            Assert.That(_hypers, Has.Count.EqualTo(1));
            Assert.That(_director.IsHyperTriggerPending, Is.False);
            Assert.That(_hypers[0].EventId, Is.EqualTo(2), "저혈당 이벤트와 번호를 함께 쓴다");
        }

        [Test]
        public void NotifyHyperEventEnded_Twice_Throws()
        {
            Setup(null);
            _model.EatSnack("juice", 5);
            Run(3);
            _director.NotifyHyperEventEnded();

            Assert.Throws<InvalidOperationException>(() => _director.NotifyHyperEventEnded());
            Assert.DoesNotThrow(() => _director.NotifyGraceEnded());
        }

        [Test]
        public void NotifyHyperEventEnded_WithoutActiveEvent_Throws()
        {
            Setup(null);

            Assert.Throws<InvalidOperationException>(() => _director.NotifyHyperEventEnded());
        }

        [Test]
        public void DuringHyperEvent_SuddenDropIsNotEvaluated()
        {
            // 발동 전 3번만 난수를 읽는다(0.9 ≥ p=0.5라 급하강 없음). 이후 읽으면 FakeRandom이 예외를 낸다.
            Setup(b => b.SuddenDropChancePerSec = 0.5, 0.9, 0.9, 0.9);
            _model.EatSnack("juice", 5);
            Run(3);
            Assert.That(_director.IsHyperEventActive, Is.True);

            Assert.DoesNotThrow(() => Run(3));
            Assert.That(_drops, Is.Empty);
        }
    }
}
