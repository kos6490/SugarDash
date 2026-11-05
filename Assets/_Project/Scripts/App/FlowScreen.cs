namespace SugarDash.App
{
    /// <summary>GameFlowController가 보여 주는 화면.</summary>
    public enum FlowScreen
    {
        TypeSelect,
        Title,
        Settings,
        Running,
        /// <summary>탈락 장면(어른이 달려와 말풍선 한 줄). 끝나면 결과 화면으로 간다.</summary>
        Elimination,
        Result,
    }
}
