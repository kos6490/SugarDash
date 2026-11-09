using System;
using System.Collections.Generic;

namespace SugarDash.Presentation.Runner
{
    /// <summary>
    /// 패턴을 최고 속도에서도 지나갈 수 있는지 확인한다(docs/track-patterns.md 3절).
    /// 줄마다 지나갈 수 있는 레인을 앞 줄에서 이어 가며 찾고, 마지막 줄까지 길이 남아야 통과다.
    /// 슬라이드와 점프는 서로를 바로 끊고 공중에서도 레인을 옮길 수 있으므로(PlayerController),
    /// 시간이 더 드는 경우는 레인 이동과 점프 뒤 다시 점프(착지해야 함)뿐이다.
    /// </summary>
    public static class TrackPatternRules
    {
        private static readonly int[] Lanes = { -1, 0, 1 };

        public static bool Passable(PatternCell cell) => cell != PatternCell.Block;

        /// <summary>문제마다 한 줄. 문제가 없으면 빈 목록.</summary>
        public static List<string> Validate(IReadOnlyList<TrackPattern> patterns, PatternTiming timing)
        {
            if (timing == null) throw new ArgumentNullException(nameof(timing));
            var problems = new List<string>();
            if (patterns == null || patterns.Count == 0)
            {
                problems.Add("패턴이 하나도 없다");
                return problems;
            }

            var ids = new HashSet<string>();
            bool hasStartEasy = false;
            for (int i = 0; i < patterns.Count; i++)
            {
                var p = patterns[i];
                if (p == null)
                {
                    problems.Add($"{i}번 패턴이 비어 있다");
                    continue;
                }
                string name = string.IsNullOrWhiteSpace(p.Id) ? $"{i}번 패턴" : p.Id;
                if (string.IsNullOrWhiteSpace(p.Id)) problems.Add($"{name}: id가 비어 있다");
                else if (!ids.Add(p.Id)) problems.Add($"{name}: id가 다른 패턴과 겹친다");
                if (p.Rows.Count == 0)
                {
                    problems.Add($"{name}: 줄이 없다");
                    continue;
                }
                if (p.Weight < 0) problems.Add($"{name}: weight는 0 이상이어야 한다");
                if (p.StartDistanceM < 0) problems.Add($"{name}: startDistanceM은 0 이상이어야 한다");
                if (p.Easy && p.Rows.Count != 1) problems.Add($"{name}: 쉬운 패턴은 줄이 하나여야 한다");
                string path = CheckPath(p, timing);
                if (path != null) problems.Add($"{name}: {path}");
                if (p.Easy && p.StartDistanceM == 0 && p.Weight > 0) hasStartEasy = true;
            }
            if (!hasStartEasy) problems.Add("startDistanceM 0, weight 0보다 큰 쉬운 패턴이 하나 이상 있어야 한다");
            return problems;
        }

        /// <summary>
        /// 패턴과 패턴 사이 거리가 어떤 두 줄이 이어져도 지나갈 수 있을 만큼인지 본다. 문제가 없으면 null.
        /// </summary>
        public static string ValidateSpacing(float spacingM, PatternTiming timing)
        {
            if (timing == null) throw new ArgumentNullException(nameof(timing));
            double worst = timing.ReactionSec + Math.Max(2 * timing.LaneChangeSec, timing.JumpSec);
            double need = timing.MaxRunSpeed * worst;
            return spacingM + 1e-6 >= need ? null : $"패턴 사이 거리 {spacingM}m가 최소 {need:0.##}m보다 짧다";
        }

        /// <summary>a 레인(앞 줄)에서 b 레인(다음 줄)으로 이어 갈 때 드는 시간.</summary>
        private static double NeedSec(PatternCell a, int laneA, PatternCell b, int laneB, PatternTiming t)
        {
            double move = Math.Abs(laneA - laneB) * t.LaneChangeSec;
            double rejump = a == PatternCell.Low && b == PatternCell.Low ? t.JumpSec : 0;
            return t.ReactionSec + Math.Max(move, rejump);
        }

        private static string CheckPath(TrackPattern p, PatternTiming t)
        {
            var reachable = new bool[3];
            for (int r = 0; r < p.Rows.Count; r++)
            {
                var row = p.Rows[r];
                if (row == null) return $"{r + 1}번째 줄이 비어 있다";
                var next = new bool[3];
                bool any = false;
                foreach (int b in Lanes)
                {
                    bool ok = Passable(row.CellAt(b));
                    if (ok && r > 0)
                    {
                        ok = false;
                        if (row.GapBeforeM <= 0) return $"{r + 1}번째 줄의 gapBeforeM은 0보다 커야 한다";
                        var prev = p.Rows[r - 1];
                        foreach (int a in Lanes)
                        {
                            if (!reachable[a + 1]) continue;
                            double need = t.MaxRunSpeed * NeedSec(prev.CellAt(a), a, row.CellAt(b), b, t);
                            if (row.GapBeforeM + 1e-6 >= need)
                            {
                                ok = true;
                                break;
                            }
                        }
                    }
                    any |= ok;
                    next[b + 1] = ok;
                }
                reachable = next;
                if (!any)
                {
                    bool blocked = true;
                    foreach (int b in Lanes) blocked &= !Passable(row.CellAt(b));
                    return blocked
                        ? $"{r + 1}번째 줄은 세 레인이 모두 레인 막이라 지나갈 수 없다"
                        : $"{r + 1}번째 줄까지 최고 속도에서 지나갈 길이 없다(gapBeforeM을 늘린다)";
                }
            }
            return null;
        }
    }
}
