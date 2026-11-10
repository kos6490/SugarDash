namespace SugarDash.Presentation
{
    /// <summary>대처 선택 패널의 제목과 버튼 글자. 화면 코드(HypoChoicePanel)와 분리해 둔다.</summary>
    // [문구 파일] 따옴표 안의 글자만 바꿀 수 있다. 상수 이름, 메서드, 코드 구조는 바꾸지 않고 `// 확정` 줄은 그대로 둔다(docs/team-rules.md 2.3절).
    public static class ChoicePanelText
    {
        public const string TellAdult = "어른에게 알리기"; // 확정

        public const string KeepRunning = "계속 달리기";
        public const string NoSugar = "먹을 게 없어요";
        public const string DoseUnder = "조금";
        public const string DoseProper = "알맞게";
        public const string DoseOver = "많이";
        public const string Recheck = "다시 재기";
        public const string RunAgain = "다시 달리기";
        public const string Juice = "주스 마시기";

        public const string RespondTitle = "무엇을 먹을까요?";
        public const string EmptyBagTitle = "가방에 빠른 당이 없어요. 어떻게 할까요?";
        public const string NextActionTitle = "이제 무엇을 할까요?";
        public const string HighTitle = "혈당이 높아요. 어떻게 할까요?";
        public const string Type2TellTitle = "몸이 신호를 보내요. 어떻게 할까요?";
        public const string GuardianPromptTitle = "도움이 필요해요!";

        public static string DoseTitle(string snackId) => $"{FoodText.WithObjectParticle(snackId)} 얼마나 먹을까요?";

        public static string RecheckResult(double glucoseAfter) => $"다시 잰 혈당: {glucoseAfter:0} mg/dL";
    }
}
