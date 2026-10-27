namespace SugarDash.Domain.Config
{
    /// <summary>설정 파일 하나에 대응하는 전체 설정.</summary>
    public sealed class GameConfig
    {
        public const string RequiredUnit = "mg/dL";

        public GameConfig(string configVersion, string unit, GlucoseConfig glucose, HypoConfig hypo,
            RunnerConfig runner)
        {
            ConfigVersion = configVersion;
            Unit = unit;
            Glucose = glucose;
            Hypo = hypo;
            Runner = runner;
        }

        /// <summary>로그 레코드마다 함께 저장한다.</summary>
        public string ConfigVersion { get; }
        public string Unit { get; }
        public GlucoseConfig Glucose { get; }
        public HypoConfig Hypo { get; }
        public RunnerConfig Runner { get; }
    }
}
