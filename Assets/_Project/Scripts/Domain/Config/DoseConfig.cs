namespace SugarDash.Domain.Config
{
    /// <summary>3단계(처치량 결정) 선택지. 간식 1회분에 곱하는 배수다.</summary>
    public sealed class DoseConfig
    {
        public DoseConfig(double under, double proper, double over)
        {
            Under = under;
            Proper = proper;
            Over = over;
        }

        public double Under { get; }
        public double Proper { get; }
        public double Over { get; }
    }
}
