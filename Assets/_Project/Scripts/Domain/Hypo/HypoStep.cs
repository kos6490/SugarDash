namespace SugarDash.Domain.Hypo
{
    /// <summary>기록 단위가 되는 대처 단계. Remeasure와 Notify가 함께 4단계(재확인)다.</summary>
    public enum HypoStep
    {
        Recognize,
        Respond,
        Dose,
        Remeasure,
        Notify,
    }
}
