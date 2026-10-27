using SugarDash.Domain.Hypo;

namespace SugarDash.Domain.Events
{
    /// <summary>오류가 나서 "어른에게 알리기"를 기다리기 시작했다. Presentation은 위급 상황을 연출한다.</summary>
    public sealed class GuardianPromptStarted
    {
        public GuardianPromptStarted(int eventId, HypoStep step, HypoErrorCode errorCode, double simT, double t)
        {
            EventId = eventId;
            Step = step;
            ErrorCode = errorCode;
            SimT = simT;
            T = t;
        }

        public int EventId { get; }
        public HypoStep Step { get; }
        public HypoErrorCode ErrorCode { get; }
        /// <summary>simDt 누적 시각.</summary>
        public double SimT { get; }
        /// <summary>decisionDt 누적 시각.</summary>
        public double T { get; }
    }
}
