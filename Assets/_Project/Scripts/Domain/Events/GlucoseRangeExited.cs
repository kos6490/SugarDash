namespace SugarDash.Domain.Events
{
    /// <summary>실제 혈당이 적정 범위(normalRangeMin 이상, normalRangeMax 미만)를 벗어났다.</summary>
    public sealed class GlucoseRangeExited
    {
        public GlucoseRangeExited(double actual, double simT, double t)
        {
            Actual = actual;
            SimT = simT;
            T = t;
        }

        /// <summary>판정한 순간의 실제 혈당.</summary>
        public double Actual { get; }
        /// <summary>simDt 누적 시각.</summary>
        public double SimT { get; }
        /// <summary>decisionDt 누적 시각.</summary>
        public double T { get; }
    }
}
