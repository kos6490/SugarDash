namespace SugarDash.Domain.Config
{
    /// <summary>시간 압축 설정. 근거가 있는 값은 실제 분 단위로 받고 여기서 게임 초로 바꾼다.</summary>
    public sealed class TimeConfig
    {
        public TimeConfig(double timeScale, double recheckIntervalRealMin, string recheckIntervalSource,
            double fastForwardTickSimSec)
        {
            TimeScale = timeScale;
            RecheckIntervalRealMin = recheckIntervalRealMin;
            RecheckIntervalSource = recheckIntervalSource;
            FastForwardTickSimSec = fastForwardTickSimSec;
        }

        /// <summary>실제 시간 ÷ 게임 시간.</summary>
        public double TimeScale { get; }
        public double RecheckIntervalRealMin { get; }
        public string RecheckIntervalSource { get; }
        /// <summary>재측정 빨리 감기를 나눠 Tick하는 간격(게임 초). 표시 혈당 지연 버퍼를 채우기 위해 나눈다.</summary>
        public double FastForwardTickSimSec { get; }

        public double RecheckIntervalGameSec => RecheckIntervalRealMin * 60 / TimeScale;
    }
}
