namespace SugarDash.App
{
    /// <summary>튜토리얼 안내 하나. 유형마다 쓰는 안내와 순서가 다르다(설계 5절).</summary>
    public enum TutorialStep
    {
        MoveLeft,
        MoveRight,
        Jump,
        Slide,
        /// <summary>1형 트랙 간식.</summary>
        Snack,
        /// <summary>2형 choose 음식 줄.</summary>
        FoodRow,
        /// <summary>2형 move 쉬는 곳.</summary>
        Rest,
        /// <summary>1형 저혈당 1단계 위험 인지.</summary>
        HypoRecognize,
        HypoRespond,
        HypoDose,
        HypoRemeasure,
        HypoNotify,
        /// <summary>1형 고혈당 알리기.</summary>
        HyperNotify,
        /// <summary>2형 높음 notice 신호.</summary>
        HighNotice,
        /// <summary>2형 높음 tell.</summary>
        HighTell,
        /// <summary>2형 낮음 notice 신호.</summary>
        LowNotice,
        /// <summary>2형 낮음 tell.</summary>
        LowTell,
        /// <summary>안내 구간 끝 설명.</summary>
        Finish,
    }
}
