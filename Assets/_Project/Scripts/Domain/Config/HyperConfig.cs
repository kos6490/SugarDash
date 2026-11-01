namespace SugarDash.Domain.Config
{
    /// <summary>고혈당 "어른에게 알리기" 설정(docs/hyper-event.md). 제한 시간은 decisionDt 기준 초.</summary>
    public sealed class HyperConfig
    {
        public HyperConfig(double notifyTimeLimitSec)
        {
            NotifyTimeLimitSec = notifyTimeLimitSec;
        }

        /// <summary>알리기 선택 제한 시간. 넘기면 오류(HY_TIMEOUT)다.</summary>
        public double NotifyTimeLimitSec { get; }
    }
}
