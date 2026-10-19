using SugarDash.Domain.Ports;

namespace SugarDash.Infrastructure
{
    /// <summary><see cref="IRandomSource"/> 구현. 같은 시드면 같은 난수열을 낸다.</summary>
    public sealed class SystemRandomSource : IRandomSource
    {
        private readonly System.Random _random;

        public SystemRandomSource(int seed) => _random = new System.Random(seed);

        public double NextDouble() => _random.NextDouble();
    }
}
