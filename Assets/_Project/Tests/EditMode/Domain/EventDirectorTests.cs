using System;
using System.Collections.Generic;
using SugarDash.Domain.Director;
using SugarDash.Domain.Events;
using SugarDash.Domain.Glucose;
using SugarDash.Domain.Ports;
using NUnit.Framework;

namespace SugarDash.Domain.Tests
{
    public class EventDirectorTests
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
        private List<SuddenDropStarted> _drops;
        private List<HyperEventTriggered> _hypers;
        private List<GaugeWarningRaised> _warnings;
        private List<GlucoseRangeEntered> _entered;
        private List<GlucoseRangeExited> _exited;

        private void Setup(Action<ConfigBuilder> configure, params double[] randoms)
        {
            var builder = new ConfigBuilder();
            configure?.Invoke(builder);
            var config = builder.BuildGlucose();
            var bus = new DomainEventBus();
            _hypos = new List<HypoEventTriggered>();
            _drops = new List<SuddenDropStarted>();
            bus.Subscribe<HypoEventTriggered>(_hypos.Add);
            bus.Subscribe<SuddenDropStarted>(_drops.Add);
            _hypers = new List<HyperEventTriggered>();
            bus.Subscribe<HyperEventTriggered>(_hypers.Add);
            _warnings = new List<GaugeWarningRaised>();
            bus.Subscribe<GaugeWarningRaised>(_warnings.Add);
            _entered = new List<GlucoseRangeEntered>();
            _exited = new List<GlucoseRangeExited>();
            bus.Subscribe<GlucoseRangeEntered>(_entered.Add);
            bus.Subscribe<GlucoseRangeExited>(_exited.Add);
            _model = new GlucoseModel(config);
            _director = new EventDirector(config, _model, new FakeRandom(randoms), bus);
        }

        /// <summary>1초씩 진행하며 매번 판정한다. 급하강 확률은 0으로 둔 설정에서만 쓴다.</summary>
        private void Run(int seconds)
        {
            for (int i = 0; i < seconds; i++)
            {
                _model.Tick(1, 1, 1, 0);
                _director.Evaluate(1);
            }
        }

        // 저혈당 판정 테스트는 급하강 확률 0. p = 0이면 난수를 읽어도 발동하지 않는다.
        private static double[] Never(int count)
        {
            var values = new double[count];
            for (int i = 0; i < count; i++) values[i] = 0.5;
            return values;
        }

        [Test]
        public void AboveThreshold_DoesNotTrigger()
        {
            Setup(b => b.BaseDecayPerSec = 1, Never(10));

            Run(10); // 120 → 110

            Assert.That(_hypos, Is.Empty);
        }

        [Test]
        public void BelowThreshold_TriggersOnce()
        {
            Setup(b => { b.Initial = 72; b.BaseDecayPerSec = 1; }, Never(10));

            Run(3); // 69
            Run(2);

            Assert.That(_hypos, Has.Count.EqualTo(1));
            Assert.That(_hypos[0].IsRecurrence, Is.False);
            Assert.That(_hypos[0].Actual, Is.EqualTo(69).Within(1e-9));
            Assert.That(_hypos[0].SimT, Is.EqualTo(3).Within(1e-9));
            Assert.That(_director.IsHypoEventActive, Is.True);
        }

        [Test]
        public void StillLowAfterCooldown_TriggersRecurrence()
        {
            Setup(b => { b.Initial = 72; b.BaseDecayPerSec = 1; }, Never(30));
            Run(3);
            _director.NotifyHypoEventEnded();
            _director.NotifyGraceEnded();

            Run(9); // cooldown 10 미만
            Assert.That(_hypos, Has.Count.EqualTo(1));

            Run(1);
            Assert.That(_hypos, Has.Count.EqualTo(2));
            Assert.That(_hypos[1].IsRecurrence, Is.True);
        }

        [Test]
        public void RecoveringAboveReleaseLevel_RearmsAsNormalEvent()
        {
            Setup(b => { b.Initial = 72; b.BaseDecayPerSec = 1; }, Never(40));
            Run(3); // 69
            _director.NotifyHypoEventEnded();
            _director.NotifyGraceEnded();
            _model.EatSnack("juice", 1);

            Run(3); // 69 - 3 + 30 = 96 ≥ 80 → 재무장
            Run(27); // 69, cooldown 이미 끝남

            Assert.That(_hypos, Has.Count.EqualTo(2));
            Assert.That(_hypos[1].IsRecurrence, Is.False);
        }

        [Test]
        public void BetweenThresholdAndReleaseLevel_WaitsThenTriggersRecurrence()
        {
            // 0.25 단위로 두어 부동소수 오차 없이 경계값을 맞춘다.
            Setup(b => { b.Initial = 72; b.BaseDecayPerSec = 0.25; }, Never(60));
            Run(15); // 9초에 발동, 15초에 68.25
            _director.NotifyHypoEventEnded();
            _director.NotifyGraceEnded();
            _model.EatSnack("juice", 0.25); // +7.5, 최고 75

            Run(10); // cooldown 종료 시 73.25, 70~80 사이
            Assert.That(_hypos, Has.Count.EqualTo(1));

            Run(13); // 70.0, 아직 발동 안 함
            Assert.That(_hypos, Has.Count.EqualTo(1));

            Run(1); // 69.75
            Assert.That(_hypos, Has.Count.EqualTo(2));
            Assert.That(_hypos[1].IsRecurrence, Is.True);
        }

        [Test]
        public void NotifyHypoEventEnded_WithoutActiveEvent_Throws()
        {
            Setup(null);

            Assert.Throws<InvalidOperationException>(() => _director.NotifyHypoEventEnded());
        }

        [Test]
        public void SuddenDrop_StartsWhenRandomBelowProbability()
        {
            Setup(b => b.SuddenDropChancePerSec = 0.5, 0.1);

            _model.Tick(1, 1, 1, 0);
            _director.Evaluate(1);

            Assert.That(_drops, Has.Count.EqualTo(1));
            Assert.That(_model.IsSuddenDropActive, Is.True);

            _model.Tick(1, 1, 1, 0);
            _director.Evaluate(1); // 진행 중이면 난수를 읽지 않는다
            Assert.That(_drops, Has.Count.EqualTo(1));
        }

        [Test]
        public void SuddenDrop_DoesNotStartWhenRandomAboveProbability()
        {
            Setup(b => b.SuddenDropChancePerSec = 0.5, 0.6);

            _model.Tick(1, 1, 1, 0);
            _director.Evaluate(1);

            Assert.That(_drops, Is.Empty);
        }

        [Test]
        public void SuddenDrop_ProbabilityScalesWithSimDt()
        {
            // p = 1 − (1 − 0.5)^2 = 0.75
            Setup(b => b.SuddenDropChancePerSec = 0.5, 0.7);

            _model.Tick(2, 2, 2, 0);
            _director.Evaluate(2);

            Assert.That(_drops, Has.Count.EqualTo(1));
        }

        [Test]
        public void SuddenDrop_IsNotJudgedWhenSimTimeDoesNotFlow()
        {
            Setup(b => b.SuddenDropChancePerSec = 1); // 난수 없음: 읽으면 예외

            _director.Evaluate(0);
            _model.Tick(0, 0, 1, 0);
            _director.Evaluate(0);

            Assert.That(_drops, Is.Empty);
        }

        [Test]
        public void EventIds_IncreaseFromOne()
        {
            Setup(b => { b.Initial = 72; b.BaseDecayPerSec = 1; });
            Run(3);
            _director.NotifyHypoEventEnded();
            _director.NotifyGraceEnded();

            Run(10);

            Assert.That(_hypos, Has.Count.EqualTo(2));
            Assert.That(_hypos[0].EventId, Is.EqualTo(1));
            Assert.That(_hypos[1].EventId, Is.EqualTo(2));
        }

        [Test]
        public void GaugeWarning_RaisedOnceWhenDisplayedCrossesBelow()
        {
            Setup(b => b.BaseDecayPerSec = 1); // warning 80, delay 3

            Run(42); // 표시 = actual(39) = 81
            Assert.That(_warnings, Is.Empty);
            Assert.That(_director.IsGaugeWarningOn, Is.False);

            Run(2); // 표시 = actual(41) = 79
            Assert.That(_warnings, Has.Count.EqualTo(1));
            Assert.That(_warnings[0].Displayed, Is.EqualTo(79).Within(1e-9));
            Assert.That(_director.IsGaugeWarningOn, Is.True);

            Run(3);
            Assert.That(_warnings, Has.Count.EqualTo(1));
        }

        [Test]
        public void GaugeWarning_TurnsOffWhenDisplayedRecovers_AndCanRaiseAgain()
        {
            Setup(b => b.BaseDecayPerSec = 1);
            Run(44); // 경고 켜짐, actual 76
            _model.EatSnack("juice", 1);

            Run(6); // actual(47) = 103, 표시 = 103
            Assert.That(_director.IsGaugeWarningOn, Is.False);

            Run(25); // 표시 = actual(72) = 78
            Assert.That(_warnings, Has.Count.EqualTo(2));
            Assert.That(_director.IsGaugeWarningOn, Is.True);
        }

        [Test]
        public void GaugeWarning_InitiallyOn_IsNotPublished()
        {
            Setup(b => b.Initial = 75);

            Assert.That(_director.IsGaugeWarningOn, Is.True);
            Run(1);
            Assert.That(_warnings, Is.Empty);
        }

        [Test]
        public void DuringSuppression_NoTriggerAndNoSuddenDrop()
        {
            Setup(b => { b.Initial = 72; b.BaseDecayPerSec = 1; b.SuddenDropChancePerSec = 0.5; },
                0.99, 0.99, 0.99, 0.99);
            Run(3); // 발동, 난수 3개 사용
            _director.NotifyHypoEventEnded();

            Run(20); // cooldown 10보다 길다. 억제 중에는 난수를 읽지 않는다.
            Assert.That(_hypos, Has.Count.EqualTo(1));
            Assert.That(_drops, Is.Empty);

            _director.NotifyGraceEnded();
            Run(1); // 남은 난수 1개로 급하강 판정(미발생), 재발 발동

            Assert.That(_hypos, Has.Count.EqualTo(2));
            Assert.That(_hypos[1].IsRecurrence, Is.True);
            Assert.That(_drops, Is.Empty);
        }

        [Test]
        public void NotifyGraceEnded_WithoutSuppression_Throws()
        {
            Setup(null);

            Assert.Throws<InvalidOperationException>(() => _director.NotifyGraceEnded());
        }

        [Test]
        public void Evaluate_NegativeSimDt_Throws()
        {
            Setup(null);

            Assert.Throws<ArgumentOutOfRangeException>(() => _director.Evaluate(-1));
        }

        [Test]
        public void TargetRange_InitialInRange_PublishesEntered()
        {
            Setup(null);

            _director.EvaluateTargetRange();

            Assert.That(_entered, Has.Count.EqualTo(1));
            Assert.That(_entered[0].Actual, Is.EqualTo(120).Within(1e-9));
            Assert.That(_exited, Is.Empty);
            Assert.That(_director.IsInTargetRange, Is.True);
        }

        [Test]
        public void TargetRange_InitialOutOfRange_PublishesNothing()
        {
            Setup(b => b.Initial = 75);

            _director.EvaluateTargetRange();

            Assert.That(_entered, Is.Empty);
            Assert.That(_exited, Is.Empty);
            Assert.That(_director.IsInTargetRange, Is.False);
        }

        [Test]
        public void TargetRange_CrossingBelow_PublishesExitedOnce()
        {
            Setup(b => { b.Initial = 82; b.BaseDecayPerSec = 1; });

            Run(10); // 82 → 72. 80 아래(79)에서 한 번 나간다.

            Assert.That(_entered, Has.Count.EqualTo(1));
            Assert.That(_exited, Has.Count.EqualTo(1));
            Assert.That(_exited[0].Actual, Is.EqualTo(79).Within(1e-9));
            Assert.That(_director.IsInTargetRange, Is.False);
        }

        [Test]
        public void TargetRange_ReachingHyperThreshold_PublishesExited()
        {
            Setup(b => b.Initial = 235);
            _model.EatSnack("juice", 1); // 30 상승, onset 1, absorb 2

            Run(1); // 235
            Assert.That(_exited, Is.Empty);
            Run(1); // 250: 고혈당 임계값과 같으면 범위 밖

            Assert.That(_exited, Has.Count.EqualTo(1));
            Assert.That(_exited[0].Actual, Is.EqualTo(250).Within(1e-9));
            Assert.That(_director.IsInTargetRange, Is.False);
        }

        [Test]
        public void TargetRange_UsesNormalRangeMax_BelowHyperThreshold()
        {
            // 적정 범위 위쪽(180)은 고혈당 알리기 기준(250)과 따로다.
            Setup(b => { b.Initial = 165; b.NormalRangeMax = 180; });
            _model.EatSnack("juice", 1); // 30 상승, onset 1, absorb 2

            Run(1); // 165
            Assert.That(_exited, Is.Empty);
            Run(1); // 180: 위쪽 경계와 같으면 범위 밖

            Assert.That(_exited, Has.Count.EqualTo(1));
            Assert.That(_exited[0].Actual, Is.EqualTo(180).Within(1e-9));
            Assert.That(_hypers, Is.Empty, "고혈당 알리기 기준(250)에는 닿지 않았다");
        }

        [Test]
        public void SetSuddenDropChance_Zero_NeverStarts()
        {
            Setup(b => b.SuddenDropChancePerSec = 1, 0.5); // 확률 0이면 난수를 읽지 않는다

            _director.SetSuddenDropChancePerSec(0);
            Run(5);
            Assert.That(_drops, Is.Empty);

            _director.SetSuddenDropChancePerSec(null);
            Run(1);
            Assert.That(_drops, Has.Count.EqualTo(1));
        }

        [Test]
        public void Gate_None_BlocksHypoHyperAndSuddenDrop()
        {
            Setup(b => { b.Initial = 60; b.SuddenDropChancePerSec = 1; }, 0.5, 0.5, 0.5);
            Assert.That(_director.Gate, Is.EqualTo(TriggerGate.All));
            _director.SetTriggerGate(TriggerGate.None);

            Run(3);

            Assert.That(_hypos, Is.Empty);
            Assert.That(_drops, Is.Empty);
            Assert.That(_hypers, Is.Empty);

            _director.SetTriggerGate(TriggerGate.All);
            Run(1);
            Assert.That(_hypos, Has.Count.EqualTo(1));
        }

        [Test]
        public void Gate_LowOnly_AllowsHypo_BlocksHyper()
        {
            Setup(b => { b.Initial = 60; b.SuddenDropChancePerSec = 1; }, 0.5, 0.5);
            _director.SetTriggerGate(TriggerGate.LowOnly);
            Run(1);
            Assert.That(_hypos, Has.Count.EqualTo(1));
            Assert.That(_drops, Is.Empty);

            Setup(b => b.Initial = 260);
            _director.SetTriggerGate(TriggerGate.LowOnly);
            Run(2);
            Assert.That(_hypers, Is.Empty);
            Assert.That(_director.IsHyperTriggerPending, Is.False);
        }

        [Test]
        public void Gate_HighOnly_AllowsHyper_BlocksHypo()
        {
            Setup(b => b.Initial = 260);
            _director.SetTriggerGate(TriggerGate.HighOnly);
            Run(1);
            Assert.That(_hypers, Has.Count.EqualTo(1));

            Setup(b => b.Initial = 60);
            _director.SetTriggerGate(TriggerGate.HighOnly);
            Run(2);
            Assert.That(_hypos, Is.Empty);
        }
    }
}
