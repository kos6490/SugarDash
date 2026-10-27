namespace SugarDash.Domain.Hypo
{
    /// <summary>알리기 대기에서 탈락한 방식.</summary>
    public enum EliminationCause
    {
        /// <summary>"어른에게 알리기" 대신 "다시 달리기"를 골랐다.</summary>
        Declined,
        /// <summary>알리기 제한 시간을 넘겼다.</summary>
        PromptTimeout,
    }
}
