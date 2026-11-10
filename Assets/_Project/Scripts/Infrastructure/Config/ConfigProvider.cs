using System;
using System.Collections.Generic;
using SugarDash.Domain.Config;
using SugarDash.Domain.Hypo;
using SugarDash.Domain.Ports;
using SugarDash.Infrastructure.Logging;
using SugarDash.Infrastructure.Config.Dto;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SugarDash.Infrastructure.Config
{
    /// <summary>
    /// <see cref="IConfigSource"/> 구현. JSON을 DTO로 읽어 도메인 설정으로 바꾸고 불변식을 검증한다.
    /// 비어 있는 값(PLACEHOLDER)과 불변식 위반을 모두 모아 한 번에 실패시킨다.
    /// </summary>
    public sealed class ConfigProvider : IConfigSource
    {
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MissingMemberHandling = MissingMemberHandling.Error, // 오타 난 키를 잡는다
        };

        private readonly string _json;

        public ConfigProvider(string json) => _json = json ?? throw new ArgumentNullException(nameof(json));

        public GameConfig Load()
        {
            string trackType;
            JObject root;
            try
            {
                root = JObject.Parse(_json);
                trackType = root.Value<string>("trackType");
            }
            catch (Exception e) when (e is JsonException || e is InvalidCastException)
            {
                throw new ConfigValidationException(Array.Empty<string>(), new[] { "JSON 형식 오류: " + e.Message });
            }

            if (trackType == null) throw new ConfigValidationException(new[] { "trackType" }, Array.Empty<string>());
            if (trackType != "type1" && trackType != "type2")
                throw new ConfigValidationException(Array.Empty<string>(),
                    new[] { $"trackType은 type1 또는 type2 이어야 한다 ({trackType})" });

            var reader = new Reader();
            GameConfig config;
            try
            {
                if (trackType == "type2")
                    config = reader.MapType2(JsonConvert.DeserializeObject<Type2ConfigDto>(_json, Settings));
                else
                    config = reader.Map(JsonConvert.DeserializeObject<GameConfigDto>(_json, Settings));
            }
            catch (JsonException e)
            {
                throw new ConfigValidationException(Array.Empty<string>(), new[] { "JSON 형식 오류: " + e.Message });
            }

            reader.Missing.AddRange(NullOverrideKeys(root));
            if (reader.Missing.Count > 0 || reader.Violations.Count > 0)
                throw new ConfigValidationException(reader.Missing, reader.Violations);

            var violations = GameConfigValidator.Validate(config);
            if (violations.Count > 0) throw new ConfigValidationException(Array.Empty<string>(), violations);

            return config;
        }

        /// <summary>
        /// 장면 덮어쓰기에서 적었지만 값이 null인 키의 경로. DTO로는 "적지 않음"과 구분되지 않아 JSON에서 직접 찾는다.
        /// 빌드 설정은 값이 정해질 장면 조건을 이렇게 null로 적어 둔다.
        /// </summary>
        private static IEnumerable<string> NullOverrideKeys(JObject root)
        {
            if (!(root["sections"] is JArray sections)) yield break;
            for (int i = 0; i < sections.Count; i++)
            {
                if (!(sections[i] is JObject section) || !(section["scenes"] is JArray scenes)) continue;
                for (int j = 0; j < scenes.Count; j++)
                {
                    if (!(scenes[j] is JObject scene) || !(scene["overrides"] is JObject overrides)) continue;
                    foreach (var key in overrides.Properties())
                        if (key.Value.Type == JTokenType.Null)
                            yield return $"sections[{i}].scenes[{j}].overrides.{key.Name}";
                }
            }
        }

        /// <summary>DTO를 도메인 설정으로 바꾸며 빈 값의 경로를 모은다. 빈 값이 있으면 결과는 버린다.</summary>
        private sealed class Reader
        {
            public readonly List<string> Missing = new List<string>();
            public readonly List<string> Violations = new List<string>();

            public GameConfig Map(GameConfigDto dto)
            {
                if (dto == null)
                {
                    Missing.Add("(root)");
                    return null;
                }

                string configVersion = Text(dto.ConfigVersion, "configVersion", dto);
                string unit = Text(dto.Unit, "unit", dto);

                var time = Section(dto.Time, "time");
                var timeConfig = new TimeConfig(
                    Number(time?.TimeScale, "time.timeScale", time),
                    Number(time?.RecheckIntervalRealMin, "time.recheckIntervalRealMin", time),
                    Text(time?.RecheckIntervalSource, "time.recheckIntervalSource", time),
                    Number(time?.FastForwardTickSimSec, "time.fastForwardTickSimSec", time));

                var glucose = Section(dto.Glucose, "glucose");
                var levels = new GlucoseLevelConfig(
                    Number(glucose?.Initial, "glucose.initial", glucose),
                    Number(glucose?.Min, "glucose.min", glucose),
                    Number(glucose?.Max, "glucose.max", glucose),
                    Number(glucose?.HypoThreshold, "glucose.hypoThreshold", glucose),
                    Number(glucose?.HypoReleaseMargin, "glucose.hypoReleaseMargin", glucose),
                    Number(glucose?.HyperThreshold, "glucose.hyperThreshold", glucose),
                    Number(glucose?.HyperReleaseMargin, "glucose.hyperReleaseMargin", glucose),
                    Number(glucose?.BaseDecayPerSec, "glucose.baseDecayPerSec", glucose),
                    Number(glucose?.RunDecayPerSec, "glucose.runDecayPerSec", glucose),
                    Number(glucose?.NormalRangeMin, "glucose.normalRangeMin", glucose),
                    Number(glucose?.NormalRangeMax, "glucose.normalRangeMax", glucose));

                var recurrence = Section(dto.Recurrence, "recurrence");
                var recurrenceConfig = new RecurrenceConfig(
                    Number(recurrence?.CooldownSec, "recurrence.cooldownSec", recurrence));

                var display = Section(dto.Display, "display");
                var displayConfig = new DisplayConfig(
                    Number(display?.DelaySec, "display.delaySec", display),
                    Number(display?.WarningThreshold, "display.warningThreshold", display));

                var hypo = Section(dto.Hypo, "hypo");
                var doses = hypo == null ? null : Section(hypo.Doses, "hypo.doses");
                var hypoConfig = new HypoConfig(
                    Number(hypo?.Step1TimeLimitSec, "hypo.step1TimeLimitSec", hypo),
                    Number(hypo?.Step2TimeLimitSec, "hypo.step2TimeLimitSec", hypo),
                    Number(hypo?.Step3TimeLimitSec, "hypo.step3TimeLimitSec", hypo),
                    Number(hypo?.Step4TimeLimitSec, "hypo.step4TimeLimitSec", hypo),
                    Number(hypo?.GuardianPromptTimeLimitSec, "hypo.guardianPromptTimeLimitSec", hypo),
                    Number(hypo?.ResumeCountdownSec, "hypo.resumeCountdownSec", hypo),
                    Number(hypo?.GraceSec, "hypo.graceSec", hypo),
                    Number(hypo?.AnswerFeedbackSec, "hypo.answerFeedbackSec", hypo),
                    Text(hypo?.GuardianSnackId, "hypo.guardianSnackId", hypo),
                    new DoseConfig(
                        Number(doses?.Under, "hypo.doses.under", doses),
                        Number(doses?.Proper, "hypo.doses.proper", doses),
                        Number(doses?.Over, "hypo.doses.over", doses)));

                var hyper = Section(dto.Hyper, "hyper");
                var hyperConfig = new HyperConfig(
                    Number(hyper?.NotifyTimeLimitSec, "hyper.notifyTimeLimitSec", hyper));

                var type1 = Section(dto.Type1, "type1");
                var type1Config = new Type1Config(
                    Number(type1?.HabitCheckWindowSec, "type1.habitCheckWindowSec", type1));

                var drop = Section(dto.SuddenDrop, "suddenDrop");
                var dropConfig = new SuddenDropConfig(
                    Number(drop?.ChancePerSec, "suddenDrop.chancePerSec", drop),
                    Number(drop?.Amount, "suddenDrop.amount", drop),
                    Number(drop?.DurationSec, "suddenDrop.durationSec", drop));

                var snacks = new List<SnackConfig>();
                var snackDtos = Section(dto.Snacks, "snacks");
                for (int i = 0; snackDtos != null && i < snackDtos.Count; i++)
                {
                    string path = $"snacks[{i}]";
                    var snack = Section(snackDtos[i], path);
                    snacks.Add(new SnackConfig(
                        Text(snack?.Id, path + ".id", snack),
                        Kind(snack?.Kind, path + ".kind", snack),
                        Number(snack?.TotalRise, path + ".totalRise", snack),
                        Number(snack?.OnsetSec, path + ".onsetSec", snack),
                        Number(snack?.AbsorbSec, path + ".absorbSec", snack),
                        Text(snack?.Source, path + ".source", snack)));
                }

                var runner = Section(dto.Runner, "runner");
                var runnerConfig = new RunnerConfig(
                    Number(runner?.TrackSnackServings, "runner.trackSnackServings", runner),
                    Number(runner?.TrackSnackIntervalMinSec, "runner.trackSnackIntervalMinSec", runner),
                    Number(runner?.TrackSnackIntervalMaxSec, "runner.trackSnackIntervalMaxSec", runner));

                var elimination = Section(dto.Elimination, "elimination");
                var eliminationConfig = new EliminationConfig(
                    Number(elimination?.GlucoseLow, "elimination.glucoseLow", elimination),
                    Number(elimination?.GlucoseHigh, "elimination.glucoseHigh", elimination));

                var symptom = Section(dto.Symptom, "symptom");
                var symptomConfig = new SymptomConfig(
                    Number(symptom?.ShakeStartGlucose, "symptom.shakeStartGlucose", symptom));

                var scoreConfig = MapScore(Section(dto.Score, "score"));

                var sections = MapSections(dto.Sections);

                var glucoseConfig = new GlucoseConfig(timeConfig, levels, recurrenceConfig, displayConfig, dropConfig,
                    snacks);
                return new GameConfig(configVersion, unit, glucoseConfig, hypoConfig, hyperConfig,
                    runnerConfig, eliminationConfig, symptomConfig, scoreConfig, sections,
                    type1: type1Config);
            }

            public GameConfig MapType2(Type2ConfigDto dto)
            {
                if (dto == null)
                {
                    Missing.Add("(root)");
                    return null;
                }

                string configVersion = Text(dto.ConfigVersion, "configVersion", dto);
                string unit = Text(dto.Unit, "unit", dto);

                var time = Section(dto.Time, "time");
                double timeScale = Number(time?.TimeScale, "time.timeScale", time);

                var glucose = Section(dto.Glucose, "glucose");
                var levels = new GlucoseLevelConfig(
                    Number(glucose?.Initial, "glucose.initial", glucose),
                    Number(glucose?.Min, "glucose.min", glucose),
                    Number(glucose?.Max, "glucose.max", glucose),
                    Number(glucose?.HypoThreshold, "glucose.hypoThreshold", glucose),
                    Number(glucose?.HypoReleaseMargin, "glucose.hypoReleaseMargin", glucose),
                    Number(glucose?.HyperThreshold, "glucose.hyperThreshold", glucose),
                    Number(glucose?.HyperReleaseMargin, "glucose.hyperReleaseMargin", glucose),
                    Number(glucose?.BaseDecayPerSec, "glucose.baseDecayPerSec", glucose),
                    Number(glucose?.RunDecayPerSec, "glucose.runDecayPerSec", glucose),
                    Number(glucose?.NormalRangeMin, "glucose.normalRangeMin", glucose),
                    Number(glucose?.NormalRangeMax, "glucose.normalRangeMax", glucose));

                var t2 = Section(dto.Type2, "type2");
                var runner = Section(dto.Runner, "runner");
                var type2 = new Type2Config(
                    Number(t2?.HighHoldSec, "type2.highHoldSec", t2),
                    Number(t2?.LowHoldSec, "type2.lowHoldSec", t2),
                    Number(t2?.NoticeSec, "type2.noticeSec", t2),
                    Number(t2?.TellTimeLimitSec, "type2.tellTimeLimitSec", t2),
                    Number(t2?.GuardianPromptTimeLimitSec, "type2.guardianPromptTimeLimitSec", t2),
                    Number(t2?.AnswerFeedbackSec, "type2.answerFeedbackSec", t2),
                    Number(t2?.ResumeCountdownSec, "type2.resumeCountdownSec", t2),
                    Number(t2?.GraceSec, "type2.graceSec", t2),
                    Text(t2?.GuardianSnackId, "type2.guardianSnackId", t2),
                    Text(t2?.JuiceChoiceFoodId, "type2.juiceChoiceFoodId", t2),
                    Number(runner?.FoodRowIntervalMinSec, "runner.foodRowIntervalMinSec", runner),
                    Number(runner?.FoodRowIntervalMaxSec, "runner.foodRowIntervalMaxSec", runner),
                    Number(runner?.RestIntervalMinSec, "runner.restIntervalMinSec", runner),
                    Number(runner?.RestIntervalMaxSec, "runner.restIntervalMaxSec", runner),
                    Number(runner?.RestDurationSec, "runner.restDurationSec", runner));

                // timeScale이 0이면 불변식 검증이 먼저 실패해야 하므로 0으로 나누지 않는다.
                double toGameSec = timeScale > 0 ? 60 / timeScale : 0;
                var foods = new List<SnackConfig>();
                var foodDtos = Section(dto.Foods, "foods");
                for (int i = 0; foodDtos != null && i < foodDtos.Count; i++)
                {
                    string path = $"foods[{i}]";
                    var food = Section(foodDtos[i], path);
                    // Kind는 2형에서 쓰지 않는다(빠른·느린 당 구분은 1형 대처용).
                    foods.Add(new SnackConfig(
                        Text(food?.Id, path + ".id", food),
                        SnackKind.Fast,
                        Number(food?.TotalRise, path + ".totalRise", food),
                        Number(food?.OnsetRealMin, path + ".onsetRealMin", food) * toGameSec,
                        Number(food?.AbsorbRealMin, path + ".absorbRealMin", food) * toGameSec,
                        Text(food?.Source, path + ".source", food)));
                }

                var elimination = Section(dto.Elimination, "elimination");
                var eliminationConfig = new EliminationConfig(
                    Number(elimination?.GlucoseLow, "elimination.glucoseLow", elimination),
                    Number(elimination?.GlucoseHigh, "elimination.glucoseHigh", elimination));

                var score = MapType2Score(Section(dto.Score, "score"));
                var sections = MapSections(dto.Sections);

                return GameConfig.ForType2(configVersion, unit, timeScale, levels, foods, type2,
                    eliminationConfig, score, sections);
            }

            private ScoreConfig MapType2Score(Type2ScoreDto score)
            {
                var correct = score == null ? null : Section(score.Correct, "score.correct");
                var correctWeights = new Dictionary<HypoStep, double>
                {
                    { HypoStep.HyperNotify, Number(correct?.HyperNotify, "score.correct.hyperNotify", correct) },
                    { HypoStep.LowNotify, Number(correct?.LowNotify, "score.correct.lowNotify", correct) },
                };

                var errors = score == null ? null : Section(score.Errors, "score.errors");
                var errorWeights = new Dictionary<HypoErrorCode, double>
                {
                    { HypoErrorCode.HyKeepRunning, Error(errors?.HyKeepRunning, HypoErrorCode.HyKeepRunning, errors) },
                    { HypoErrorCode.HyJuice, Error(errors?.HyJuice, HypoErrorCode.HyJuice, errors) },
                    { HypoErrorCode.HyTimeout, Error(errors?.HyTimeout, HypoErrorCode.HyTimeout, errors) },
                    { HypoErrorCode.LoKeepRunning, Error(errors?.LoKeepRunning, HypoErrorCode.LoKeepRunning, errors) },
                    { HypoErrorCode.LoTimeout, Error(errors?.LoTimeout, HypoErrorCode.LoTimeout, errors) },
                };

                return new ScoreConfig(
                    Number(score?.InRangePerSec, "score.inRangePerSec", score),
                    Number(score?.PerMeter, "score.perMeter", score),
                    correctWeights, errorWeights);
            }

            private List<SectionConfig> MapSections(List<SectionDto> sectionDtos)
            {
                var sections = new List<SectionConfig>();
                sectionDtos = Section(sectionDtos, "sections");
                for (int i = 0; sectionDtos != null && i < sectionDtos.Count; i++)
                {
                    string path = $"sections[{i}]";
                    var section = Section(sectionDtos[i], path);
                    bool last = i == sectionDtos.Count - 1;
                    double? length = section?.LengthM;
                    // 마지막 구간과 장면이 있는 구간은 길이가 없다. 마지막이 아닌 장면 구간은 하루를 도는 횟수(days)로 끝난다.
                    bool hasScenes = section?.Scenes != null;
                    if (!last && !hasScenes && !length.HasValue && section != null) Missing.Add(path + ".lengthM");
                    if (!last && hasScenes && !section.Days.HasValue) Missing.Add(path + ".days");
                    var scenes = hasScenes ? MapScenes(section.Scenes, path + ".scenes") : null;
                    sections.Add(new SectionConfig(Text(section?.Id, path + ".id", section), length, scenes, section?.Days));
                }
                return sections;
            }

            private List<SceneConfig> MapScenes(List<SceneDto> sceneDtos, string sectionPath)
            {
                var scenes = new List<SceneConfig>();
                for (int j = 0; j < sceneDtos.Count; j++)
                {
                    string path = $"{sectionPath}[{j}]";
                    var scene = Section(sceneDtos[j], path);
                    if (scene == null) continue;
                    var o = scene.Overrides;
                    var overrides = o == null
                        ? SceneOverrides.None
                        : new SceneOverrides(o.RunDecayPerSec, o.SuddenDropChancePerSec, o.TrackSnackIds,
                            o.TrackSnackIntervalMinSec, o.TrackSnackIntervalMaxSec, o.FoodRowIntervalMinSec,
                            o.FoodRowIntervalMaxSec, o.RestIntervalMinSec, o.RestIntervalMaxSec, o.FoodRowIds, o.EmptyBagChance);
                    if (!scene.LengthM.HasValue) Missing.Add(path + ".lengthM");
                    scenes.Add(new SceneConfig(Text(scene.Id, path + ".id", scene),
                        TimeOfDayOf(scene.TimeOfDay, path + ".timeOfDay", scene), scene.LengthM,
                        scene.Spawns ?? true, overrides, HabitMomentOf(scene.HabitCheck, path + ".habitCheck")));
                }
                return scenes;
            }

            // 적지 않으면 습관 순간이 없다.
            private HabitMoment? HabitMomentOf(string value, string path)
            {
                switch (value)
                {
                    case null: return null;
                    case "preExercise": return HabitMoment.PreExercise;
                    case "bedtime": return HabitMoment.Bedtime;
                    default:
                        Violations.Add($"{path}는 preExercise 또는 bedtime 이어야 한다 ({value})");
                        return null;
                }
            }

            private TimeOfDay TimeOfDayOf(string value, string path, object section)
            {
                switch (value)
                {
                    case "morning": return TimeOfDay.Morning;
                    case "afternoon": return TimeOfDay.Afternoon;
                    case "evening": return TimeOfDay.Evening;
                    case "night": return TimeOfDay.Night;
                    case null:
                        if (section != null) Missing.Add(path);
                        return default;
                    default:
                        Violations.Add($"{path}는 morning, afternoon, evening, night 중 하나여야 한다 ({value})");
                        return default;
                }
            }

            private ScoreConfig MapScore(ScoreDto score)
            {
                var correct = score == null ? null : Section(score.Correct, "score.correct");
                var correctWeights = new Dictionary<HypoStep, double>
                {
                    { HypoStep.Recognize, Number(correct?.Recognize, "score.correct.recognize", correct) },
                    { HypoStep.Respond, Number(correct?.Respond, "score.correct.respond", correct) },
                    { HypoStep.Dose, Number(correct?.Dose, "score.correct.dose", correct) },
                    { HypoStep.Remeasure, Number(correct?.Remeasure, "score.correct.remeasure", correct) },
                    { HypoStep.Notify, Number(correct?.Notify, "score.correct.notify", correct) },
                    { HypoStep.HyperNotify, Number(correct?.HyperNotify, "score.correct.hyperNotify", correct) },
                };

                var errors = score == null ? null : Section(score.Errors, "score.errors");
                var errorWeights = new Dictionary<HypoErrorCode, double>
                {
                    { HypoErrorCode.SMissed, Error(errors?.SMissed, HypoErrorCode.SMissed, errors) },
                    { HypoErrorCode.RsSlow, Error(errors?.RsSlow, HypoErrorCode.RsSlow, errors) },
                    { HypoErrorCode.RsKeepRunning, Error(errors?.RsKeepRunning, HypoErrorCode.RsKeepRunning, errors) },
                    { HypoErrorCode.RsNone, Error(errors?.RsNone, HypoErrorCode.RsNone, errors) },
                    { HypoErrorCode.RsTimeout, Error(errors?.RsTimeout, HypoErrorCode.RsTimeout, errors) },
                    { HypoErrorCode.HelpInstead, Error(errors?.HelpInstead, HypoErrorCode.HelpInstead, errors) },
                    { HypoErrorCode.DsUnder, Error(errors?.DsUnder, HypoErrorCode.DsUnder, errors) },
                    { HypoErrorCode.DsOver, Error(errors?.DsOver, HypoErrorCode.DsOver, errors) },
                    { HypoErrorCode.DsTimeout, Error(errors?.DsTimeout, HypoErrorCode.DsTimeout, errors) },
                    { HypoErrorCode.RcNoRecheck, Error(errors?.RcNoRecheck, HypoErrorCode.RcNoRecheck, errors) },
                    { HypoErrorCode.RcNoNotify, Error(errors?.RcNoNotify, HypoErrorCode.RcNoNotify, errors) },
                    { HypoErrorCode.HyKeepRunning, Error(errors?.HyKeepRunning, HypoErrorCode.HyKeepRunning, errors) },
                    { HypoErrorCode.HyTimeout, Error(errors?.HyTimeout, HypoErrorCode.HyTimeout, errors) },
                };

                return new ScoreConfig(
                    Number(score?.InRangePerSec, "score.inRangePerSec", score),
                    Number(score?.PerMeter, "score.perMeter", score),
                    correctWeights, errorWeights);
            }

            private double Error(double? value, HypoErrorCode code, object section) =>
                Number(value, "score.errors." + LogCodes.Error(code), section);

            private T Section<T>(T section, string path) where T : class
            {
                if (section == null) Missing.Add(path);
                return section;
            }

            // section이 이미 없으면 하위 경로는 따로 보고하지 않는다.
            private double Number(double? value, string path, object section)
            {
                if (value.HasValue) return value.Value;
                if (section != null) Missing.Add(path);
                return 0;
            }

            private int Integer(int? value, string path, object section)
            {
                if (value.HasValue) return value.Value;
                if (section != null) Missing.Add(path);
                return 0;
            }

            private string Text(string value, string path, object section)
            {
                if (value == null && section != null) Missing.Add(path);
                return value;
            }

            private SnackKind Kind(string value, string path, object section)
            {
                switch (value)
                {
                    case "fast": return SnackKind.Fast;
                    case "slow": return SnackKind.Slow;
                    case null:
                        if (section != null) Missing.Add(path);
                        return default;
                    default:
                        Violations.Add($"{path}는 fast 또는 slow 이어야 한다 ({value})");
                        return default;
                }
            }
        }
    }
}
