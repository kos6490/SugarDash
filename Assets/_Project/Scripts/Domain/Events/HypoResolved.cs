namespace SugarDash.Domain.Events
{
    /// <summary>4단계 대처를 마쳤다.</summary>
    public sealed class HypoResolved
    {
        public HypoResolved(int eventId, bool allCorrect, double simT, double t)
        {
            EventId = eventId;
            AllCorrect = allCorrect;
            SimT = simT;
            T = t;
        }

        public int EventId { get; }
        /// <summary>4단계 모두 보정 없이 정답이었다.</summary>
        public bool AllCorrect { get; }
        /// <summary>simDt 누적 시각.</summary>
        public double SimT { get; }
        /// <summary>decisionDt 누적 시각.</summary>
        public double T { get; }
    }
}
