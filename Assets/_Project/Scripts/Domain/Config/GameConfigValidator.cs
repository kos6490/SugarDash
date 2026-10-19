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

            return errors;
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
