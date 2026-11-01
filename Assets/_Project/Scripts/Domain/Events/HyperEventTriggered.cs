namespace SugarDash.Domain.Events
{
    /// <summary>실제 혈당이 고혈당 임계값 이상으로 올라가 고혈당 이벤트("어른에게 알리기" 한 단계)가 발동했다.</summary>
    public sealed class HyperEventTriggered
    {
        public HyperEventTriggered(int eventId, double simT, double t, double actual)
        {
            EventId = eventId;
            SimT = simT;
            T = t;
            Actual = actual;
        }

        /// <summary>저혈당 이벤트와 같은 순서로 주행 안에서 1씩 늘어나는 이벤트 번호.</summary>
        public int EventId { get; }
        /// <summary>simDt 누적 시각.</summary>
        public double SimT { get; }
        /// <summary>decisionDt 누적 시각.</summary>
        public double T { get; }
        public double Actual { get; }
    }
}
