using SugarDash.Domain.Hypo;

namespace SugarDash.Domain.Events
{
    /// <summary>오류 뒤 어른에게 알리지 않아 탈락했다. 어른이 도와주러 오는 장면 뒤 결과 화면으로 간다.</summary>
    public sealed class RunEliminated
    {
        public RunEliminated(int eventId, HypoStep step, HypoErrorCode reason, EliminationCause cause, double simT, double t)
        {
            EventId = eventId;
            Step = step;
            Reason = reason;
            Cause = cause;
            SimT = simT;
            T = t;
        }

        public int EventId { get; }
        public HypoStep Step { get; }
        /// <summary>탈락으로 이어진 그 단계의 오류.</summary>
        public HypoErrorCode Reason { get; }
        public EliminationCause Cause { get; }
        /// <summary>simDt 누적 시각.</summary>
        public double SimT { get; }
        /// <summary>decisionDt 누적 시각.</summary>
        public double T { get; }
    }
}
