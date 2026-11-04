// 기획 담당이 확정할 자리 채움 문구
using SugarDash.App;
using SugarDash.Domain.Config;

namespace SugarDash.Presentation
{
    /// <summary>
    /// 튜토리얼 안내 문구(docs/superpowers/specs/2026-11-04-tutorial-design.md 5절 초안).
    /// 화면과 분리해 EditMode 테스트로 확인한다. 의학 설명이나 치료 효과를 말하는 표현은 쓰지 않는다.
    /// </summary>
    public static class TutorialText
    {
        public const string TryAgain = "다시 해봐요";
        public const string Acknowledge = "알겠어요";
        public const string Skip = "건너뛰기";
        public const string SettingsButton = "튜토리얼 다시 하기";

        private const string Notify = "어른에게 알리기를 골라요";

        /// <summary>안내 하나의 설명 문구. 유형과 관계없이 같은 안내(이동, 점프, 슬라이드)는 같다.</summary>
        public static string For(TutorialStep step, TrackType type)
        {
            switch (step)
            {
                case TutorialStep.MoveLeft: return "왼쪽으로 밀어서 피해요";
                case TutorialStep.MoveRight: return "오른쪽으로 밀어서 피해요";
                case TutorialStep.Jump: return "위로 밀면 뛰어넘어요";
                case TutorialStep.Slide: return "아래로 밀면 미끄러져요";
                case TutorialStep.Snack: return "간식이 있는 줄로 가면 먹어요. 먹으면 혈당이 올라요";
                case TutorialStep.FoodRow: return "달리면서 먹을 음식을 골라요. 먹으면 혈당이 올라요";
                case TutorialStep.Rest: return "여기로 가면 쉴 수 있어요. 쉬는 동안은 혈당이 덜 내려가요";
                case TutorialStep.HypoRecognize: return "화면이 흔들리면 몸이 보내는 신호예요. 측정기를 눌러요";
                case TutorialStep.HypoRespond: return "빠른 당을 골라요";
                case TutorialStep.HypoDose: return "알맞은 양을 골라요";
                case TutorialStep.HypoRemeasure: return "먹은 뒤에는 다시 재 봐요";
                case TutorialStep.HypoNotify: return "어른에게 알려요";
                case TutorialStep.HyperNotify: return "혈당이 높을 때도 어른에게 알려요";
                case TutorialStep.HighNotice: return "목이 마르고 화장실에 가고 싶은 신호가 보이면 어른에게 알려요";
                case TutorialStep.LowNotice: return "힘이 없는 신호가 보이면 어른에게 알려요";
                case TutorialStep.HighTell:
                case TutorialStep.LowTell: return Notify;
                case TutorialStep.Finish: return "이제 혼자 달려 봐요!";
                default: return "";
            }
        }
    }
}
