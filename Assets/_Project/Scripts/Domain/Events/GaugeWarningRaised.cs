namespace SugarDash.Domain.Events
{
    /// <summary>표시 혈당이 경고선 아래로 내려가 게이지 경고가 켜졌다.</summary>
    public sealed class GaugeWarningRaised
    {
        public GaugeWarningRaised(double simT, double t, double displayed)
        {
            SimT = simT;
            T = t;
            Displayed = displayed;
        }

        /// <summary>simDt 누적 시각.</summary>
        public double SimT { get; }
        /// <summary>decisionDt 누적 시각.</summary>
        public double T { get; }
        public double Displayed { get; }
    }
}
