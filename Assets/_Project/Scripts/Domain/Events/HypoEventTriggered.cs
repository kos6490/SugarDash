namespace SugarDash.Domain.Events
{
    /// <summary>실제 혈당이 저혈당 임계값 아래로 내려가 저혈당 이벤트가 발동했다.</summary>
    public sealed class HypoEventTriggered
    {
        public HypoEventTriggered(bool isRecurrence, double simTime, double realElapsed, double actual)
        {
            IsRecurrence = isRecurrence;
            SimTime = simTime;
            RealElapsed = realElapsed;
            Actual = actual;
        }

        /// <summary>처치 후 해제 수준으로 회복하지 못하고 cooldown 뒤 다시 발동한 경우 true.</summary>
        public bool IsRecurrence { get; }
        public double SimTime { get; }
        public double RealElapsed { get; }
        public double Actual { get; }
    }
}
