namespace SugarDash.Domain.Config
{
    /// <summary>주행(러너) 설정.</summary>
    public sealed class RunnerConfig
    {
        public RunnerConfig(double trackSnackServings, double runDurationSec, double trackSnackIntervalSec)
        {
            TrackSnackServings = trackSnackServings;
            RunDurationSec = runDurationSec;
            TrackSnackIntervalSec = trackSnackIntervalSec;
        }

        /// <summary>트랙에서 주운 간식 하나에 곱하는 배수. 처치량보다 작아 혈당을 조금만 올린다.</summary>
        public double TrackSnackServings { get; }
        /// <summary>주행 길이. worldDt 누적 기준이라 대처 중 세계가 멈춘 시간은 세지 않는다.</summary>
        public double RunDurationSec { get; }
        /// <summary>트랙 간식이 나오는 간격(worldDt 기준 초). 혈당 균형을 직접 좌우한다.</summary>
        public double TrackSnackIntervalSec { get; }
    }
}
