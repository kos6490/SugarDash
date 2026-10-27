namespace SugarDash.Domain.Events
{
    /// <summary>측정기를 눌렀다. 측정값은 실제 혈당이다.</summary>
    public sealed class MeterPressed
    {
        public MeterPressed(int eventId, double measured, bool unnecessary, bool inGrace, double simT, double t)
        {
            EventId = eventId;
            Measured = measured;
            Unnecessary = unnecessary;
            InGrace = inGrace;
            SimT = simT;
            T = t;
        }

        /// <summary>이벤트 밖이면 0.</summary>
        public int EventId { get; }
        public double Measured { get; }
        /// <summary>실제 혈당이 hypoThreshold 이상일 때 누른 불필요 측정.</summary>
        public bool Unnecessary { get; }
        /// <summary>재개 후 유예 시간 중 측정.</summary>
        public bool InGrace { get; }
        /// <summary>simDt 누적 시각.</summary>
        public double SimT { get; }
        /// <summary>decisionDt 누적 시각.</summary>
        public double T { get; }
    }
}
