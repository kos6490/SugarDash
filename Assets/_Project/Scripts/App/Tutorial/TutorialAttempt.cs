namespace SugarDash.App
{
    /// <summary>트랙 안내 대상을 지나간 결과. 화면의 충돌 판정이 모아 넘긴다.</summary>
    public readonly struct TutorialAttempt
    {
        public TutorialAttempt(bool hit, bool collected, bool rested)
        {
            Hit = hit;
            Collected = collected;
            Rested = rested;
        }

        /// <summary>장애물에 부딪혔다.</summary>
        public bool Hit { get; }
        /// <summary>간식이나 음식을 먹었다.</summary>
        public bool Collected { get; }
        /// <summary>쉬기 시작했다.</summary>
        public bool Rested { get; }
    }
}
