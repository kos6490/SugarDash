using System;
using System.Collections.Generic;
using SugarDash.Domain.Config;
using NUnit.Framework;

namespace SugarDash.Domain.Tests
{
    public class GameConfigValidatorTests
    {
        [Test]
        public void ValidConfig_HasNoErrors()
        {
            Assert.That(GameConfigValidator.Validate(new ConfigBuilder().Build()), Is.Empty);
        }

        [Test]
        public void RecheckIntervalGameSec_ConvertsRealMinutesByTimeScale()
        {
            var config = new ConfigBuilder { RecheckIntervalRealMin = 15, TimeScale = 150 }.BuildGlucose();

            Assert.That(config.Time.RecheckIntervalGameSec, Is.EqualTo(6).Within(1e-9));
        }

        private static IEnumerable<TestCaseData> SingleViolations()
        {
            TestCaseData Case(string name, Action<ConfigBuilder> mutate) =>
                new TestCaseData(mutate).SetName("Violation_" + name);

            yield return Case("MinNotBelowHypo", b => b.Min = 70);
            yield return Case("InitialNotAboveHypo", b => b.Initial = 60);
            yield return Case("InitialNotBelowHyper", b => b.Initial = 250);
            yield return Case("HyperAboveMax", b => b.Max = 200);
            yield return Case("ReleaseMarginZero", b => b.HypoReleaseMargin = 0);
            yield return Case("ReleaseLevelNotBelowHyper", b => b.HypoReleaseMargin = 200);
            yield return Case("BaseDecayNegative", b => b.BaseDecayPerSec = -1);
            yield return Case("RunDecayNegative", b => b.RunDecayPerSec = -1);
            yield return Case("SnackRiseNegative", b => b.SnackById("juice").TotalRise = -1);
            yield return Case("SnackOnsetNegative", b => b.SnackById("juice").OnsetSec = -1);
            yield return Case("SnackAbsorbZero", b => b.SnackById("cracker").AbsorbSec = 0);
            yield return Case("DelayNegative", b => b.DelaySec = -1);
            yield return Case("WarningBelowHypo", b => b.WarningThreshold = 60);
            yield return Case("DropChanceNegative", b => b.SuddenDropChancePerSec = -0.1);
            yield return Case("DropChanceAboveOne", b => b.SuddenDropChancePerSec = 1.5);
            yield return Case("DropAmountZero", b => b.SuddenDropAmount = 0);
            yield return Case("DropDurationZero", b => b.SuddenDropDurationSec = 0);
            yield return Case("CooldownZero", b => b.CooldownSec = 0);
            yield return Case("DuplicateSnackId", b => b.SnackById("cracker").Id = "juice");
            yield return Case("FastOnsetNotBeforeSlow", b => b.SnackById("juice").OnsetSec = 5);
            yield return Case("TimeScaleZero", b => b.TimeScale = 0);
            yield return Case("FastOnsetNotBeforeRecheck", b => b.TimeScale = 900);
            yield return Case("ConfigVersionEmpty", b => b.ConfigVersion = " ");
            yield return Case("UnitNotMgPerDl", b => b.Unit = "mmol/L");
            yield return Case("Step1NotLongerThanDelay", b => b.Step1TimeLimitSec = 3);
        }

        [TestCaseSource(nameof(SingleViolations))]
        public void EachInvariant_IsReportedOnce(Action<ConfigBuilder> mutate)
        {
            var builder = new ConfigBuilder();
            mutate(builder);

            Assert.That(GameConfigValidator.Validate(builder.Build()), Has.Count.EqualTo(1));
        }

        [Test]
        public void MultipleViolations_AreAllCollected()
        {
            var builder = new ConfigBuilder { CooldownSec = 0, WarningThreshold = 60, Step1TimeLimitSec = 1 };

            Assert.That(GameConfigValidator.Validate(builder.Build()), Has.Count.EqualTo(3));
        }

        [Test]
        public void RecheckIntervalNotPositive_IsReported()
        {
            var builder = new ConfigBuilder { RecheckIntervalRealMin = 0 };

            Assert.That(GameConfigValidator.Validate(builder.Build()), Is.Not.Empty);
        }
    }
}
