using System;
using System.Collections.Generic;
using System.Linq;
using SugarDash.Domain.Config;
using SugarDash.Domain.Hypo;

namespace SugarDash.Domain.Tests
{
    /// <summary>
    /// 테스트 전용 설정 픽스처. 여기 수치는 계산을 검증하기 위한 값이며 실제 게임 수치가 아니다.
    /// </summary>
    public sealed class ConfigBuilder
    {
        public sealed class Snack
        {
            public string Id;
            public SnackKind Kind;
            public double TotalRise;
            public double OnsetSec;
            public double AbsorbSec;
            public string Source = "test";
        }

        public string ConfigVersion = "test";
        public string Unit = GameConfig.RequiredUnit;

        public double TimeScale = 150;               // 재측정 15분 → 6 게임 초
        public double RecheckIntervalRealMin = 15;
        public string RecheckIntervalSource = "test";
        public double FastForwardTickSimSec = 0.5;

        public double Initial = 120;
        public double Min = 20;
        public double Max = 400;
        public double HypoThreshold = 70;
        public double HypoReleaseMargin = 10;
        public double HyperThreshold = 250;
        public double HyperReleaseMargin = 20;
        public double BaseDecayPerSec = 0;
        public double RunDecayPerSec = 0;
        public double NormalRangeMin = 80;

        public double CooldownSec = 10;

        public double DelaySec = 3;
        public double WarningThreshold = 80;

        public double SuddenDropChancePerSec = 0;
        public double SuddenDropAmount = 30;
        public double SuddenDropDurationSec = 5;

        public double Step1TimeLimitSec = 5;
        public double Step2TimeLimitSec = 5;
        public double Step3TimeLimitSec = 5;
        public double Step4TimeLimitSec = 5;
        public double GuardianPromptTimeLimitSec = 5;
        public double ResumeCountdownSec = 1;
        public double GraceSec = 3;
        /// <summary>결과 문구 창. 0이면 문구 창 없이 바로 다음 단계로 간다(기존 타이밍 테스트 유지).</summary>
        public double AnswerFeedbackSec = 0;
        public string GuardianSnackId = "juice";
        public double DoseUnder = 1;
        public double DoseProper = 2;
        public double DoseOver = 3;

        public double HyperNotifyTimeLimitSec = 5;

        public double TrackSnackServings = 0.5;
        public double TrackSnackIntervalMinSec = 8;
        public double TrackSnackIntervalMaxSec = 8;

        public double GlucoseLow = 40;
        public double GlucoseHigh = 300;
        public double ShakeStartGlucose = 90;

        public double InRangePerSec = 1;
        public double PerMeter = 0.1;
        public double CorrectWeight = 10;
        public double ErrorWeight = 0;
        /// <summary>ErrorWeight 기본값 위에 덮어쓰는 오류별 가중치.</summary>
        public Dictionary<HypoErrorCode, double> ErrorWeightOverrides = new Dictionary<HypoErrorCode, double>();

        // 2형 설정(BuildType2). 나머지 공유 값은 위 필드를 쓴다.
        public double HighHoldSec = 2;
        public double LowHoldSec = 2;
        public double NoticeSec = 1;
        public double TellTimeLimitSec = 5;
        public string JuiceChoiceFoodId = "juice";
        public double FoodRowIntervalMinSec = 4;
        public double FoodRowIntervalMaxSec = 4;
        public double RestIntervalMinSec = 10;
        public double RestIntervalMaxSec = 10;
        public double RestDurationSec = 3;

        public List<SectionConfig> Sections = new List<SectionConfig> { new SectionConfig("signal", null) };

        public List<Snack> Snacks = new List<Snack>
        {
            new Snack { Id = "juice", Kind = SnackKind.Fast, TotalRise = 30, OnsetSec = 1, AbsorbSec = 2 },
            new Snack { Id = "cracker", Kind = SnackKind.Slow, TotalRise = 30, OnsetSec = 4, AbsorbSec = 8 },
        };

        public Snack SnackById(string id) => Snacks.First(s => s.Id == id);

        public GlucoseConfig BuildGlucose() => new GlucoseConfig(
            new TimeConfig(TimeScale, RecheckIntervalRealMin, RecheckIntervalSource, FastForwardTickSimSec),
            new GlucoseLevelConfig(Initial, Min, Max, HypoThreshold, HypoReleaseMargin, HyperThreshold,
                HyperReleaseMargin, BaseDecayPerSec, RunDecayPerSec, NormalRangeMin),
            new RecurrenceConfig(CooldownSec),
            new DisplayConfig(DelaySec, WarningThreshold),
            new SuddenDropConfig(SuddenDropChancePerSec, SuddenDropAmount, SuddenDropDurationSec),
            Snacks.Select(s => new SnackConfig(s.Id, s.Kind, s.TotalRise, s.OnsetSec, s.AbsorbSec, s.Source))
                .ToList());

        public GameConfig Build() => new GameConfig(ConfigVersion, Unit, BuildGlucose(), new HypoConfig(
            Step1TimeLimitSec, Step2TimeLimitSec, Step3TimeLimitSec, Step4TimeLimitSec, GuardianPromptTimeLimitSec,
            ResumeCountdownSec, GraceSec, AnswerFeedbackSec, GuardianSnackId, new DoseConfig(DoseUnder, DoseProper, DoseOver)),
            new HyperConfig(HyperNotifyTimeLimitSec),
            new RunnerConfig(TrackSnackServings, TrackSnackIntervalMinSec, TrackSnackIntervalMaxSec),
            new EliminationConfig(GlucoseLow, GlucoseHigh),
            new SymptomConfig(ShakeStartGlucose),
            BuildScore(),
            Sections.ToList());

        /// <summary>2형 설정. 음식은 기존 Snacks 목록을 그대로 쓴다.</summary>
        public GameConfig BuildType2() => GameConfig.ForType2(ConfigVersion, Unit, TimeScale,
            new GlucoseLevelConfig(Initial, Min, Max, HypoThreshold, HypoReleaseMargin, HyperThreshold,
                HyperReleaseMargin, BaseDecayPerSec, RunDecayPerSec, NormalRangeMin),
            Snacks.Select(s => new SnackConfig(s.Id, s.Kind, s.TotalRise, s.OnsetSec, s.AbsorbSec, s.Source))
                .ToList(),
            new Type2Config(HighHoldSec, LowHoldSec, NoticeSec, TellTimeLimitSec, GuardianPromptTimeLimitSec,
                AnswerFeedbackSec, ResumeCountdownSec, GraceSec, GuardianSnackId, JuiceChoiceFoodId,
                FoodRowIntervalMinSec, FoodRowIntervalMaxSec, RestIntervalMinSec, RestIntervalMaxSec,
                RestDurationSec),
            new EliminationConfig(GlucoseLow, GlucoseHigh),
            BuildScore(),
            Sections.ToList());

        private ScoreConfig BuildScore()
        {
            var correct = Enum.GetValues(typeof(HypoStep)).Cast<HypoStep>().ToDictionary(step => step, _ => CorrectWeight);
            var errors = Enum.GetValues(typeof(HypoErrorCode)).Cast<HypoErrorCode>()
                .Where(code => code != HypoErrorCode.None)
                .ToDictionary(code => code, code => ErrorWeightOverrides.TryGetValue(code, out var w) ? w : ErrorWeight);
            return new ScoreConfig(InRangePerSec, PerMeter, correct, errors);
        }
    }
}
