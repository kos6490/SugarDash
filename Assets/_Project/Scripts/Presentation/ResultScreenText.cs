using System;
using System.Collections.Generic;
using System.Linq;
using SugarDash.Domain.Config;
using SugarDash.Domain.Metrics;

namespace SugarDash.Presentation
{
    /// <summary>결과 화면 문구(docs/result-screen.md 1절). 화면과 분리해 EditMode 테스트로 확인한다.</summary>
    // [문구 파일] 따옴표 안의 글자만 바꿀 수 있다. 상수 이름, 메서드, 코드 구조는 바꾸지 않고 `// 확정` 줄은 그대로 둔다(docs/team-rules.md 2.3절).
    public static class ResultScreenText
    {
        /// <summary>튜토리얼 판 결과 제목. 요약 줄은 없다.</summary>
        public const string TutorialTitle = "튜토리얼 끝!"; // 확정

        public static string Title(RunSummary result)
        {
            if (result == null || result.Outcome != RunOutcome.Eliminated) return "결과를 불러오지 못했어요";
            return "어른이 도와주러 왔어요";
        }

        public static string Summary(RunSummary result, double? score = null, double? best = null,
            bool isNewBest = false)
        {
            if (result == null || result.Outcome == RunOutcome.Unfinished) return "";

            var lines = new List<string>();
            if (score != null)
            {
                lines.Add($"점수 {Math.Floor(score.Value):0}점");
                if (best != null) lines.Add($"최고 점수 {Math.Floor(best.Value):0}점");
                if (isNewBest) lines.Add("새 최고 기록!");
            }
            bool type2 = result.TrackType == TrackType.Type2;
            if (type2)
            {
                // 2형: 고혈당·저혈당 줄을 각각 이벤트가 있을 때만 넣는다.
                string high = NotifySummary("높을", result.HyperEvents.Count, result.HyperEvents.Count(e => e.AllCorrect));
                if (high != null) lines.Add(high);
                string low = NotifySummary("낮을", result.LowEvents.Count, result.LowEvents.Count(e => e.AllCorrect));
                if (low != null) lines.Add(low);
                if (high == null && low == null) lines.Add(Type2NoSignal);
            }
            else
            {
                lines.Add(HypoSummary(result));
                string hyper = HyperSummary(result);
                if (hyper != null) lines.Add(hyper);
            }

            // 하나도 해내지 못한 항목이 있으면 격려 문구를 맨 아래에 한 번만 붙인다.
            bool anyMissed = type2
                ? Missed(result.HyperEvents.Count, result.HyperEvents.Count(e => e.AllCorrect)) ||
                  Missed(result.LowEvents.Count, result.LowEvents.Count(e => e.AllCorrect))
                : Missed(result.Events.Count, result.Events.Count(e => e.AllCorrect)) ||
                  Missed(result.HyperEvents.Count, result.HyperEvents.Count(e => e.AllCorrect));
            if (anyMissed) lines.Add(type2 ? Type2Encouragement : Encouragement);
            return string.Join("\n", lines);
        }

        private const string Encouragement = "다음에 더 잘해봐요!";
        /// <summary>2형 격려. 2형에서 놓치는 것은 모두 "어른에게 알리기"라 할 일을 말한다.</summary>
        private const string Type2Encouragement = "다음에는 어른에게 꼭 알려요!";
        /// <summary>2형에서 신호가 한 번도 없던 판.</summary>
        private const string Type2NoSignal = "이번에는 몸이 신호를 보내지 않았어요";

        private static bool Missed(int count, int good) => count > 0 && good == 0;

        private static string HypoSummary(RunSummary result)
        {
            if (result.Events.Count == 0) return "이번에는 혈당이 낮아지지 않았어요";

            int good = result.Events.Count(e => e.AllCorrect);
            if (good == 0) return $"혈당이 낮을 때 {result.Events.Count}번 모두 아쉬웠어요";
            if (good == result.Events.Count) return $"혈당이 낮을 때 {good}번 모두 잘 해냈어요";
            return $"혈당이 낮을 때 {result.Events.Count}번 중 {good}번 잘 해냈어요";
        }

        /// <summary>알리기 한 줄(2형 높을 때·낮을 때, 1형 고혈당). 이벤트가 없으면 null(줄을 더하지 않는다).</summary>
        private static string NotifySummary(string level, int count, int good)
        {
            if (count == 0) return null;
            if (good == 0) return $"혈당이 {level} 때 {count}번 모두 아쉬웠어요";
            if (good == count) return $"혈당이 {level} 때 {count}번 모두 바로 알렸어요";
            return $"혈당이 {level} 때 {count}번 중 {good}번 바로 알렸어요";
        }

        /// <summary>고혈당이 없었으면 null(줄을 더하지 않는다). 2형 높을 때 줄과 같은 글이다.</summary>
        private static string HyperSummary(RunSummary result) =>
            NotifySummary("높을", result.HyperEvents.Count, result.HyperEvents.Count(e => e.AllCorrect));
    }
}
