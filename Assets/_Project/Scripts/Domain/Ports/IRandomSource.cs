namespace SugarDash.Domain.Ports
{
    /// <summary>시드를 지정할 수 있는 난수 원천. 도메인은 이 포트로만 난수를 얻는다.</summary>
    public interface IRandomSource
    {
        /// <summary>[0, 1) 범위의 난수.</summary>
        double NextDouble();
    }
}
