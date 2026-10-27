using SugarDash.Domain.Hypo;

namespace SugarDash.Domain.Events
{
    /// <summary>아동이 어른에게 알렸다. 그 단계는 정답 상태로 보정되어 진행한다.</summary>
    public sealed class GuardianNotified
    {
        public GuardianNotified(int eventId, HypoStep step, double simT, double t)
        {
            EventId = eventId;
            Step = step;
            SimT = simT;
            T = t;
        }

        public int EventId { get; }
        public HypoStep Step { get; }
        /// <summary>simDt 누적 시각.</summary>
        public double SimT { get; }
        /// <summary>decisionDt 누적 시각.</summary>
        public double T { get; }
    }
}
