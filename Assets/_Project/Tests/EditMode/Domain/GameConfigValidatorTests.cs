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
            TestCaseData Case(string name, Action<ConfigBuilder> mutate, int expectedCount = 1) =>
                new TestCaseData(mutate, expectedCount).SetName("Violation_" + name);

            yield return Case("MinNotBelowHypo", b => b.Min = 70, 2); // 1 + 18(G0 = 40 < min)
            yield return Case("InitialNotAboveHypo", b => b.Initial = 60);
            yield return Case("InitialNotBelowHyper", b => b.Initial = 250);
            yield return Case("HyperAboveMax", b => b.Max = 200);
            yield return Case("ReleaseMarginZero", b => b.HypoReleaseMargin = 0);
            yield return Case("ReleaseLevelNotBelowHyper", b => b.HypoReleaseMargin = 200);
            yield return Case("BaseDecayNegative", b => b.BaseDecayPerSec = -1);
            yield return Case("RunDecayNegative", b => b.RunDecayPerSec = -1);
            yield return Case("SnackRiseNegative", b => b.SnackById("cracker").TotalRise = -1);
            yield return Case("SnackOnsetNegative", b => b.SnackById("juice").OnsetSec = -1);
            yield return Case("SnackAbsorbZero", b => b.SnackById("cracker").AbsorbSec = 0);
            yield return Case("DelayNegative", b => b.DelaySec = -1, 2);
            yield return Case("WarningBelowHypo", b => b.WarningThreshold = 60);
            yield return Case("DropChanceNegative", b => b.SuddenDropChancePerSec = -0.1);
            yield return Case("DropChanceAboveOne", b => b.SuddenDropChancePerSec = 1.5);
            yield return Case("DropAmountZero", b => b.SuddenDropAmount = 0);
            yield return Case("DropDurationZero", b => b.SuddenDropDurationSec = 0);
            yield return Case("CooldownZero", b => b.CooldownSec = 0);
            yield return Case("DuplicateSnackId", b => b.SnackById("cracker").Id = "juice");
            yield return Case("FastOnsetNotBeforeSlow", b => b.SnackById("cracker").OnsetSec = 1);
            yield return Case("TimeScaleZero", b => b.TimeScale = 0);
            yield return Case("FastOnsetNotBeforeRecheck", b => b.TimeScale = 900, 3);
            yield return Case("ConfigVersionEmpty", b => b.ConfigVersion = " ");
            yield return Case("UnitNotMgPerDl", b => b.Unit = "mmol/L");
            yield return Case("Step1NotLongerThanDelay", b => b.Step1TimeLimitSec = 3);
            yield return Case("NormalRangeBelowHypo", b => b.NormalRangeMin = 60);
            yield return Case("NormalRangeNotBelowHyper", b => { b.NormalRangeMin = 250; b.DoseProper = 8; b.DoseOver = 9; });
            yield return Case("RecheckNotLongerThanDelay", b => { b.DelaySec = 6; b.Step1TimeLimitSec = 7; }, 2);
            yield return Case("FastForwardTickZero", b => b.FastForwardTickSimSec = 0);
            yield return Case("FastForwardTickNotBelowDelay", b => b.FastForwardTickSimSec = 3);
            yield return Case("Step2Zero", b => b.Step2TimeLimitSec = 0);
            yield return Case("Step3Zero", b => b.Step3TimeLimitSec = 0);
            yield return Case("Step4Zero", b => b.Step4TimeLimitSec = 0);
            yield return Case("GuardianPromptZero", b => b.GuardianPromptTimeLimitSec = 0);
            yield return Case("ResumeCountdownZero", b => b.ResumeCountdownSec = 0);
            yield return Case("GraceZero", b => b.GraceSec = 0);
            yield return Case("DoseUnderZero", b => b.DoseUnder = 0, 2); // 16 + 19(trackSnack 0.5 ≥ under)
            yield return Case("DoseProperNotAboveUnder", b => b.DoseUnder = 2);
            yield return Case("DoseOverNotAboveProper", b => b.DoseOver = 2);
            yield return Case("GuardianSnackMissing", b => b.GuardianSnackId = "candy");
            yield return Case("GuardianSnackSlow", b => b.GuardianSnackId = "cracker");
            yield return Case("RecoveryActualInsufficient", b => b.NormalRangeMin = 101);
            // W = 6에서는 전량 흡수되지만 W − delay = 3에서는 절반만 흡수된다.
            yield return Case("RecoveryDisplayInsufficient", b => b.SnackById("juice").AbsorbSec = 4);
            yield return Case("TrackSnackZero", b => b.TrackSnackServings = 0);
            yield return Case("TrackSnackNotBelowDoseUnder", b => b.TrackSnackServings = 1);
            yield return Case("RunDurationZero", b => b.RunDurationSec = 0);
        }

        [TestCaseSource(nameof(SingleViolations))]
        public void EachInvariant_IsReportedOnce(Action<ConfigBuilder> mutate, int expectedCount)
        {
            var builder = new ConfigBuilder();
            mutate(builder);

            Assert.That(GameConfigValidator.Validate(builder.Build()), Has.Count.EqualTo(expectedCount));
        }

        [Test]
        public void Recovery_BoundaryIsInclusive()
        {
            // G0 = 70 − 30 − 1.5×5 − 0.5×25 = 20, A = 60, D = 0.5×(5+5+6) = 8 → 72
            var builder = new ConfigBuilder { BaseDecayPerSec = 0.5, RunDecayPerSec = 1, NormalRangeMin = 72 };
            Assert.That(GameConfigValidator.Validate(builder.Build()), Is.Empty);

            builder.NormalRangeMin = 72.001;
            Assert.That(GameConfigValidator.Validate(builder.Build()), Has.Count.EqualTo(1));
        }

        [Test]
        public void WorstCaseBelowMin_IsReported()
        {
            // G0 = 70 − 30 = 40 < min 45. 흡수량이 커서 회복 조건은 통과한다.
            var builder = new ConfigBuilder { Min = 45 };

            var errors = GameConfigValidator.Validate(builder.Build());

            Assert.That(errors, Has.Count.EqualTo(1));
            Assert.That(errors[0], Does.Contain("min"));
        }

        [Test]
        public void TimeScaleZero_SkipsRecheckDerivedInvariants()
        {
            var builder = new ConfigBuilder { TimeScale = 0 };

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
