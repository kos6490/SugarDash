namespace SugarDash.Domain.Config
{
    /// <summary>혈당 범위, 임계값, 하강률. 단위는 mg/dL, 게임 초.</summary>
    public sealed class GlucoseLevelConfig
    {
        public GlucoseLevelConfig(double initial, double min, double max, double hypoThreshold,
            double hypoReleaseMargin, double hyperThreshold, double baseDecayPerSec, double runDecayPerSec,
            double normalRangeMin)
        {
            Initial = initial;
            Min = min;
            Max = max;
            HypoThreshold = hypoThreshold;
            HypoReleaseMargin = hypoReleaseMargin;
            HyperThreshold = hyperThreshold;
            BaseDecayPerSec = baseDecayPerSec;
            RunDecayPerSec = runDecayPerSec;
            NormalRangeMin = normalRangeMin;
        }

        public double Initial { get; }
        public double Min { get; }
        public double Max { get; }
        public double HypoThreshold { get; }
        public double HypoReleaseMargin { get; }
        public double HyperThreshold { get; }
        public double BaseDecayPerSec { get; }
        /// <summary>달리기 감소율. activity와 worldDt에 비례한다.</summary>
        public double RunDecayPerSec { get; }
        /// <summary>적정 처치 후 재측정에서 도달해야 하는 하한(불변식 18).</summary>
        public double NormalRangeMin { get; }

        public double HypoReleaseLevel => HypoThreshold + HypoReleaseMargin;
    }
}
