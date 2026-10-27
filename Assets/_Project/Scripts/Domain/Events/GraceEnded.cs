namespace SugarDash.Domain.Events
{
    /// <summary>유예 시간이 끝났다.</summary>
    public sealed class GraceEnded
    {
        public GraceEnded(int eventId, double simT, double t)
        {
            EventId = eventId;
            SimT = simT;
            T = t;
        }

        public int EventId { get; }
        /// <summary>simDt 누적 시각.</summary>
        public double SimT { get; }
        /// <summary>decisionDt 누적 시각.</summary>
        public double T { get; }
    }
}
