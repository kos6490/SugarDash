namespace SugarDash.Domain.Events
{
    /// <summary>2형 주행 중 앉아서 쉬기를 끝내고 다시 달린다.</summary>
    public sealed class RestEnded
    {
        public RestEnded(double actual, double simT, double t)
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
