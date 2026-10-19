using System;
using SugarDash.Domain.Glucose;
using NUnit.Framework;

namespace SugarDash.Domain.Tests
{
    public class GlucoseModelTests
    {
        private const double Eps = 1e-9;

        private static GlucoseModel Model(Action<ConfigBuilder> configure = null)
        {
            var builder = new ConfigBuilder();
            configure?.Invoke(builder);
            return new GlucoseModel(builder.BuildGlucose());
        }

        [Test]
        public void Initially_ActualAndDisplayedEqualInitial()
        {
            var model = Model();

            Assert.That(model.Actual, Is.EqualTo(120));
            Assert.That(model.Displayed, Is.EqualTo(120));
            Assert.That(model.Mode, Is.EqualTo(GlucoseClockMode.Running));
        }

        [Test]
        public void Running_DecaysByBaseAndRunTimesActivity()
        {
            var model = Model(b => { b.BaseDecayPerSec = 0.5; b.RunDecayPerSec = 1; });

            model.Tick(2, 0.5);

            Assert.That(model.Actual, Is.EqualTo(120 - (0.5 + 1 * 0.5) * 2).Within(Eps));
            Assert.That(model.SimTime, Is.EqualTo(2).Within(Eps));
        }

        [Test]
        public void Running_WithZeroActivity_AppliesBaseDecayOnly()
        {
            var model = Model(b => { b.BaseDecayPerSec = 0.5; b.RunDecayPerSec = 1; });

            model.Tick(2, 0);

            Assert.That(model.Actual, Is.EqualTo(119).Within(Eps));
        }

        [Test]
        public void Snack_IsAbsorbedUniformlyAfterOnset()
        {
            var model = Model(); // juice: rise 30, onset 1, absorb 2

            model.EatSnack("juice", 1);
            model.Tick(1, 0);
            Assert.That(model.Actual, Is.EqualTo(120).Within(Eps));
            model.Tick(1, 0);
            Assert.That(model.Actual, Is.EqualTo(135).Within(Eps));
            model.Tick(1, 0);
            Assert.That(model.Actual, Is.EqualTo(150).Within(Eps));
            model.Tick(1, 0);
            Assert.That(model.Actual, Is.EqualTo(150).Within(Eps));
        }

        [Test]
        public void Snack_RiseScalesWithServings()
        {
            var model = Model();

            model.EatSnack("juice", 2);
            model.Tick(5, 0);

            Assert.That(model.Actual, Is.EqualTo(180).Within(Eps));
        }

        [Test]
        public void Result_DoesNotDependOnTickLength()
        {
            void Configure(ConfigBuilder b) { b.BaseDecayPerSec = 0.5; b.RunDecayPerSec = 0.7; }
            var fine = Model(Configure);
            var coarse = Model(Configure);
            fine.EatSnack("cracker", 1);
            coarse.EatSnack("cracker", 1);

            for (int i = 0; i < 100; i++) fine.Tick(0.1, 0.8);
            coarse.Tick(10, 0.8);

            Assert.That(fine.Actual, Is.EqualTo(coarse.Actual).Within(1e-6));
            Assert.That(fine.Displayed, Is.EqualTo(coarse.Displayed).Within(1e-6));
        }

        [Test]
        public void Actual_IsClampedToMinAndMax()
        {
            var low = Model(b => b.BaseDecayPerSec = 100);
            low.Tick(10, 0);
            Assert.That(low.Actual, Is.EqualTo(20));

            var high = Model(b => b.SnackById("juice").TotalRise = 1000);
            high.EatSnack("juice", 1);
            high.Tick(10, 0);
            Assert.That(high.Actual, Is.EqualTo(400));
        }

        [Test]
        public void Displayed_IsActualDelayedByDelaySec()
        {
            var model = Model(b => b.BaseDecayPerSec = 1); // delay 3

            model.Tick(1, 0);
            Assert.That(model.Displayed, Is.EqualTo(120).Within(Eps), "지연 시간 전에는 초기값");

            model.Tick(4, 0); // actual(5) = 115, displayed = actual(2) = 118 (보간)
            Assert.That(model.Actual, Is.EqualTo(115).Within(Eps));
            Assert.That(model.Displayed, Is.EqualTo(118).Within(Eps));
        }

        [Test]
        public void ActualFrozen_HoldsActual_AndDisplayCatchesUpWithinDelay()
        {
            var model = Model(b => b.BaseDecayPerSec = 1);
            model.Tick(5, 0); // actual 115, displayed 118

            model.SetMode(GlucoseClockMode.ActualFrozen);
            model.Tick(3, 1);

            Assert.That(model.Actual, Is.EqualTo(115).Within(Eps));
            Assert.That(model.Displayed, Is.EqualTo(115).Within(Eps));
            Assert.That(model.SimTime, Is.EqualTo(5).Within(Eps));
            Assert.That(model.RealElapsed, Is.EqualTo(8).Within(Eps));
        }

        [Test]
        public void ActualFrozen_DoesNotProgressSnackAbsorption()
        {
            var model = Model();
            model.EatSnack("juice", 1);

            model.SetMode(GlucoseClockMode.ActualFrozen);
            model.Tick(10, 0);
            model.SetMode(GlucoseClockMode.Running);
            model.Tick(2, 0);

            Assert.That(model.Actual, Is.EqualTo(135).Within(Eps));
        }

        [Test]
        public void AllFrozen_HoldsActualAndDisplayed_ButRealTimeAdvances()
        {
            var model = Model(b => b.BaseDecayPerSec = 1);
            model.Tick(5, 0);

            model.SetMode(GlucoseClockMode.AllFrozen);
            model.Tick(3, 1);

            Assert.That(model.Actual, Is.EqualTo(115).Within(Eps));
            Assert.That(model.Displayed, Is.EqualTo(118).Within(Eps));
            Assert.That(model.SimTime, Is.EqualTo(5).Within(Eps));
            Assert.That(model.RealElapsed, Is.EqualTo(8).Within(Eps));
        }

        [Test]
        public void FastForward_AppliesBaseDecayAndSnacks_WithoutRunDecayOrRealTime()
        {
            var model = Model(b => { b.BaseDecayPerSec = 0.5; b.RunDecayPerSec = 5; });
            model.EatSnack("juice", 1);
            model.SetMode(GlucoseClockMode.AllFrozen);

            model.FastForward(6);

            Assert.That(model.Actual, Is.EqualTo(120 - 3 + 30).Within(Eps));
            Assert.That(model.SimTime, Is.EqualTo(6).Within(Eps));
            Assert.That(model.RealElapsed, Is.EqualTo(0).Within(Eps));
            // 표시 = actual(3) = 120 - 1.5 + 30. 흡수 구간 경계에서도 정확해야 한다.
            Assert.That(model.Displayed, Is.EqualTo(148.5).Within(Eps));
            Assert.That(model.Mode, Is.EqualTo(GlucoseClockMode.AllFrozen));
        }

        [Test]
        public void SuddenDrop_LowersByAmountOverDuration()
        {
            var model = Model(); // amount 30, duration 5

            model.StartSuddenDrop();
            model.Tick(2, 0);
            Assert.That(model.IsSuddenDropActive, Is.True);
            Assert.That(model.Actual, Is.EqualTo(108).Within(Eps));

            model.Tick(10, 0);
            Assert.That(model.IsSuddenDropActive, Is.False);
            Assert.That(model.Actual, Is.EqualTo(90).Within(Eps));
        }

        [Test]
        public void InvalidCalls_Throw()
        {
            var model = Model();

            Assert.Throws<ArgumentOutOfRangeException>(() => model.Tick(-1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => model.Tick(1, 1.5));
            Assert.Throws<ArgumentOutOfRangeException>(() => model.FastForward(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => model.EatSnack("juice", -1));
            Assert.Throws<ArgumentException>(() => model.EatSnack("unknown", 1));

            model.StartSuddenDrop();
            Assert.Throws<InvalidOperationException>(() => model.StartSuddenDrop());
        }
    }
}
