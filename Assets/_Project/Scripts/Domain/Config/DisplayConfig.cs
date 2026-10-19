namespace SugarDash.Domain.Config
{
    /// <summary>표시 혈당 설정.</summary>
    public sealed class DisplayConfig
    {
        public DisplayConfig(double delaySec, double warningThreshold)
        {
            DelaySec = delaySec;
            WarningThreshold = warningThreshold;
        }

        /// <summary>표시 혈당이 실제 혈당보다 늦는 시간(게임 초).</summary>
        public double DelaySec { get; }
        public double WarningThreshold { get; }
    }
}
