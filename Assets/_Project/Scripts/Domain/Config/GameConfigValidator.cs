using System;
using System.Collections.Generic;
using System.Linq;
using SugarDash.Domain.Hypo;

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
            if (config.TrackType == TrackType.Type2)
            {
                ValidateType2(errors, config);
                return errors;
            }
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
            if (!(levels.HyperReleaseMargin > 0))
                errors.Add($"glucose.hyperReleaseMargin > 0 이어야 한다 ({levels.HyperReleaseMargin})");
            // 저혈당·고혈당의 해제 구간이 겹치지 않아야 한다.
            if (!(levels.HypoReleaseLevel < levels.HyperReleaseLevel))
                errors.Add($"glucose: hypoThreshold + hypoReleaseMargin < hyperThreshold − hyperReleaseMargin 이어야 한다 " +
                           $"({levels.HypoReleaseLevel} ≥ {levels.HyperReleaseLevel})");

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
            if (!(0 < runner.TrackSnackIntervalMinSec && runner.TrackSnackIntervalMinSec <= runner.TrackSnackIntervalMaxSec))
                errors.Add($"0 < runner.trackSnackIntervalMinSec ≤ runner.trackSnackIntervalMaxSec 이어야 한다 " +
                           $"(min={runner.TrackSnackIntervalMinSec}, max={runner.TrackSnackIntervalMaxSec})");

            // 20. 고혈당 알리기(docs/hyper-event.md)
            Positive(errors, "hyper.notifyTimeLimitSec", config.Hyper.NotifyTimeLimitSec);

            // 21. 혈당 안전 범위 탈락 기준
            var elimination = config.Elimination;
            if (!(levels.Min <= elimination.GlucoseLow && elimination.GlucoseLow < levels.HypoThreshold))
                errors.Add($"glucose.min ≤ elimination.glucoseLow < glucose.hypoThreshold 이어야 한다 " +
                           $"(min={levels.Min}, glucoseLow={elimination.GlucoseLow}, hypoThreshold={levels.HypoThreshold})");
            if (!(levels.HyperThreshold < elimination.GlucoseHigh && elimination.GlucoseHigh <= levels.Max))
                errors.Add($"glucose.hyperThreshold < elimination.glucoseHigh ≤ glucose.max 이어야 한다 " +
                           $"(hyperThreshold={levels.HyperThreshold}, glucoseHigh={elimination.GlucoseHigh}, max={levels.Max})");

            // 24. 증상 연출: 흔들림은 탈락 기준 위에서 시작해야 하고 혈당 범위를 벗어나지 않는다
            double shakeStart = config.Symptom.ShakeStartGlucose;
            if (!(elimination.GlucoseLow < shakeStart && shakeStart <= levels.Max))
                errors.Add($"elimination.glucoseLow < symptom.shakeStartGlucose ≤ glucose.max 이어야 한다 " +
                           $"(glucoseLow={elimination.GlucoseLow}, shakeStartGlucose={shakeStart}, max={levels.Max})");

            // 22. 점수
            ValidateScore(errors, config.Score, Type1Steps, Type1Codes,
                new[] { HypoErrorCode.RsKeepRunning, HypoErrorCode.HyKeepRunning });

            // 23. 구간
            ValidateSections(errors, config);

            // 25. 혈당 재기 습관 순간(docs/section2.md 7.1절)
            ValidateHabitCheck(errors, config);

            return errors;
        }

        private static void ValidateHabitCheck(List<string> errors, GameConfig config)
        {
            if (config.Type1 != null)
            {
                Positive(errors, "type1.habitCheckWindowSec", config.Type1.HabitCheckWindowSec);
                return;
            }
            bool used = config.Sections.Where(s => s.HasScenes).SelectMany(s => s.Scenes).Any(s => s.HabitCheck.HasValue);
            if (used) errors.Add("type1: habitCheck를 쓰는 장면이 있으면 type1.habitCheckWindowSec가 있어야 한다");
        }

        private static readonly HypoStep[] Type1Steps =
        {
            HypoStep.Recognize, HypoStep.Respond, HypoStep.Dose, HypoStep.Remeasure, HypoStep.Notify,
            HypoStep.HyperNotify,
        };

        private static readonly HypoErrorCode[] Type1Codes =
        {
            HypoErrorCode.SMissed, HypoErrorCode.RsSlow, HypoErrorCode.RsKeepRunning, HypoErrorCode.RsNone,
            HypoErrorCode.RsTimeout, HypoErrorCode.DsUnder, HypoErrorCode.DsOver, HypoErrorCode.DsTimeout,
            HypoErrorCode.RcNoRecheck, HypoErrorCode.RcNoNotify, HypoErrorCode.HelpInstead,
            HypoErrorCode.HyKeepRunning, HypoErrorCode.HyTimeout,
        };

        private static readonly HypoStep[] Type2Steps = { HypoStep.HyperNotify, HypoStep.LowNotify };

        private static readonly HypoErrorCode[] Type2Codes =
        {
            HypoErrorCode.HyKeepRunning, HypoErrorCode.HyJuice, HypoErrorCode.HyTimeout,
            HypoErrorCode.LoKeepRunning, HypoErrorCode.LoTimeout,
        };

        private static readonly HypoErrorCode[] Type2ZeroCodes = { HypoErrorCode.HyKeepRunning, HypoErrorCode.LoKeepRunning };

        // 22. 유형마다 필요한 단계·오류 코드와 0 고정 코드가 다르다.
        private static void ValidateScore(List<string> errors, ScoreConfig score, IReadOnlyList<HypoStep> steps,
            IReadOnlyList<HypoErrorCode> codes, IReadOnlyList<HypoErrorCode> zeroCodes)
        {
            NonNegative(errors, "score.inRangePerSec", score.InRangePerSec);
            NonNegative(errors, "score.perMeter", score.PerMeter);
            foreach (var step in steps)
            {
                if (!score.CorrectWeights.TryGetValue(step, out var weight))
                    errors.Add($"score.correct: {step} 없음");
                else
                    NonNegative(errors, $"score.correct.{step}", weight);
            }
            foreach (var code in codes)
            {
                if (!score.ErrorWeights.TryGetValue(code, out var weight))
                    errors.Add($"score.errors: {code} 없음");
                else
                    NonNegative(errors, $"score.errors.{code}", weight);
            }
            // 위험한 선택은 0점이다.
            foreach (var code in zeroCodes)
            {
                if (score.ErrorWeights.TryGetValue(code, out var weight) && weight != 0)
                    errors.Add($"score.errors.{code}는 0 이어야 한다 ({weight})");
            }
        }

        // 2형 불변식(설계 8.3절). 1형 전용 규칙(측정기, 표시 지연, 4단계, 급하강, 흔들림)은 검사하지 않는다.
        private static void ValidateType2(List<string> errors, GameConfig config)
        {
            var levels = config.Glucose.Levels;
            var t2 = config.Type2;
            if (t2 == null)
            {
                errors.Add("type2 설정이 없다");
                return;
            }

            if (string.IsNullOrWhiteSpace(config.ConfigVersion))
                errors.Add("configVersion이 비어 있다");
            if (config.Unit != GameConfig.RequiredUnit)
                errors.Add($"unit은 {GameConfig.RequiredUnit} 이어야 한다 ({config.Unit})");
            Positive(errors, "time.timeScale", config.Glucose.Time.TimeScale);

            if (!(levels.Min < levels.HypoThreshold && levels.HypoThreshold < levels.Initial &&
                  levels.Initial < levels.HyperThreshold && levels.HyperThreshold <= levels.Max))
                errors.Add($"glucose: min < hypoThreshold < initial < hyperThreshold ≤ max 이어야 한다 " +
                           $"(min={levels.Min}, hypoThreshold={levels.HypoThreshold}, initial={levels.Initial}, " +
                           $"hyperThreshold={levels.HyperThreshold}, max={levels.Max})");
            if (!(levels.HypoReleaseMargin > 0))
                errors.Add($"glucose.hypoReleaseMargin > 0 이어야 한다 ({levels.HypoReleaseMargin})");
            if (!(levels.HyperReleaseMargin > 0))
                errors.Add($"glucose.hyperReleaseMargin > 0 이어야 한다 ({levels.HyperReleaseMargin})");
            NonNegative(errors, "glucose.baseDecayPerSec", levels.BaseDecayPerSec);
            NonNegative(errors, "glucose.runDecayPerSec", levels.RunDecayPerSec);

            // 1
            var elimination = config.Elimination;
            if (!(levels.Min <= elimination.GlucoseLow && elimination.GlucoseLow < levels.HypoThreshold))
                errors.Add($"glucose.min ≤ elimination.glucoseLow < glucose.hypoThreshold 이어야 한다 " +
                           $"(min={levels.Min}, glucoseLow={elimination.GlucoseLow}, hypoThreshold={levels.HypoThreshold})");
            if (!(levels.HyperThreshold < elimination.GlucoseHigh && elimination.GlucoseHigh <= levels.Max))
                errors.Add($"glucose.hyperThreshold < elimination.glucoseHigh ≤ glucose.max 이어야 한다 " +
                           $"(hyperThreshold={levels.HyperThreshold}, glucoseHigh={elimination.GlucoseHigh}, max={levels.Max})");
            // 2
            if (!(levels.HypoReleaseLevel < levels.HyperReleaseLevel))
                errors.Add($"glucose: hypoThreshold + hypoReleaseMargin < hyperThreshold − hyperReleaseMargin 이어야 한다 " +
                           $"({levels.HypoReleaseLevel} ≥ {levels.HyperReleaseLevel})");
            // 3
            if (!(levels.HypoThreshold <= levels.NormalRangeMin && levels.NormalRangeMin < levels.HyperThreshold))
                errors.Add($"glucose: hypoThreshold ≤ normalRangeMin < hyperThreshold 이어야 한다 " +
                           $"(hypoThreshold={levels.HypoThreshold}, normalRangeMin={levels.NormalRangeMin}, " +
                           $"hyperThreshold={levels.HyperThreshold})");

            // 4
            NonNegative(errors, "type2.highHoldSec", t2.HighHoldSec);
            NonNegative(errors, "type2.lowHoldSec", t2.LowHoldSec);
            NonNegative(errors, "type2.noticeSec", t2.NoticeSec);
            NonNegative(errors, "type2.answerFeedbackSec", t2.AnswerFeedbackSec);
            Positive(errors, "type2.tellTimeLimitSec", t2.TellTimeLimitSec);
            Positive(errors, "type2.guardianPromptTimeLimitSec", t2.GuardianPromptTimeLimitSec);
            Positive(errors, "type2.resumeCountdownSec", t2.ResumeCountdownSec);
            Positive(errors, "type2.graceSec", t2.GraceSec);
            Positive(errors, "type2.restDurationSec", t2.RestDurationSec);

            // 5
            if (!(0 < t2.FoodRowIntervalMinSec && t2.FoodRowIntervalMinSec <= t2.FoodRowIntervalMaxSec))
                errors.Add($"0 < type2.foodRowIntervalMinSec ≤ type2.foodRowIntervalMaxSec 이어야 한다 " +
                           $"(min={t2.FoodRowIntervalMinSec}, max={t2.FoodRowIntervalMaxSec})");
            if (!(0 < t2.RestIntervalMinSec && t2.RestIntervalMinSec <= t2.RestIntervalMaxSec))
                errors.Add($"0 < type2.restIntervalMinSec ≤ type2.restIntervalMaxSec 이어야 한다 " +
                           $"(min={t2.RestIntervalMinSec}, max={t2.RestIntervalMaxSec})");

            // 6, 7
            var foods = config.Glucose.Snacks;
            if (foods.Count < 2)
                errors.Add($"foods: 2개 이상이어야 한다 ({foods.Count})");
            var duplicateIds = foods.GroupBy(f => f.Id).Where(group => group.Count() > 1).Select(group => group.Key)
                .ToList();
            if (duplicateIds.Count > 0)
                errors.Add("foods: id가 중복된다 (" + string.Join(", ", duplicateIds) + ")");
            if (!foods.Any(f => f.Id == t2.GuardianSnackId))
                errors.Add($"type2.guardianSnackId는 foods에 있어야 한다 ({t2.GuardianSnackId})");
            if (!foods.Any(f => f.Id == t2.JuiceChoiceFoodId))
                errors.Add($"type2.juiceChoiceFoodId는 foods에 있어야 한다 ({t2.JuiceChoiceFoodId})");
            for (int i = 0; i < foods.Count; i++)
            {
                NonNegative(errors, $"foods[{i}].totalRise", foods[i].TotalRise);
                NonNegative(errors, $"foods[{i}].onsetSec", foods[i].OnsetSec);
                Positive(errors, $"foods[{i}].absorbSec", foods[i].AbsorbSec);
            }

            ValidateScore(errors, config.Score, Type2Steps, Type2Codes, Type2ZeroCodes);
            ValidateSections(errors, config);
        }

        private static readonly string[] SectionIds = { "signal", "condition", "complex" };

        private static void ValidateSections(List<string> errors, GameConfig config)
        {
            var sections = config.Sections;
            if (sections.Count == 0)
            {
                errors.Add("sections: 1개 이상이어야 한다");
                return;
            }
            var seen = new HashSet<string>();
            for (int i = 0; i < sections.Count; i++)
            {
                var section = sections[i];
                if (Array.IndexOf(SectionIds, section.Id) < 0)
                    errors.Add($"sections[{i}].id는 signal, condition, complex 중 하나여야 한다 ({section.Id})");
                else if (!seen.Add(section.Id))
                    errors.Add($"sections[{i}].id가 중복된다 ({section.Id})");

                bool last = i == sections.Count - 1;
                if (section.HasScenes)
                {
                    if (!last)
                        errors.Add($"sections[{i}]: 장면이 있는 구간은 마지막 구간이어야 한다 ({section.Id})");
                    if (section.LengthM != null)
                        errors.Add($"sections[{i}].lengthM은 장면이 있는 구간에서 비어 있어야 한다 ({section.LengthM})");
                    ValidateScenes(errors, config, i, section.Scenes);
                }
                else if (last)
                {
                    if (section.LengthM != null)
                        errors.Add($"sections[{i}].lengthM은 마지막 구간에서 비어 있어야 한다 ({section.LengthM})");
                }
                else if (!(section.LengthM.HasValue && section.LengthM.Value > 0))
                {
                    errors.Add($"sections[{i}].lengthM > 0 이어야 한다 ({section.LengthM})");
                }
            }
        }

        private static void ValidateScenes(List<string> errors, GameConfig config, int sectionIndex,
            IReadOnlyList<SceneConfig> scenes)
        {
            if (scenes.Count == 0)
            {
                errors.Add($"sections[{sectionIndex}].scenes: 1개 이상이어야 한다");
                return;
            }
            var seen = new HashSet<string>();
            for (int j = 0; j < scenes.Count; j++)
            {
                var scene = scenes[j];
                string path = $"sections[{sectionIndex}].scenes[{j}]";
                if (string.IsNullOrEmpty(scene.Id))
                    errors.Add($"{path}.id는 비어 있으면 안 된다");
                else if (!seen.Add(scene.Id))
                    errors.Add($"{path}.id가 중복된다 ({scene.Id})");

                // 장면은 되풀이되므로 끝없는 장면은 안 된다.
                double? length = scene.LengthM;
                if (!(length.HasValue && length.Value > 0 && !double.IsInfinity(length.Value)))
                    errors.Add($"{path}.lengthM > 0 이고 유한해야 한다 ({length})");

                if (config.TrackType == TrackType.Type2) OtherTypeKey(errors, path, "habitCheck", scene.HabitCheck.HasValue);
                ValidateOverrides(errors, config, path, scene.Overrides);
            }
        }

        // 덮어쓴 값은 전역 값과 같은 규칙으로 검사한다. 간격은 덮어쓴 뒤의 min·max 쌍으로 본다.
        private static void ValidateOverrides(List<string> errors, GameConfig config, string path, SceneOverrides o)
        {
            if (o.RunDecayPerSec.HasValue)
                NonNegative(errors, $"{path}.runDecayPerSec", o.RunDecayPerSec.Value);

            if (config.TrackType == TrackType.Type2)
            {
                OtherTypeKey(errors, path, "suddenDropChancePerSec", o.SuddenDropChancePerSec.HasValue);
                OtherTypeKey(errors, path, "trackSnackIds", o.TrackSnackIds != null);
                OtherTypeKey(errors, path, "trackSnackIntervalMinSec", o.TrackSnackIntervalMinSec.HasValue);
                OtherTypeKey(errors, path, "trackSnackIntervalMaxSec", o.TrackSnackIntervalMaxSec.HasValue);
                if (o.FoodRowIds != null)
                {
                    var foods = config.Glucose.Snacks;
                    // 음식 줄에는 서로 다른 음식 2개가 놓인다.
                    if (o.FoodRowIds.Distinct().Count() < 2)
                        errors.Add($"{path}.foodRowIds: 서로 다른 id가 2개 이상이어야 한다");
                    foreach (var id in o.FoodRowIds.Where(id => foods.All(f => f.Id != id)).Distinct())
                        errors.Add($"{path}.foodRowIds는 foods에 있어야 한다 ({id})");
                }
                var t2 = config.Type2;
                IntervalOverride(errors, path, "foodRowInterval", o.FoodRowIntervalMinSec, o.FoodRowIntervalMaxSec,
                    t2.FoodRowIntervalMinSec, t2.FoodRowIntervalMaxSec);
                IntervalOverride(errors, path, "restInterval", o.RestIntervalMinSec, o.RestIntervalMaxSec,
                    t2.RestIntervalMinSec, t2.RestIntervalMaxSec);
                return;
            }

            OtherTypeKey(errors, path, "foodRowIds", o.FoodRowIds != null);
            OtherTypeKey(errors, path, "foodRowIntervalMinSec", o.FoodRowIntervalMinSec.HasValue);
            OtherTypeKey(errors, path, "foodRowIntervalMaxSec", o.FoodRowIntervalMaxSec.HasValue);
            OtherTypeKey(errors, path, "restIntervalMinSec", o.RestIntervalMinSec.HasValue);
            OtherTypeKey(errors, path, "restIntervalMaxSec", o.RestIntervalMaxSec.HasValue);
            if (o.SuddenDropChancePerSec.HasValue)
            {
                double chance = o.SuddenDropChancePerSec.Value;
                if (!(chance >= 0 && chance <= 1))
                    errors.Add($"{path}.suddenDropChancePerSec는 0 이상 1 이하여야 한다 ({chance})");
            }
            if (o.TrackSnackIds != null)
            {
                var snacks = config.Glucose.Snacks;
                if (o.TrackSnackIds.Count == 0)
                    errors.Add($"{path}.trackSnackIds: 1개 이상이어야 한다");
                foreach (var id in o.TrackSnackIds.Where(id => snacks.All(s => s.Id != id)).Distinct())
                    errors.Add($"{path}.trackSnackIds는 snacks에 있어야 한다 ({id})");
            }
            var runner = config.Runner;
            IntervalOverride(errors, path, "trackSnackInterval", o.TrackSnackIntervalMinSec, o.TrackSnackIntervalMaxSec,
                runner.TrackSnackIntervalMinSec, runner.TrackSnackIntervalMaxSec);
        }

        private static void OtherTypeKey(List<string> errors, string path, string key, bool present)
        {
            if (present) errors.Add($"{path}.{key}는 이 유형 설정에 쓸 수 없다");
        }

        private static void IntervalOverride(List<string> errors, string path, string key, double? min, double? max,
            double globalMin, double globalMax)
        {
            if (!min.HasValue && !max.HasValue) return;
            double lo = min ?? globalMin;
            double hi = max ?? globalMax;
            if (!(0 < lo && lo <= hi))
                errors.Add($"0 < {path}.{key}MinSec ≤ {path}.{key}MaxSec 이어야 한다 (덮어쓴 뒤 min={lo}, max={hi})");
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
            NonNegative(errors, "hypo.answerFeedbackSec", hypo.AnswerFeedbackSec);

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
            double decayAfterDose = baseDecay * (hypo.Step4TimeLimitSec + guardian + recheck
                                                 + FeedbackWindowsAfterDose * ValidFeedbackSec(hypo));
            return worstAtDose + absorbed - decayAfterDose;
        }

        /// <summary>
        /// 처치 전 결과 문구 창의 최대 횟수. 1단계 시간 초과 → 알리기 대기(1) → 2단계(2) → 오류 → 알리기 대기(3)
        /// → 3단계(4) → 오류·시간 초과 → 알리기 대기(5) → 처치. 발동 직후 1단계에는 문구 창이 없다.
        /// </summary>
        private const int FeedbackWindowsBeforeDose = 5;

        /// <summary>
        /// 처치 뒤 재측정 빨리 감기까지 결과 문구 창의 최대 횟수. 처치 → 4단계 ①(1) → 오류·시간 초과
        /// → 알리기 대기(2) → 어른이 재측정.
        /// </summary>
        private const int FeedbackWindowsAfterDose = 2;

        /// <summary>문구 창 길이. 15번이 이미 보고한 잘못된 값(음수·NaN)이면 18번 계산에는 0으로 둔다.</summary>
        private static double ValidFeedbackSec(HypoConfig hypo) =>
            hypo.AnswerFeedbackSec >= 0 ? hypo.AnswerFeedbackSec : 0;

        /// <summary>
        /// 발동 직후 급하강 전량, 1단계는 주행 중, 1~3단계 모두 오류 후 알리기 대기를 최대로 쓴 경우의 처치 시점 혈당.
        /// 결과 문구 창(세계는 멈추고 혈당 시간은 흐른다)도 최대 횟수만큼 기본 하강으로 깎는다.
        /// </summary>
        private static double WorstGlucoseAtDose(GameConfig config)
        {
            var g = config.Glucose;
            var levels = g.Levels;
            var hypo = config.Hypo;
            double baseDecay = levels.BaseDecayPerSec;
            return levels.HypoThreshold - g.SuddenDrop.Amount
                   - (baseDecay + MaxRunDecay(config)) * hypo.Step1TimeLimitSec
                   - baseDecay * (3 * hypo.GuardianPromptTimeLimitSec + hypo.Step2TimeLimitSec + hypo.Step3TimeLimitSec
                                  + FeedbackWindowsBeforeDose * ValidFeedbackSec(hypo));
        }

        /// <summary>
        /// 1단계(주행 계속) 동안의 달리기 감소. 저혈당은 어느 장면에서든 시작될 수 있으므로
        /// 전역 값과 장면 덮어쓰기 중 가장 큰 값을 쓴다.
        /// </summary>
        private static double MaxRunDecay(GameConfig config)
        {
            double max = config.Glucose.Levels.RunDecayPerSec;
            foreach (var section in config.Sections)
            {
                if (!section.HasScenes) continue;
                foreach (var scene in section.Scenes)
                {
                    double? o = scene.Overrides.RunDecayPerSec;
                    if (o.HasValue && o.Value > max) max = o.Value;
                }
            }
            return max;
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
