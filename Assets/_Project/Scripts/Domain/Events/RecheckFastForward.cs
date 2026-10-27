namespace SugarDash.Domain.Events
{
    /// <summary>4단계 재측정을 위해 재측정 간격만큼 혈당 시간을 빨리 감았다. 실제로 기다리지는 않는다.</summary>
    public sealed class RecheckFastForward
    {
        public RecheckFastForward(int eventId, double simSecSkipped, double glucoseBefore, double glucoseAfter, double simT, double t)
        {
            EventId = eventId;
            SimSecSkipped = simSecSkipped;
            GlucoseBefore = glucoseBefore;
            GlucoseAfter = glucoseAfter;
            SimT = simT;
            T = t;
        }

        public int EventId { get; }
        public double SimSecSkipped { get; }
        /// <summary>빨리 감기 전 실제 혈당.</summary>
        public double GlucoseBefore { get; }
        /// <summary>빨리 감기 후 실제 혈당(재측정값).</summary>
        public double GlucoseAfter { get; }
        /// <summary>simDt 누적 시각.</summary>
        public double SimT { get; }
        /// <summary>decisionDt 누적 시각.</summary>
        public double T { get; }
    }
}
