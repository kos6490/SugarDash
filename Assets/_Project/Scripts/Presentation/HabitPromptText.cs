// 기획 담당이 확정할 자리 채움 문구
using SugarDash.Domain.Config;

namespace SugarDash.Presentation
{
    /// <summary>
    /// 1형 혈당 재기 습관 순간의 말풍선 문구(docs/section2.md 7.1절). 화면(HabitPromptView)과 분리해 둔다.
    /// 의학 설명이나 치료 효과를 말하는 표현은 쓰지 않는다.
    /// </summary>
    // [문구 파일] 따옴표 안의 글자만 바꿀 수 있다. 상수 이름, 메서드, 코드 구조는 바꾸지 않고 `// 확정` 줄은 그대로 둔다(docs/team-rules.md 2.3절).
    public static class HabitPromptText
    {
        /// <summary>습관 순간의 말풍선 문구. 순간이 없으면 빈 문자열.</summary>
        public static string For(HabitMoment? moment)
        {
            switch (moment)
            {
                case HabitMoment.PreExercise: return "운동 전에 혈당 재 볼까?";
                case HabitMoment.Bedtime: return "자기 전에 혈당 재 볼까?";
                default: return "";
            }
        }
    }
}
