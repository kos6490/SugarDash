using System.Linq;
using SugarDash.Domain.Metrics;

namespace SugarDash.Presentation
{
    /// <summary>결과 화면 문구(docs/result-screen.md 1절). 화면과 분리해 EditMode 테스트로 확인한다.</summary>
    public static class ResultScreenText
    {
        public static string Title(RunSummary result)
        {
            if (result == null || result.Outcome == RunOutcome.Unfinished) return "결과를 불러오지 못했어요";
            return result.Outcome == RunOutcome.Eliminated ? "어른이 도와주러 왔어요" : "끝까지 달렸어요!";
        }

        public static string Summary(RunSummary result)
        {
            if (result == null || result.Outcome == RunOutcome.Unfinished) return "";
            if (result.Events.Count == 0) return "이번에는 저혈당이 없었어요";

            int good = result.Events.Count(e => e.AllCorrect);
            if (good == 0) return $"저혈당이 {result.Events.Count}번 왔어요.\n다음에는 꼭 해낼 수 있어요!";
            return $"저혈당 {result.Events.Count}번 중 {good}번 모두 잘 해냈어요";
        }
    }
}
