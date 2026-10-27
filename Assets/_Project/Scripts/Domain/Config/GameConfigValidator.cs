using System;
using System.Collections.Generic;
using System.Linq;

namespace SugarDash.Domain.Config
{
    /// <summary>
    /// 설정 불변식 검증(docs/glucose-model.md 8절). 위반을 멈추지 않고 모두 모아 반환한다.
    /// </summary>
    public static class GameConfigValidator
    {
        public static IReadOnlyList<string> Validate(GameConfig config)
        {
            var errors = new List<string>();
            var g = config.Glucose;
            var levels = g.Levels;

            // 1
            if (!(levels.Min < levels.HypoThreshold && levels.HypoThreshold < levels.Initial &&
                  levels.Initial < levels.HyperThreshold && levels.HyperThreshold <= levels.Max))
            {
                errors.Add($"glucose: min < hypoThreshold < initial < hyperThreshold ≤ max 이어야 한다 " +
                           $"(min={levels.Min}, hypoThreshold={levels.HypoThreshold}, initial={levels.Initial}, " +
                           $"hyperThreshold={levels.HyperThreshold}, max={levels.Max})");
            }

            // 2
            if (!(levels.HypoReleaseMargin > 0))
                errors.Add($"glucose.hypoReleaseMargin > 0 이어야 한다 ({levels.HypoReleaseMargin})");
            if (!(levels.HypoReleaseLevel < levels.HyperThreshold))
                errors.Add($"glucose: hypoThreshold + hypoReleaseMargin < hyperThreshold 이어야 한다 " +
                           $"({levels.HypoReleaseLevel} ≥ {levels.HyperThreshold})");

            // 3
            NonNegative(errors, "glucose.baseDecayPerSec", levels.BaseDecayPerSec);
            NonNegative(errors, "glucose.runDecayPerSec", levels.RunDecayPerSec);
            for (int i = 0; i < g.Snacks.Count; i++)
            {
                var snack = g.Snacks[i];
                NonNegative(errors, $"snacks[{i}].totalRise", snack.TotalRise);
                NonNegative(errors, $"snacks[{i}].onsetSec", snack.OnsetSec);
                Positive(errors, $"snacks[{i}].absorbSec", snack.AbsorbSec);
            }

            // 4, 5
            NonNegative(errors, "display.delaySec", g.Display.DelaySec);
            if (!(g.Display.WarningThreshold >= levels.HypoThreshold))
                errors.Add($"display.warningThreshold ≥ glucose.hypoThreshold 이어야 한다 " +
                           $"({g.Display.WarningThreshold} < {levels.HypoThreshold})");

            // 6. chancePerSec는 1초당 확률이므로 1을 넘을 수 없다.
            if (!(g.SuddenDrop.ChancePerSec >= 0 && g.SuddenDrop.ChancePerSec <= 1))
                errors.Add($"suddenDrop.chancePerSec는 0 이상 1 이하여야 한다 ({g.SuddenDrop.ChancePerSec})");
            Positive(errors, "suddenDrop.amount", g.SuddenDrop.Amount);
            Positive(errors, "suddenDrop.durationSec", g.SuddenDrop.DurationSec);

            // 7
            Positive(errors, "recurrence.cooldownSec", g.Recurrence.CooldownSec);

            // 8
            var duplicateIds = g.Snacks.GroupBy(s => s.Id).Where(group => group.Count() > 1).Select(group => group.Key)
                .ToList();
            if (duplicateIds.Count > 0)
                errors.Add("snacks: id가 중복된다 (" + string.Join(", ", duplicateIds) + ")");

            var fast = g.Snacks.Where(s => s.Kind == SnackKind.Fast).ToList();
            var slow = g.Snacks.Where(s => s.Kind == SnackKind.Slow).ToList();
            if (fast.Count > 0 && slow.Count > 0)
            {
                double latestFast = fast.Max(s => s.OnsetSec);
                double earliestSlow = slow.Min(s => s.OnsetSec);
                if (!(latestFast < earliestSlow))
                    errors.Add($"snacks: 모든 fast 간식의 onsetSec < 모든 slow 간식의 onsetSec 이어야 한다 " +
                               $"(fast 최대 {latestFast}, slow 최소 {earliestSlow})");
            }

            // 9
            Positive(errors, "time.timeScale", g.Time.TimeScale);
            Positive(errors, "time.recheckIntervalRealMin", g.Time.RecheckIntervalRealMin);

            // 10. 4단계 재측정에 빠른 당의 효과가 보여야 한다.
            if (fast.Count > 0)
            {
                double latestFast = fast.Max(s => s.OnsetSec);
                double recheck = g.Time.RecheckIntervalGameSec;
                if (!(latestFast < recheck))
                    errors.Add($"snacks: 모든 fast 간식의 onsetSec < 재측정 간격(게임 초) 이어야 한다 " +
                               $"({latestFast} ≥ {recheck})");
            }

            // 11
            if (string.IsNullOrWhiteSpace(config.ConfigVersion))
                errors.Add("configVersion이 비어 있다");
            if (config.Unit != GameConfig.RequiredUnit)
                errors.Add($"unit은 {GameConfig.RequiredUnit} 이어야 한다 ({config.Unit})");

            // 12. 핵심 교차 불변식
            if (!(config.Hypo.Step1TimeLimitSec > g.Display.DelaySec))
                errors.Add($"hypo.step1TimeLimitSec > display.delaySec 이어야 한다 " +
                           $"({config.Hypo.Step1TimeLimitSec} ≤ {g.Display.DelaySec})");

            ValidateHypo(errors, config);

            // 19. 트랙 간식은 처치량보다 작아야 한다(주행 중 혈당을 조금만 올린다).
            var runner = config.Runner;
            if (!(0 < runner.TrackSnackServings && runner.TrackSnackServings < config.Hypo.Doses.Under))
                errors.Add($"0 < runner.trackSnackServings < hypo.doses.under 이어야 한다 " +
                           $"(trackSnackServings={runner.TrackSnackServings}, under={config.Hypo.Doses.Under})");
            Positive(errors, "runner.runDurationSec", runner.RunDurationSec);

            return errors;
        }

        // 13~18. 저혈당 이벤트(HypoEventFsm) 설정.
        private static void ValidateHypo(List<string> errors, GameConfig config)
        {
            var g = config.Glucose;
            var levels = g.Levels;
            var hypo = config.Hypo;
            var doses = hypo.Doses;

            // 13
            if (!(levels.HypoThreshold <= levels.NormalRangeMin && levels.NormalRangeMin < levels.HyperThreshold))
                errors.Add($"glucose: hypoThreshold ≤ normalRangeMin < hyperThreshold 이어야 한다 " +
                           $"(hypoThreshold={levels.HypoThreshold}, normalRangeMin={levels.NormalRangeMin}, " +
                           $"hyperThreshold={levels.HyperThreshold})");

            // W가 유효하지 않으면 9번이 이미 보고하므로 W에서 나온 14·18번은 건너뛴다.
            double recheck = g.Time.RecheckIntervalGameSec;
            bool recheckValid = recheck > 0 && !double.IsInfinity(recheck);

            // 14
            if (recheckValid && !(recheck > g.Display.DelaySec))
                errors.Add($"재측정 간격(게임 초) > display.delaySec 이어야 한다 ({recheck} ≤ {g.Display.DelaySec})");
            double tick = g.Time.FastForwardTickSimSec;
            if (!(tick > 0 && tick < g.Display.DelaySec))
                errors.Add($"0 < time.fastForwardTickSimSec < display.delaySec 이어야 한다 " +
                           $"(tick={tick}, delaySec={g.Display.DelaySec})");

            // 15
            Positive(errors, "hypo.step2TimeLimitSec", hypo.Step2TimeLimitSec);
            Positive(errors, "hypo.step3TimeLimitSec", hypo.Step3TimeLimitSec);
            Positive(errors, "hypo.step4TimeLimitSec", hypo.Step4TimeLimitSec);
            Positive(errors, "hypo.guardianPromptTimeLimitSec", hypo.GuardianPromptTimeLimitSec);
            Positive(errors, "hypo.resumeCountdownSec", hypo.ResumeCountdownSec);
            Positive(errors, "hypo.graceSec", hypo.GraceSec);

            // 16
            if (!(0 < doses.Under && doses.Under < doses.Proper && doses.Proper < doses.Over))
                errors.Add($"hypo.doses: 0 < under < proper < over 이어야 한다 " +
                           $"(under={doses.Under}, proper={doses.Proper}, over={doses.Over})");

            // 17
            var guardianSnack = g.Snacks.FirstOrDefault(s => s.Id == hypo.GuardianSnackId);
            if (guardianSnack == null || guardianSnack.Kind != SnackKind.Fast)
                errors.Add($"hypo.guardianSnackId는 snacks에 있는 fast 간식이어야 한다 ({hypo.GuardianSnackId})");

            // 18. 최악의 경우에서 적정량을 먹고 재측정 빨리 감기를 마치면 실제·표시 혈당 모두 정상 범위여야 한다.
            if (!recheckValid) return;
            double worstAtDose = WorstGlucoseAtDose(config);
            if (!(worstAtDose >= levels.Min))
                errors.Add($"최악의 경우 처치 시점 혈당이 glucose.min 아래로 내려간다 ({worstAtDose} < {levels.Min})");
            foreach (var snack in g.Snacks.Where(s => s.Kind == SnackKind.Fast))
            {
                double actual = RecoveredGlucose(config, snack, recheck);
                double displayed = RecoveredGlucose(config, snack, recheck - g.Display.DelaySec);
                if (actual >= levels.NormalRangeMin && displayed >= levels.NormalRangeMin) continue;

                errors.Add($"적정 처치 후 재측정 혈당이 normalRangeMin({levels.NormalRangeMin}) 미만이다 " +
                           $"(간식 {snack.Id}: 실제 {actual}, 표시 {displayed})");
            }
        }

        /// <summary>
        /// 불변식 18의 보수적 하한. 처치 시점 혈당이 min 아래로 가는 설정은 따로 위반으로 본다.
        /// </summary>
        /// <param name="absorbWindow">투입부터 흡수가 진행된 simTime.</param>
        private static double RecoveredGlucose(GameConfig config, SnackConfig snack, double absorbWindow)
        {
            var g = config.Glucose;
            var hypo = config.Hypo;
            double baseDecay = g.Levels.BaseDecayPerSec;
            double guardian = hypo.GuardianPromptTimeLimitSec;
            double recheck = g.Time.RecheckIntervalGameSec;

            double worstAtDose = WorstGlucoseAtDose(config);
            double absorbedRatio = Math.Max(0, Math.Min(1, (absorbWindow - snack.OnsetSec) / snack.AbsorbSec));
            double absorbed = snack.TotalRise * hypo.Doses.Proper * absorbedRatio;
            double decayAfterDose = baseDecay * (hypo.Step4TimeLimitSec + guardian + recheck);
            return worstAtDose + absorbed - decayAfterDose;
        }

        /// <summary>
        /// 발동 직후 급하강 전량, 1단계는 주행 중, 1~3단계 모두 오류 후 알리기 대기를 최대로 쓴 경우의 처치 시점 혈당.
        /// </summary>
        private static double WorstGlucoseAtDose(GameConfig config)
        {
            var g = config.Glucose;
            var levels = g.Levels;
            var hypo = config.Hypo;
            double baseDecay = levels.BaseDecayPerSec;
            return levels.HypoThreshold - g.SuddenDrop.Amount
                   - (baseDecay + levels.RunDecayPerSec) * hypo.Step1TimeLimitSec
                   - baseDecay * (3 * hypo.GuardianPromptTimeLimitSec + hypo.Step2TimeLimitSec + hypo.Step3TimeLimitSec);
        }

        // NaN도 위반으로 잡도록 부정형으로 비교한다.
        private static void NonNegative(List<string> errors, string path, double value)
        {
            if (!(value >= 0)) errors.Add($"{path} ≥ 0 이어야 한다 ({value})");
        }

        private static void Positive(List<string> errors, string path, double value)
        {
            if (!(value > 0)) errors.Add($"{path} > 0 이어야 한다 ({value})");
        }
    }
}
