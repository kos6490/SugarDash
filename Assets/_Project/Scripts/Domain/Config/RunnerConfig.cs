namespace SugarDash.Domain.Config
{
    /// <summary>주행(러너) 설정.</summary>
    public sealed class RunnerConfig
    {
        public RunnerConfig(double trackSnackServings, double trackSnackIntervalMinSec, double trackSnackIntervalMaxSec)
        {
            TrackSnackServings = trackSnackServings;
            TrackSnackIntervalMinSec = trackSnackIntervalMinSec;
            TrackSnackIntervalMaxSec = trackSnackIntervalMaxSec;
        }

        /// <summary>트랙에서 주운 간식 하나에 곱하는 배수. 처치량보다 작아 혈당을 조금만 올린다.</summary>
        public double TrackSnackServings { get; }
        /// <summary>트랙 간식이 나오는 간격의 최솟값(worldDt 기준 초). 혈당 균형을 직접 좌우한다.</summary>
        public double TrackSnackIntervalMinSec { get; }
        /// <summary>트랙 간식이 나오는 간격의 최댓값(worldDt 기준 초).</summary>
        public double TrackSnackIntervalMaxSec { get; }
    }
}
