namespace SugarDash.Domain.Events
{
    /// <summary>2형에서 저혈당 notice가 끝나 tell("어른에게 알리기" 선택)이 발동했다.</summary>
    public sealed class LowEventTriggered
    {
        public LowEventTriggered(int eventId, double simT, double t, double actual)
        {
            EventId = eventId;
            SimT = simT;
            T = t;
            Actual = actual;
        }

        /// <summary>NoticeStarted와 같은 이벤트 번호.</summary>
        public int EventId { get; }
        /// <summary>simDt 누적 시각.</summary>
        public double SimT { get; }
        /// <summary>decisionDt 누적 시각.</summary>
        public double T { get; }
        public double Actual { get; }
    }
}
