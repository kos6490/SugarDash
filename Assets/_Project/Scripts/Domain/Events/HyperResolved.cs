namespace SugarDash.Domain.Events
{
    /// <summary>고혈당 이벤트에서 어른에게 알렸다.</summary>
    public sealed class HyperResolved
    {
        public HyperResolved(int eventId, bool allCorrect, double simT, double t)
        {
            EventId = eventId;
            AllCorrect = allCorrect;
            SimT = simT;
            T = t;
        }

        public int EventId { get; }
        /// <summary>오류 없이 바로 알렸다.</summary>
        public bool AllCorrect { get; }
        /// <summary>simDt 누적 시각.</summary>
        public double SimT { get; }
        /// <summary>decisionDt 누적 시각.</summary>
        public double T { get; }
    }
}
