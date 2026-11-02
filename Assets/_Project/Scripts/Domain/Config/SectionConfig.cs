namespace SugarDash.Domain.Config
{
    /// <summary>구간 하나의 설정.</summary>
    public sealed class SectionConfig
    {
        public SectionConfig(string id, double? lengthM)
        {
            Id = id;
            LengthM = lengthM;
        }

        /// <summary>signal, condition, complex 중 하나.</summary>
        public string Id { get; }
        /// <summary>구간 길이(m). 마지막 구간만 null(끝없이 이어진다).</summary>
        public double? LengthM { get; }
    }
}
