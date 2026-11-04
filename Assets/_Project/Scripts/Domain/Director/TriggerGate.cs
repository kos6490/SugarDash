namespace SugarDash.Domain.Director
{
    /// <summary>EventDirector가 어떤 발동을 허용할지 정한다. 튜토리얼이 가르치는 이벤트만 열 때 쓴다.</summary>
    public enum TriggerGate
    {
        All,
        None,
        LowOnly,
        HighOnly,
    }
}
