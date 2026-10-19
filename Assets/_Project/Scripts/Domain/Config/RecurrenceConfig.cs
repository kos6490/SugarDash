namespace SugarDash.Domain.Config
{
    /// <summary>저혈당 이벤트 재발생 설정.</summary>
    public sealed class RecurrenceConfig
    {
        public RecurrenceConfig(double cooldownSec) => CooldownSec = cooldownSec;

        /// <summary>이벤트 종료 후 발동하지 않는 기간(simTime 기준 게임 초).</summary>
        public double CooldownSec { get; }
    }
}
