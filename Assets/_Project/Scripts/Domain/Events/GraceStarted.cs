namespace SugarDash.Domain.Events
{
    /// <summary>재개 후 유예 시간이 시작됐다. 장애물을 만들지 않는다.</summary>
    public sealed class GraceStarted
    {
        public GraceStarted(int eventId, double simT, double t)
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
