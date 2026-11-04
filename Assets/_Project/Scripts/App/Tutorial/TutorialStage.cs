namespace SugarDash.App
{
    /// <summary>안내 하나 안에서의 진행 상태(설계 4절).</summary>
    public enum TutorialStage
    {
        /// <summary>튜토리얼 판이 아니다.</summary>
        Inactive,
        /// <summary>대상이 다가오거나 혈당 이벤트가 발동하기를 기다린다.</summary>
        Approach,
        /// <summary>설명 창. 세 시계가 멈추고 "알겠어요"를 기다린다.</summary>
        Prompt,
        /// <summary>직접 해 본다(3-2-1 포함).</summary>
        Trying,
        /// <summary>"다시 해봐요" 문구. 세 시계가 멈춘다.</summary>
        Retry,
        /// <summary>안내 구간을 마쳤다. 이제 보통 주행이다.</summary>
        Done,
    }
}
