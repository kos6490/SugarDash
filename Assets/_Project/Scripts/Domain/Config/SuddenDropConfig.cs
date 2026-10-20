namespace SugarDash.Domain.Config
{
    /// <summary>급하강 이벤트 설정.</summary>
    public sealed class SuddenDropConfig
    {
        public SuddenDropConfig(double chancePerSec, double amount, double durationSec)
        {
            ChancePerSec = chancePerSec;
            Amount = amount;
            DurationSec = durationSec;
        }

        /// <summary>simTime 1초 동안 발생할 확률.</summary>
        public double ChancePerSec { get; }
        /// <summary>급하강 한 번에 내려가는 총량. <see cref="DurationSec"/> 동안 균등하게 내려간다.</summary>
        public double Amount { get; }
        public double DurationSec { get; }
    }
}
