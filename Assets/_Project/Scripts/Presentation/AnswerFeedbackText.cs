using System;
using SugarDash.Domain.Events;

namespace SugarDash.Presentation
{
    /// <summary>
    /// 선택 뒤 결과 문구와 재개 카운트다운 숫자. 문구는 무엇을 골랐는지만 말하고 정답·오답은 말하지 않는다.
    /// </summary>
    public static class AnswerFeedbackText
    {
        private const string Notified = "어른에게 알렸어요";

        public static string For(StepAnswered e)
        {
            if (e == null) throw new ArgumentNullException(nameof(e));
            if (e.HelpInstead) return Notified;

            switch (e.ChoiceId)
            {
                case null: return "시간이 다 됐어요";
                case "meter": return "측정했어요";
                case "keep_running": return "계속 달리기를 골랐어요";
                case "no_sugar": return "먹을 게 없다고 했어요";
                case "under": return "조금 먹었어요";
                case "proper": return "알맞게 먹었어요";
                case "over": return "많이 먹었어요";
                case "recheck": return "다시 쟀어요";
                case "run_again": return "다시 달리기를 골랐어요";
                case "juice": return "주스를 마셨어요";
                case "help": return Notified;
                default: return $"{e.ChoiceId}을(를) 골랐어요"; // 간식 id
            }
        }

        public static string ForGuardianNotified() => Notified;

        /// <summary>카운트다운에 보여 줄 숫자(남은 2.01초면 3, 2.0초면 2). 남은 시간이 없으면 null.</summary>
        public static int? CountdownNumber(double remaining) =>
            remaining > 0 ? (int)Math.Ceiling(remaining) : (int?)null;
    }
}
