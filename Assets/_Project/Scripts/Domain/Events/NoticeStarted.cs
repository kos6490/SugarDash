using SugarDash.Domain.Director;

namespace SugarDash.Domain.Events
{
    /// <summary>2형에서 실제 혈당이 기준을 일정 시간 넘겨 notice 신호(갈증 말풍선 등)가 시작됐다.</summary>
    public sealed class NoticeStarted
    {
        public NoticeStarted(int eventId, NoticeKind kind, double simT, double t, double actual)
        {
            EventId = eventId;
            Kind = kind;
            SimT = simT;
            T = t;
            Actual = actual;
        }

        /// <summary>이어지는 tell 이벤트(HyperEventTriggered, LowEventTriggered)와 같은 번호.</summary>
        public int EventId { get; }
        public NoticeKind Kind { get; }
        /// <summary>simDt 누적 시각.</summary>
        public double SimT { get; }
        /// <summary>decisionDt 누적 시각.</summary>
        public double T { get; }
        public double Actual { get; }
    }
}
