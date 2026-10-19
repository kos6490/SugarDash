namespace SugarDash.Domain.Config
{
    /// <summary>시간 압축 설정. 근거가 있는 값은 실제 분 단위로 받고 여기서 게임 초로 바꾼다.</summary>
    public sealed class TimeConfig
    {
        public TimeConfig(double timeScale, double recheckIntervalRealMin, string recheckIntervalSource)
        {
            TimeScale = timeScale;
            RecheckIntervalRealMin = recheckIntervalRealMin;
            RecheckIntervalSource = recheckIntervalSource;
        }

        /// <summary>실제 시간 ÷ 게임 시간.</summary>
        public double TimeScale { get; }
        public double RecheckIntervalRealMin { get; }
        public string RecheckIntervalSource { get; }

        public double RecheckIntervalGameSec => RecheckIntervalRealMin * 60 / TimeScale;
    }
}
