namespace SugarDash.Domain.Events
{
    /// <summary>급하강 이벤트가 시작됐다.</summary>
    public sealed class SuddenDropStarted
    {
        public SuddenDropStarted(double simT, double t)
        {
            SimT = simT;
            T = t;
        }

        /// <summary>simDt 누적 시각.</summary>
        public double SimT { get; }
        /// <summary>decisionDt 누적 시각.</summary>
        public double T { get; }
    }
}
