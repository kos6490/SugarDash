namespace SugarDash.Domain.Config
{
    /// <summary>혈당 안전 범위 탈락 설정.</summary>
    public sealed class EliminationConfig
    {
        public EliminationConfig(double glucoseLow, double glucoseHigh)
        {
            GlucoseLow = glucoseLow;
            GlucoseHigh = glucoseHigh;
        }

        /// <summary>실제 혈당이 이 값 아래로 내려가면 탈락 대상이다.</summary>
        public double GlucoseLow { get; }
        /// <summary>실제 혈당이 이 값을 넘으면 탈락 대상이다.</summary>
        public double GlucoseHigh { get; }
    }
}
