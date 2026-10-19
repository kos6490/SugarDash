namespace SugarDash.Domain.Config
{
    public enum SnackKind
    {
        Fast,
        Slow,
    }

    /// <summary>간식 한 종류. 1회분 기준으로 onset 뒤 absorbSec 동안 totalRise만큼 균등하게 오른다.</summary>
    public sealed class SnackConfig
    {
        public SnackConfig(string id, SnackKind kind, double totalRise, double onsetSec, double absorbSec,
            string source)
        {
            Id = id;
            Kind = kind;
            TotalRise = totalRise;
            OnsetSec = onsetSec;
            AbsorbSec = absorbSec;
            Source = source;
        }

        public string Id { get; }
        public SnackKind Kind { get; }
        public double TotalRise { get; }
        public double OnsetSec { get; }
        public double AbsorbSec { get; }
        public string Source { get; }
    }
}
