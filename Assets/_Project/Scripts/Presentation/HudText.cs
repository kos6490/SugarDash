namespace SugarDash.Presentation
{
    /// <summary>주행 중 상단 표시(측정 결과, 점수, 멈춤)의 글자. 화면 코드(HudPresenter)와 분리해 둔다.</summary>
    // [문구 파일] 따옴표 안의 글자만 바꿀 수 있다. 상수 이름, 메서드, 코드 구조는 바꾸지 않고 `// 확정` 줄은 그대로 둔다(docs/team-rules.md 2.3절).
    public static class HudText
    {
        public const string Paused = "잠시 멈춤";
        /// <summary>빈 간식 가방 사건 뒤 다시 달리기 전 3-2-1 동안 보이는 습관 말.</summary>
        public const string PackFastSugar = "다음엔 빠른 당을 챙겨요";

        public static string Measured(double glucose) => $"측정: {glucose:0} mg/dL";

        public static string Score(int score) => $"점수 {score}";
    }
}
