using System.Collections.Generic;

namespace SugarDash.Domain.Config
{
    /// <summary>설정 파일 하나에 대응하는 전체 설정.</summary>
    public sealed class GameConfig
    {
        public const string RequiredUnit = "mg/dL";

        public GameConfig(string configVersion, string unit, GlucoseConfig glucose, HypoConfig hypo,
            HyperConfig hyper, RunnerConfig runner, EliminationConfig elimination, SymptomConfig symptom, ScoreConfig score,
            IReadOnlyList<SectionConfig> sections, TrackType trackType = TrackType.Type1, Type2Config type2 = null)
        {
            ConfigVersion = configVersion;
            Unit = unit;
            Glucose = glucose;
            Hypo = hypo;
            Hyper = hyper;
            Runner = runner;
            Elimination = elimination;
            Symptom = symptom;
            Score = score;
            Sections = sections;
            TrackType = trackType;
            Type2 = type2;
        }

        /// <summary>
        /// 2형 설정에서 GameConfig를 만든다. 1형 전용 값은 쓰지 않는 중립값으로 채운다.
        /// <paramref name="foods"/>는 이미 게임 초 단위다.
        /// </summary>
        public static GameConfig ForType2(string configVersion, string unit, double timeScale,
            GlucoseLevelConfig levels, IReadOnlyList<SnackConfig> foods, Type2Config type2,
            EliminationConfig elimination, ScoreConfig score, IReadOnlyList<SectionConfig> sections)
        {
            // 2형에서 쓰지 않음: 재측정, 표시 지연, 급하강, 재발, 4단계 제한 시간, 처치량, 화면 흔들림.
            var glucose = new GlucoseConfig(
                new TimeConfig(timeScale, 0, "", 0), levels, new RecurrenceConfig(0),
                new DisplayConfig(0, double.NegativeInfinity), new SuddenDropConfig(0, 0, 0), foods);
            var hypo = new HypoConfig(0, 0, 0, 0, type2.GuardianPromptTimeLimitSec, type2.ResumeCountdownSec,
                type2.GraceSec, type2.AnswerFeedbackSec, type2.GuardianSnackId, new DoseConfig(0, 0, 0));
            return new GameConfig(configVersion, unit, glucose, hypo, new HyperConfig(type2.TellTimeLimitSec),
                new RunnerConfig(1, type2.FoodRowIntervalMinSec, type2.FoodRowIntervalMaxSec), elimination,
                new SymptomConfig(double.NegativeInfinity), score, sections, TrackType.Type2, type2);
        }

        /// <summary>로그 레코드마다 함께 저장한다.</summary>
        public string ConfigVersion { get; }
        public string Unit { get; }
        public GlucoseConfig Glucose { get; }
        public HypoConfig Hypo { get; }
        public HyperConfig Hyper { get; }
        public RunnerConfig Runner { get; }
        public EliminationConfig Elimination { get; }
        public SymptomConfig Symptom { get; }
        public ScoreConfig Score { get; }
        public IReadOnlyList<SectionConfig> Sections { get; }
        public TrackType TrackType { get; }
        /// <summary>2형 설정. 1형이면 null.</summary>
        public Type2Config Type2 { get; }
    }
}
