using System;
using System.Collections.Generic;
using System.Linq;
using SugarDash.Domain.Config;
using SugarDash.Domain.Hypo;
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
        public void Type2_DefaultBuilder_IsValid()
        {
            Assert.That(GameConfigValidator.Validate(new ConfigBuilder().BuildType2()), Is.Empty);
        }

        [Test]
        public void Type1_DoesNotRequireType2ScoreKeys()
        {
            var source = new ConfigBuilder().Build();
            var correct = source.Score.CorrectWeights.Where(p => p.Key != HypoStep.LowNotify)
                .ToDictionary(p => p.Key, p => p.Value);
            var errors = source.Score.ErrorWeights
                .Where(p => p.Key != HypoErrorCode.HyJuice && p.Key != HypoErrorCode.LoKeepRunning &&
                            p.Key != HypoErrorCode.LoTimeout)
                .ToDictionary(p => p.Key, p => p.Value);
            var score = new ScoreConfig(source.Score.InRangePerSec, source.Score.PerMeter, correct, errors);
            var config = new GameConfig(source.ConfigVersion, source.Unit, source.Glucose, source.Hypo, source.Hyper,
                source.Runner, source.Elimination, source.Symptom, score, source.Sections);

            Assert.That(GameConfigValidator.Validate(config), Is.Empty);
        }

        [Test]
        public void Type2_DoesNotCheckType1OnlyRules()
        {
            var config = new ConfigBuilder().BuildType2();

            Assert.That(config.Hypo.Step1TimeLimitSec, Is.EqualTo(0));
            Assert.That(string.Join("\n", GameConfigValidator.Validate(config)), Does.Not.Contain("step1TimeLimitSec"));
        }

        private static IEnumerable<TestCaseData> Type2Violations()
        {
            TestCaseData Case(string name, string key, Action<ConfigBuilder> mutate) =>
                new TestCaseData(mutate, key).SetName("Type2_Violation_" + name);

            yield return Case("GlucoseLowAtHypo", "elimination.glucoseLow", b => b.GlucoseLow = 70);
            yield return Case("GlucoseHighAtHyper", "elimination.glucoseHigh", b => b.GlucoseHigh = 250);
            yield return Case("ReleaseBandsOverlap", "hyperReleaseMargin", b => b.HyperReleaseMargin = 175);
            yield return Case("NormalRangeBelowHypo", "normalRangeMin", b => b.NormalRangeMin = 60);
            yield return Case("HighHoldNegative", "type2.highHoldSec", b => b.HighHoldSec = -1);
            yield return Case("NoticeNegative", "type2.noticeSec", b => b.NoticeSec = -1);
            yield return Case("TellLimitZero", "type2.tellTimeLimitSec", b => b.TellTimeLimitSec = 0);
            yield return Case("GuardianPromptZero", "type2.guardianPromptTimeLimitSec", b => b.GuardianPromptTimeLimitSec = 0);
            yield return Case("RestDurationZero", "type2.restDurationSec", b => b.RestDurationSec = 0);
            yield return Case("FoodRowMinAboveMax", "type2.foodRowIntervalMinSec", b => { b.FoodRowIntervalMinSec = 5; b.FoodRowIntervalMaxSec = 4; });
            yield return Case("RestIntervalMinZero", "type2.restIntervalMinSec", b => b.RestIntervalMinSec = 0);
            yield return Case("GuardianSnackMissing", "type2.guardianSnackId", b => b.GuardianSnackId = "nope");
            yield return Case("JuiceChoiceMissing", "type2.juiceChoiceFoodId", b => b.JuiceChoiceFoodId = "nope");
            yield return Case("SingleFood", "foods: 2개 이상", b => b.Snacks.RemoveAt(1));
            yield return Case("DuplicateFoodId", "foods: id가 중복", b => b.SnackById("cracker").Id = "juice");
            yield return Case("AbsorbZero", "absorbSec", b => b.SnackById("cracker").AbsorbSec = 0);
            yield return Case("TimeScaleZero", "time.timeScale", b => b.TimeScale = 0);
            yield return Case("LowKeepRunningWeight", "LoKeepRunning", b => b.ErrorWeightOverrides[HypoErrorCode.LoKeepRunning] = 1);
        }

        [TestCaseSource(nameof(Type2Violations))]
        public void Type2_Violations(Action<ConfigBuilder> mutate, string key)
        {
            var builder = new ConfigBuilder();
            mutate(builder);

            var errors = GameConfigValidator.Validate(builder.BuildType2());

            Assert.That(errors, Is.Not.Empty);
            Assert.That(string.Join("\n", errors), Does.Contain(key));
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

            yield return Case("MinNotBelowHypo", b => b.Min = 70, 3); // 1 + 18(G0 = 40 < min) + 21(glucoseLow 40 < min)
            yield return Case("InitialNotAboveHypo", b => b.Initial = 60);
            yield return Case("InitialNotBelowHyper", b => b.Initial = 250);
            yield return Case("HyperAboveMax", b => b.Max = 200, 2); // 1 + 21(glucoseHigh 300 > max)
            yield return Case("ReleaseMarginZero", b => b.HypoReleaseMargin = 0);
            yield return Case("ReleaseLevelNotBelowHyper", b => b.HypoReleaseMargin = 200);
            yield return Case("HyperReleaseMarginZero", b => b.HyperReleaseMargin = 0);
            yield return Case("ReleaseBandsOverlap", b => b.HyperReleaseMargin = 175); // 80 ≥ 250 − 175
            yield return Case("HyperNotifyLimitZero", b => b.HyperNotifyTimeLimitSec = 0);
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
            yield return Case("AnswerFeedbackNegative", b => b.AnswerFeedbackSec = -1);
            yield return Case("AnswerFeedbackNaN", b => b.AnswerFeedbackSec = double.NaN);
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
            yield return Case("IntervalMinZero", b => b.TrackSnackIntervalMinSec = 0);
            yield return Case("IntervalMinAboveMax", b => { b.TrackSnackIntervalMinSec = 9; b.TrackSnackIntervalMaxSec = 8; });
            yield return Case("GlucoseLowAtHypoThreshold", b => b.GlucoseLow = 70);
            yield return Case("GlucoseLowBelowMin", b => b.GlucoseLow = 19);
            yield return Case("GlucoseHighAtHyperThreshold", b => b.GlucoseHigh = 250);
            yield return Case("GlucoseHighAboveMax", b => b.GlucoseHigh = 401);
            yield return Case("ShakeStartAtGlucoseLow", b => b.ShakeStartGlucose = 40);
            yield return Case("ShakeStartAboveMax", b => b.ShakeStartGlucose = 401);
            yield return Case("NegativeInRangePerSec", b => b.InRangePerSec = -1);
            yield return Case("KeepRunningNotZero", b => b.ErrorWeightOverrides[HypoErrorCode.RsKeepRunning] = 1);
            yield return Case("HyperKeepRunningNotZero", b => b.ErrorWeightOverrides[HypoErrorCode.HyKeepRunning] = 1);
            yield return Case("NoSections", b => b.Sections.Clear());
            yield return Case("LastSectionHasLength", b => b.Sections = new List<SectionConfig> { new SectionConfig("signal", 100) });
            yield return Case("MiddleSectionNoLength", b => b.Sections = new List<SectionConfig> { new SectionConfig("signal", null), new SectionConfig("condition", null) });
            yield return Case("DuplicateSectionId", b => b.Sections = new List<SectionConfig> { new SectionConfig("signal", 100), new SectionConfig("signal", null) });
            yield return Case("UnknownSectionId", b => b.Sections = new List<SectionConfig> { new SectionConfig("forest", null) });
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
        public void AnswerFeedbackZero_IsValid()
        {
            Assert.That(GameConfigValidator.Validate(new ConfigBuilder { AnswerFeedbackSec = 0 }.Build()), Is.Empty);
        }

        [Test]
        public void Recovery_IncludesAnswerFeedbackWindows()
        {
            // 처치 전 문구 창 5번, 처치 뒤 재측정까지 2번이 기본 하강으로 혈당을 깎는다.
            // G0 = 70 − 30 − 1.5×5 − 0.5×(25 + 5×1) = 17.5, A = 35×2 = 70, D = 0.5×(5+5+6 + 2×1) = 9 → 78.5
            var builder = new ConfigBuilder
            {
                BaseDecayPerSec = 0.5, RunDecayPerSec = 1, AnswerFeedbackSec = 1, Min = 17.5, NormalRangeMin = 78.5,
            };
            builder.SnackById("juice").TotalRise = 35;
            Assert.That(GameConfigValidator.Validate(builder.Build()), Is.Empty);

            builder.NormalRangeMin = 78.501;
            Assert.That(GameConfigValidator.Validate(builder.Build()), Has.Count.EqualTo(1));

            builder.NormalRangeMin = 78.5;
            builder.Min = 17.501;
            var errors = GameConfigValidator.Validate(builder.Build());
            Assert.That(errors, Has.Count.EqualTo(1));
            Assert.That(errors[0], Does.Contain("min"));
        }

        [Test]
        public void WorstCaseBelowMin_IsReported()
        {
            // G0 = 70 − 30 = 40 < min 45. 흡수량이 커서 회복 조건은 통과한다.
            var builder = new ConfigBuilder { Min = 45, GlucoseLow = 45 };

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
