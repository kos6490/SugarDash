namespace SugarDash.Domain.Hypo
{
    /// <summary>탈락한 방식.</summary>
    public enum EliminationCause
    {
        /// <summary>알리기 대기에서 "어른에게 알리기" 대신 "다시 달리기"를 골랐다.</summary>
        Declined,
        /// <summary>알리기 제한 시간을 넘겼다.</summary>
        PromptTimeout,
        /// <summary>장애물에 부딪혔다.</summary>
        ObstacleHit,
        /// <summary>실제 혈당이 안전 범위 하한(elimination.glucoseLow) 아래로 내려갔다.</summary>
        GlucoseLow,
        /// <summary>실제 혈당이 안전 범위 상한(elimination.glucoseHigh) 위로 올라갔다.</summary>
        GlucoseHigh,
    }
}
