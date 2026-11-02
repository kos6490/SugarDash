namespace SugarDash.Domain.Config
{
    /// <summary>증상 연출 설정.</summary>
    public sealed class SymptomConfig
    {
        public SymptomConfig(double shakeStartGlucose) => ShakeStartGlucose = shakeStartGlucose;

        /// <summary>실제 혈당이 이 값 아래로 내려가면 카메라 흔들림이 시작된다.</summary>
        public double ShakeStartGlucose { get; }
    }
}
