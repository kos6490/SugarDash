using System;
using System.Collections.Generic;
using SugarDash.Domain.Config;
using SugarDash.Domain.Hypo;
using SugarDash.Domain.Ports;
using SugarDash.Infrastructure.Logging;
using SugarDash.Infrastructure.Config.Dto;
using Newtonsoft.Json;

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
            GameConfigDto dto;
            try
            {
                dto = JsonConvert.DeserializeObject<GameConfigDto>(_json, Settings);
            }
            catch (JsonException e)
            {
                throw new ConfigValidationException(Array.Empty<string>(), new[] { "JSON 형식 오류: " + e.Message });
            }

            var reader = new Reader();
            var config = reader.Map(dto);
            if (reader.Missing.Count > 0 || reader.Violations.Count > 0)
                throw new ConfigValidationException(reader.Missing, reader.Violations);

            var violations = GameConfigValidator.Validate(config);
            if (violations.Count > 0) throw new ConfigValidationException(Array.Empty<string>(), violations);

            return config;
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
                    Number(glucose?.NormalRangeMin, "glucose.normalRangeMin", glucose));

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
                    Text(hypo?.GuardianSnackId, "hypo.guardianSnackId", hypo),
                    new DoseConfig(
                        Number(doses?.Under, "hypo.doses.under", doses),
                        Number(doses?.Proper, "hypo.doses.proper", doses),
                        Number(doses?.Over, "hypo.doses.over", doses)));

                var hyper = Section(dto.Hyper, "hyper");
                var hyperConfig = new HyperConfig(
                    Number(hyper?.NotifyTimeLimitSec, "hyper.notifyTimeLimitSec", hyper));

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
                    Number(runner?.RunDurationSec, "runner.runDurationSec", runner),
                    Number(runner?.TrackSnackIntervalMinSec, "runner.trackSnackIntervalMinSec", runner),
                    Number(runner?.TrackSnackIntervalMaxSec, "runner.trackSnackIntervalMaxSec", runner),
                    Integer(runner?.Hearts, "runner.hearts", runner));

                var elimination = Section(dto.Elimination, "elimination");
                var eliminationConfig = new EliminationConfig(
                    Number(elimination?.GlucoseLow, "elimination.glucoseLow", elimination),
                    Number(elimination?.GlucoseHigh, "elimination.glucoseHigh", elimination));

                var scoreConfig = MapScore(Section(dto.Score, "score"));

                var sections = new List<SectionConfig>();
                var sectionDtos = Section(dto.Sections, "sections");
                for (int i = 0; sectionDtos != null && i < sectionDtos.Count; i++)
                {
                    string path = $"sections[{i}]";
                    var section = Section(sectionDtos[i], path);
                    bool last = i == sectionDtos.Count - 1;
                    double? length = section?.LengthM;
                    // 마지막 구간만 길이가 없다(끝없이 이어진다).
                    if (!last && !length.HasValue && section != null) Missing.Add(path + ".lengthM");
                    sections.Add(new SectionConfig(Text(section?.Id, path + ".id", section), length));
                }

                var glucoseConfig = new GlucoseConfig(timeConfig, levels, recurrenceConfig, displayConfig, dropConfig,
                    snacks);
                return new GameConfig(configVersion, unit, glucoseConfig, hypoConfig, hyperConfig,
                    runnerConfig, eliminationConfig, scoreConfig, sections);
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
