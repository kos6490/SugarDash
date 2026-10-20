namespace SugarDash.Domain.Config
{
    /// <summary>4단계 대처(HypoEventFsm) 설정.</summary>
    public sealed class HypoConfig
    {
        public HypoConfig(double step1TimeLimitSec) => Step1TimeLimitSec = step1TimeLimitSec;

        /// <summary>1단계(위험 인지) 제한 시간. realElapsed 기준 초.</summary>
        public double Step1TimeLimitSec { get; }
    }
}
