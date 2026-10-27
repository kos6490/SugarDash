namespace SugarDash.Domain.Events
{
    /// <summary>실제 혈당이 저혈당 임계값 아래로 내려가 저혈당 이벤트가 발동했다.</summary>
    public sealed class HypoEventTriggered
    {
        public HypoEventTriggered(int eventId, bool isRecurrence, double simT, double t, double actual)
        {
            EventId = eventId;
            IsRecurrence = isRecurrence;
            SimT = simT;
            T = t;
            Actual = actual;
        }

        /// <summary>주행 안에서 1부터 1씩 늘어나는 이벤트 번호. 이후 이 이벤트의 모든 기록에 싣는다.</summary>
        public int EventId { get; }
        /// <summary>처치 후 해제 수준으로 회복하지 못하고 cooldown 뒤 다시 발동한 경우 true.</summary>
        public bool IsRecurrence { get; }
        /// <summary>simDt 누적 시각.</summary>
        public double SimT { get; }
        /// <summary>decisionDt 누적 시각.</summary>
        public double T { get; }
        public double Actual { get; }
    }
}
