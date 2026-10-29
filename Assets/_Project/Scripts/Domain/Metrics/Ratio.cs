namespace SugarDash.Domain.Metrics
{
    /// <summary>분자와 분모. 분모가 0이면 비율은 null.</summary>
    public readonly struct Ratio
    {
        public Ratio(int count, int total)
        {
            Count = count;
            Total = total;
        }

        public int Count { get; }
        public int Total { get; }
        public double? Rate => Total == 0 ? (double?)null : (double)Count / Total;

        public override string ToString() => $"{Count}/{Total}";
    }
}
