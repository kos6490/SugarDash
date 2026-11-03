using System;
using System.Collections.Generic;
using System.Linq;
using SugarDash.Domain.Config;
using SugarDash.Domain.Metrics;

namespace SugarDash.Presentation
{
    /// <summary>결과 화면 문구(docs/result-screen.md 1절). 화면과 분리해 EditMode 테스트로 확인한다.</summary>
    public static class ResultScreenText
    {
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
                string high = Type2Summary("높을", result.HyperEvents.Count, result.HyperEvents.Count(e => e.AllCorrect));
                if (high != null) lines.Add(high);
                string low = Type2Summary("낮을", result.LowEvents.Count, result.LowEvents.Count(e => e.AllCorrect));
                if (low != null) lines.Add(low);
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
            if (anyMissed) lines.Add(Encouragement);
            return string.Join("\n", lines);
        }

        private const string Encouragement = "다음에 더 잘해봐요!";

        private static bool Missed(int count, int good) => count > 0 && good == 0;

        private static string HypoSummary(RunSummary result)
        {
            if (result.Events.Count == 0) return "이번에는 저혈당이 없었어요";

            int good = result.Events.Count(e => e.AllCorrect);
            if (good == 0) return $"저혈당 {result.Events.Count}번 모두 아쉬웠어요";
            if (good == result.Events.Count) return $"저혈당 {good}번 모두 잘 해냈어요";
            return $"저혈당 {result.Events.Count}번 중 {good}번 모두 잘 해냈어요";
        }

        /// <summary>2형 한 줄. 이벤트가 없으면 null(줄을 더하지 않는다).</summary>
        private static string Type2Summary(string level, int count, int good)
        {
            if (count == 0) return null;
            if (good == 0) return $"혈당이 {level} 때 {count}번 모두 아쉬웠어요";
            if (good == count) return $"혈당이 {level} 때 {count}번 모두 바로 알렸어요";
            return $"혈당이 {level} 때 {count}번 중 {good}번 바로 알렸어요";
        }

        /// <summary>고혈당이 없었으면 null(줄을 더하지 않는다).</summary>
        private static string HyperSummary(RunSummary result)
        {
            int count = result.HyperEvents.Count;
            if (count == 0) return null;

            int good = result.HyperEvents.Count(e => e.AllCorrect);
            if (good == 0) return $"고혈당 {count}번 모두 아쉬웠어요";
            if (good == count) return $"고혈당 {count}번 모두 바로 알렸어요";
            return $"고혈당 {count}번 중 {good}번 바로 알렸어요";
        }
    }
}
