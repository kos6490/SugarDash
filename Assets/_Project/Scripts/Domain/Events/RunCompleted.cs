namespace SugarDash.Domain.Events
{
    /// <summary>주행 길이(runner.runDurationSec)만큼 달려 주행을 마쳤다.</summary>
    public sealed class RunCompleted
    {
        public RunCompleted(double worldT, double simT, double t)
        {
            WorldT = worldT;
            SimT = simT;
            T = t;
        }

        /// <summary>worldDt 누적 시각.</summary>
        public double WorldT { get; }
        /// <summary>simDt 누적 시각.</summary>
        public double SimT { get; }
        /// <summary>decisionDt 누적 시각.</summary>
        public double T { get; }
    }
}
