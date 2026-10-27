namespace SugarDash.Domain.Events
{
    /// <summary>실제 혈당이 저혈당 임계값 아래로 내려가 저혈당 이벤트가 발동했다.</summary>
    public sealed class HypoEventTriggered
    {
        public HypoEventTriggered(bool isRecurrence, double simT, double t, double actual)
        {
            IsRecurrence = isRecurrence;
            SimT = simT;
            T = t;
            Actual = actual;
        }

        /// <summary>처치 후 해제 수준으로 회복하지 못하고 cooldown 뒤 다시 발동한 경우 true.</summary>
        public bool IsRecurrence { get; }
        /// <summary>simDt 누적 시각.</summary>
        public double SimT { get; }
        /// <summary>decisionDt 누적 시각.</summary>
        public double T { get; }
        public double Actual { get; }
    }
}
