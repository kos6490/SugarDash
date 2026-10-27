namespace SugarDash.Domain.Hypo
{
    /// <summary>HypoEventFsm의 현재 상태.</summary>
    public enum HypoPhase
    {
        Idle,
        /// <summary>1단계 위험 인지. 세계가 계속 진행된다.</summary>
        Recognize,
        /// <summary>2단계 대응 선택.</summary>
        Respond,
        /// <summary>3단계 처치량 결정.</summary>
        Dose,
        /// <summary>4단계 재확인 ① 재측정.</summary>
        Remeasure,
        /// <summary>4단계 재확인 ② 어른에게 알리기.</summary>
        Notify,
        /// <summary>오류 뒤 "어른에게 알리기"를 기다린다.</summary>
        GuardianPrompt,
        /// <summary>다시 달리기 전 카운트다운.</summary>
        ResumeCountdown,
        /// <summary>재개 후 유예 시간. 세계는 진행한다.</summary>
        Grace,
        /// <summary>탈락. 더 진행하지 않는다.</summary>
        Eliminated,
    }
}
