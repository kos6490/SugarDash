namespace SugarDash.Domain.Events
{
    /// <summary>2형 주행 중 쉬는 구간에서 앉아서 쉬기 시작했다. 정답·오답이나 점수와는 무관하다.</summary>
    public sealed class RestStarted
    {
        public RestStarted(double actual, double simT, double t)
        {
            Actual = actual;
            SimT = simT;
            T = t;
        }

        public double Actual { get; }
        /// <summary>simDt 누적 시각.</summary>
        public double SimT { get; }
        /// <summary>decisionDt 누적 시각.</summary>
        public double T { get; }
    }
}
