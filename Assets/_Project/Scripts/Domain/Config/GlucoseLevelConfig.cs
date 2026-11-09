namespace SugarDash.Domain.Config
{
    /// <summary>혈당 범위, 임계값, 하강률. 단위는 mg/dL, 게임 초.</summary>
    public sealed class GlucoseLevelConfig
    {
        public GlucoseLevelConfig(double initial, double min, double max, double hypoThreshold,
            double hypoReleaseMargin, double hyperThreshold, double hyperReleaseMargin, double baseDecayPerSec, double runDecayPerSec,
            double normalRangeMin, double normalRangeMax)
        {
            Initial = initial;
            Min = min;
            Max = max;
            HypoThreshold = hypoThreshold;
            HypoReleaseMargin = hypoReleaseMargin;
            HyperThreshold = hyperThreshold;
            HyperReleaseMargin = hyperReleaseMargin;
            BaseDecayPerSec = baseDecayPerSec;
            RunDecayPerSec = runDecayPerSec;
            NormalRangeMin = normalRangeMin;
            NormalRangeMax = normalRangeMax;
        }

        public double Initial { get; }
        public double Min { get; }
        public double Max { get; }
        public double HypoThreshold { get; }
        public double HypoReleaseMargin { get; }
        public double HyperThreshold { get; }
        /// <summary>고혈당 이벤트 뒤 HyperThreshold − 이 값 아래로 내려와야 다시 발동할 수 있다.</summary>
        public double HyperReleaseMargin { get; }
        public double BaseDecayPerSec { get; }
        /// <summary>달리기 감소율. activity와 worldDt에 비례한다.</summary>
        public double RunDecayPerSec { get; }
        /// <summary>적정 처치 후 재측정에서 도달해야 하는 하한(불변식 18).</summary>
        public double NormalRangeMin { get; }
        /// <summary>
        /// 적정 범위(혈당 유지 점수) 위쪽 경계. 이 값 미만이어야 범위 안이다. hyperThreshold와 따로 둔다:
        /// 목표 범위 위쪽 끝과 고혈당 알리기 기준이 다르다(docs/glucose-evidence.md).
        /// </summary>
        public double NormalRangeMax { get; }

        public double HypoReleaseLevel => HypoThreshold + HypoReleaseMargin;
        public double HyperReleaseLevel => HyperThreshold - HyperReleaseMargin;
    }
}
