using System.Collections.Generic;
using System.Linq;
using SugarDash.Domain.Config;

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
        public string GuardianSnackId = "juice";
        public double DoseUnder = 1;
        public double DoseProper = 2;
        public double DoseOver = 3;

        public double TrackSnackServings = 0.5;
        public double RunDurationSec = 120;
        public double TrackSnackIntervalSec = 8;

        public List<Snack> Snacks = new List<Snack>
        {
            new Snack { Id = "juice", Kind = SnackKind.Fast, TotalRise = 30, OnsetSec = 1, AbsorbSec = 2 },
            new Snack { Id = "cracker", Kind = SnackKind.Slow, TotalRise = 30, OnsetSec = 4, AbsorbSec = 8 },
        };

        public Snack SnackById(string id) => Snacks.First(s => s.Id == id);

        public GlucoseConfig BuildGlucose() => new GlucoseConfig(
            new TimeConfig(TimeScale, RecheckIntervalRealMin, RecheckIntervalSource, FastForwardTickSimSec),
            new GlucoseLevelConfig(Initial, Min, Max, HypoThreshold, HypoReleaseMargin, HyperThreshold,
                BaseDecayPerSec, RunDecayPerSec, NormalRangeMin),
            new RecurrenceConfig(CooldownSec),
            new DisplayConfig(DelaySec, WarningThreshold),
            new SuddenDropConfig(SuddenDropChancePerSec, SuddenDropAmount, SuddenDropDurationSec),
            Snacks.Select(s => new SnackConfig(s.Id, s.Kind, s.TotalRise, s.OnsetSec, s.AbsorbSec, s.Source))
                .ToList());

        public GameConfig Build() => new GameConfig(ConfigVersion, Unit, BuildGlucose(), new HypoConfig(
            Step1TimeLimitSec, Step2TimeLimitSec, Step3TimeLimitSec, Step4TimeLimitSec, GuardianPromptTimeLimitSec,
            ResumeCountdownSec, GraceSec, GuardianSnackId, new DoseConfig(DoseUnder, DoseProper, DoseOver)),
            new RunnerConfig(TrackSnackServings, RunDurationSec, TrackSnackIntervalSec));
    }
}
