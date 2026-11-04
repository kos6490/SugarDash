using System;
using System.Collections.Generic;
using NUnit.Framework;
using SugarDash.Domain.Config;
using SugarDash.Domain.Director;
using SugarDash.Domain.Events;
using SugarDash.Domain.Glucose;
using SugarDash.Domain.Ports;

namespace SugarDash.Domain.Tests
{
    /// <summary>EventDirector 2형 모드의 hold 누적, notice, tell 발동(설계 4.3절).</summary>
    public class Type2EventDirectorTests
    {
        private sealed class FakeRandom : IRandomSource
        {
            public double NextDouble() => 0;
        }

        private GlucoseModel _model;
        private EventDirector _director;
        private List<object> _events;
        private ConfigBuilder _builder;

        // 기본: 고혈당 250(해제 230), 저혈당 70(해제 80), hold 2초, notice 1초. 혈당은 Initial로 맞춘다.
        private void Setup(Action<ConfigBuilder> configure = null)
        {
            _builder = new ConfigBuilder();
            configure?.Invoke(_builder);
            // BuildType2는 급하강 등 1형 값을 중립으로 만들므로, 급하강 시나리오를 위해 1형 GlucoseConfig를 쓴다.
            Wire(_builder.BuildGlucose(), _builder.BuildType2().Type2);
        }

        private void Wire(GlucoseConfig glucose, Type2Config type2)
        {
            var bus = new DomainEventBus();
            _events = new List<object>();
            bus.Subscribe<NoticeStarted>(e => _events.Add(e));
            bus.Subscribe<HyperEventTriggered>(e => _events.Add(e));
            bus.Subscribe<LowEventTriggered>(e => _events.Add(e));
            bus.Subscribe<HypoEventTriggered>(e => _events.Add(e));
            bus.Subscribe<SuddenDropStarted>(e => _events.Add(e));
            bus.Subscribe<GaugeWarningRaised>(e => _events.Add(e));
            _model = new GlucoseModel(glucose);
            _director = new EventDirector(glucose, _model, new FakeRandom(), bus, type2);
        }

        private void Run(double seconds, double step = 0.5)
        {
            for (double t = 0; t < seconds - 1e-9; t += step)
            {
                _model.Tick(step, step, step, 0);
                _director.Evaluate(step);
            }
        }

        private int Count<T>() => _events.FindAll(e => e is T).Count;

        [Test]
        public void HighHold_StartsNoticeThenTellAfterNoticeSec()
        {
            Setup(b => b.Initial = 260);
            Run(1.5);
            Assert.That(Count<NoticeStarted>(), Is.EqualTo(0));

            Run(0.5); // 2초
            var notice = (NoticeStarted)_events.Find(e => e is NoticeStarted);
            Assert.That(notice.Kind, Is.EqualTo(NoticeKind.High));
            Assert.That(notice.EventId, Is.EqualTo(1));
            Assert.That(Count<HyperEventTriggered>(), Is.EqualTo(0));

            Run(0.5);
            Assert.That(Count<HyperEventTriggered>(), Is.EqualTo(0));

            Run(0.5); // notice 시작 1초 뒤
            var tell = (HyperEventTriggered)_events.Find(e => e is HyperEventTriggered);
            Assert.That(tell.EventId, Is.EqualTo(notice.EventId));
            Assert.That(_director.IsHyperEventActive, Is.True);
        }

        [Test]
        public void HighHold_ResetsWhenDippingBelow()
        {
            Setup(b =>
            {
                b.Initial = 255;
                b.SuddenDropAmount = 20;
                b.SuddenDropDurationSec = 0.5;
                b.Snacks.Add(new ConfigBuilder.Snack
                    { Id = "burst", Kind = SnackKind.Fast, TotalRise = 40, OnsetSec = 0, AbsorbSec = 0.5 });
            });
            Run(1.5); // 위 1.5초
            _model.StartSuddenDrop();
            Run(0.5); // 235로 내려가 hold가 0이 된다
            Assert.That(_model.Actual, Is.LessThan(250));
            _model.EatSnack("burst", 0.5); // +20
            Run(1.5); // 다시 위 1.5초: 이어졌다면 notice가 났을 시간
            Assert.That(_model.Actual, Is.GreaterThanOrEqualTo(250));
            Assert.That(Count<NoticeStarted>(), Is.EqualTo(0));

            Run(0.5); // 2초 채움
            Assert.That(Count<NoticeStarted>(), Is.EqualTo(1));
        }

        [Test]
        public void LowHold_StartsNoticeThenLowEventTriggered()
        {
            Setup(b => b.Initial = 60);
            Run(2);
            var notice = (NoticeStarted)_events.Find(e => e is NoticeStarted);
            Assert.That(notice.Kind, Is.EqualTo(NoticeKind.Low));
            Assert.That(Count<LowEventTriggered>(), Is.EqualTo(0));

            Run(1);
            var tell = (LowEventTriggered)_events.Find(e => e is LowEventTriggered);
            Assert.That(tell.EventId, Is.EqualTo(notice.EventId));
            Assert.That(Count<HypoEventTriggered>(), Is.EqualTo(0));
        }

        [Test]
        public void NoticeContinuesToTell_EvenIfGlucoseReturns()
        {
            Setup(b =>
            {
                b.Initial = 255;
                b.SuddenDropAmount = 40;
                b.SuddenDropDurationSec = 0.5;
            });
            Run(2); // notice 시작
            Assert.That(Count<NoticeStarted>(), Is.EqualTo(1));
            _model.StartSuddenDrop();
            Run(0.5); // 215로 기준 아래
            Assert.That(_model.Actual, Is.LessThan(250));
            Run(0.5); // notice 시작 1초 뒤
            Assert.That(Count<HyperEventTriggered>(), Is.EqualTo(1));
        }

        [Test]
        public void ZeroHoldAndNotice_TriggersTellSameEvaluate()
        {
            Setup(b =>
            {
                b.Initial = 260;
                b.HighHoldSec = 0;
                b.NoticeSec = 0;
            });
            Run(0.5);
            Assert.That(_events.Count, Is.EqualTo(2));
            Assert.That(_events[0], Is.TypeOf<NoticeStarted>());
            Assert.That(_events[1], Is.TypeOf<HyperEventTriggered>());
            Assert.That(((HyperEventTriggered)_events[1]).EventId, Is.EqualTo(((NoticeStarted)_events[0]).EventId));
        }

        [Test]
        public void NoHoldDuringTellOrGrace_AndRearmAfterRelease()
        {
            Setup(b =>
            {
                b.Initial = 60;
                b.SuddenDropAmount = 40;
                b.SuddenDropDurationSec = 0.5;
                b.Snacks.Add(new ConfigBuilder.Snack
                    { Id = "burst", Kind = SnackKind.Fast, TotalRise = 40, OnsetSec = 0, AbsorbSec = 0.5 });
            });
            Run(3); // notice 2초 + tell
            Assert.That(Count<LowEventTriggered>(), Is.EqualTo(1));
            Run(5); // tell 중에는 누적도 새 notice도 없다
            Assert.That(Count<NoticeStarted>(), Is.EqualTo(1));

            _director.NotifyLowEventEnded();
            Run(5); // 유예 중(NotifyGraceEnded 전)에도 없다
            Assert.That(Count<NoticeStarted>(), Is.EqualTo(1));

            _director.NotifyGraceEnded();
            Run(5); // 해제 수준(80)에 닿기 전이라 다시 누적하지 않는다
            Assert.That(Count<NoticeStarted>(), Is.EqualTo(1));

            _model.EatSnack("burst", 1); // +40 -> 100, 해제 수준 이상
            Run(0.5);
            Assert.That(_model.Actual, Is.GreaterThanOrEqualTo(80));
            _model.StartSuddenDrop(); // 다시 60으로
            Run(2.5);
            Assert.That(_model.Actual, Is.LessThan(70));
            Assert.That(Count<NoticeStarted>(), Is.EqualTo(2));
        }

        [Test]
        public void Type2_NoSuddenDropNoGaugeWarningNoHypoEvent()
        {
            var type1 = new ConfigBuilder { SuddenDropChancePerSec = 1, WarningThreshold = 1000, Initial = 60 }
                .BuildGlucose();
            Wire(type1, new ConfigBuilder().BuildType2().Type2);
            Run(1.5); // hold 2초 미만
            Assert.That(Count<SuddenDropStarted>(), Is.EqualTo(0));
            Assert.That(Count<GaugeWarningRaised>(), Is.EqualTo(0));
            Assert.That(Count<HypoEventTriggered>(), Is.EqualTo(0));
            Assert.That(_director.IsGaugeWarningOn, Is.False);
            Assert.That(_model.IsSuddenDropActive, Is.False);
        }

        [Test]
        public void ActiveSignal_SetFromNoticeUntilTellEnds()
        {
            Setup(b => b.Initial = 260);
            Assert.That(_director.ActiveSignal, Is.Null);
            Run(1.5);
            Assert.That(_director.ActiveSignal, Is.Null);
            Run(0.5); // notice
            Assert.That(_director.ActiveSignal, Is.EqualTo(NoticeKind.High));
            Run(1);   // tell
            Assert.That(_director.ActiveSignal, Is.EqualTo(NoticeKind.High));
            _director.NotifyHyperEventEnded();
            Assert.That(_director.ActiveSignal, Is.Null);
        }

        [Test]
        public void NotifyLowEventEnded_WithoutActiveLow_Throws()
        {
            Setup();
            Assert.Throws<InvalidOperationException>(() => _director.NotifyLowEventEnded());
        }

        [Test]
        public void Type2_Gate_HighOnly_AccumulatesHighOnly()
        {
            Setup(b => b.Initial = 260);
            _director.SetTriggerGate(TriggerGate.LowOnly);
            Run(5);
            Assert.That(Count<NoticeStarted>(), Is.EqualTo(0));

            _director.SetTriggerGate(TriggerGate.HighOnly);
            Run(1.5); // 누적은 이때부터 시작한다
            Assert.That(Count<NoticeStarted>(), Is.EqualTo(0));
            Run(0.5);
            Assert.That(Count<NoticeStarted>(), Is.EqualTo(1));
        }

        [Test]
        public void Type2_Gate_HighOnly_BlocksLow()
        {
            Setup(b => b.Initial = 60);
            _director.SetTriggerGate(TriggerGate.HighOnly);
            Run(5);
            Assert.That(Count<NoticeStarted>(), Is.EqualTo(0));
        }
    }
}
