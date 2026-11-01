namespace SugarDash.Domain.Hypo
{
    /// <summary>
    /// 기록 단위가 되는 대처 단계. Remeasure와 Notify가 함께 4단계(재확인)다.
    /// HyperNotify는 고혈당 이벤트의 한 단계("어른에게 알리기")다.
    /// </summary>
    public enum HypoStep
    {
        Recognize,
        Respond,
        Dose,
        Remeasure,
        Notify,
        HyperNotify,
    }
}
