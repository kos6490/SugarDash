namespace SugarDash.Domain.Glucose
{
    /// <summary>simTime 구간 [Start, End) 동안 일정한 속도로 혈당을 바꾸는 항. 간식 흡수와 급하강에 쓴다.</summary>
    internal readonly struct RateWindow
    {
        public RateWindow(double start, double end, double ratePerSec)
        {
            Start = start;
            End = end;
            RatePerSec = ratePerSec;
        }

        public double Start { get; }
        public double End { get; }
        public double RatePerSec { get; }

        /// <summary>[from, to)와 겹치는 동안의 변화량.</summary>
        public double DeltaOver(double from, double to)
        {
            double overlap = System.Math.Min(to, End) - System.Math.Max(from, Start);
            return overlap > 0 ? overlap * RatePerSec : 0;
        }
    }
}
