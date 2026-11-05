// 기획 담당이 확정할 자리 채움 문구
using SugarDash.Domain.Hypo;

namespace SugarDash.Presentation
{
    /// <summary>
    /// 탈락 장면에서 어른이 하는 말풍선 한 줄(docs/pause-elimination.md 3절). 탈락 이유 세 갈래(장애물, 알리지 않음, 혈당 범위)마다 다르다.
    /// 아이를 탓하는 말투, 의학 설명, 치료 효과를 말하는 표현은 쓰지 않는다. "죽음", "생존" 표현도 쓰지 않는다.
    /// </summary>
    // [문구 파일] 따옴표 안의 글자만 바꿀 수 있다. 상수 이름, 메서드, 코드 구조는 바꾸지 않고 `// 확정` 줄은 그대로 둔다(docs/team-rules.md 2.3절).
    public static class EliminationText
    {
        public const string ObstacleHit = "괜찮아? 어른이 왔어";
        public const string NotTold = "힘들 땐 어른에게 알려 줘";
        public const string GlucoseRange = "어른이 도와줄게";

        /// <summary>탈락 이유의 말풍선 문구. 이유가 없으면 빈 문자열.</summary>
        public static string For(EliminationCause? cause)
        {
            switch (cause)
            {
                case EliminationCause.ObstacleHit: return ObstacleHit;
                case EliminationCause.Declined:
                case EliminationCause.PromptTimeout: return NotTold;
                case EliminationCause.GlucoseLow:
                case EliminationCause.GlucoseHigh: return GlucoseRange;
                default: return "";
            }
        }
    }
}
