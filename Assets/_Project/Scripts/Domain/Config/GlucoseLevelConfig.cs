namespace SugarDash.Domain.Config
{
    /// <summary>혈당 범위, 임계값, 하강률. 단위는 mg/dL, 게임 초.</summary>
    public sealed class GlucoseLevelConfig
    {
        public GlucoseLevelConfig(double initial, double min, double max, double hypoThreshold,
            double hypoReleaseMargin, double hyperThreshold, double baseDecayPerSec, double runDecayPerSec)
        {
            Initial = initial;
            Min = min;
            Max = max;
            HypoThreshold = hypoThreshold;
            HypoReleaseMargin = hypoReleaseMargin;
            HyperThreshold = hyperThreshold;
            BaseDecayPerSec = baseDecayPerSec;
            RunDecayPerSec = runDecayPerSec;
        }

        public double Initial { get; }
        public double Min { get; }
        public double Max { get; }
        public double HypoThreshold { get; }
        public double HypoReleaseMargin { get; }
        public double HyperThreshold { get; }
        public double BaseDecayPerSec { get; }
        public double RunDecayPerSec { get; }

        public double HypoReleaseLevel => HypoThreshold + HypoReleaseMargin;
    }
}
