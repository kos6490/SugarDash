using System.Collections.Generic;

namespace SugarDash.Domain.Config
{
    /// <summary>설정 파일 하나에 대응하는 전체 설정.</summary>
    public sealed class GameConfig
    {
        public const string RequiredUnit = "mg/dL";

        public GameConfig(string configVersion, string unit, GlucoseConfig glucose, HypoConfig hypo,
            HyperConfig hyper, RunnerConfig runner, EliminationConfig elimination, SymptomConfig symptom, ScoreConfig score,
            IReadOnlyList<SectionConfig> sections)
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
    }
}
