using System;
using System.Collections.Generic;
using System.Linq;
using SugarDash.Domain.Events;
using SugarDash.Domain.Hypo;
using SugarDash.Domain.Records;

namespace SugarDash.Domain.Metrics
{
    /// <summary>
    /// 원시 기록으로 지표와 결과 요약을 계산한다(CLAUDE.md 6절). 기록에는 지표를 저장하지 않으므로,
    /// 정의가 바뀌어도 과거 기록으로 다시 계산할 수 있다. 정의는 docs/glucose-model.md 6절에 있다.
    /// </summary>
    public static class MetricsCalculator
    {
        private static readonly MetricStep[] AllMetricSteps =
            { MetricStep.Recognize, MetricStep.Respond, MetricStep.Dose, MetricStep.Recheck };

        private static readonly HypoStep[] AllHypoSteps =
            { HypoStep.Recognize, HypoStep.Respond, HypoStep.Dose, HypoStep.Remeasure, HypoStep.Notify };

        /// <summary>결과 화면용 주행 하나의 요약.</summary>
        public static RunSummary Summarize(RunRecord run)
        {
            if (run == null) throw new ArgumentNullException(nameof(run));

            var outcome = run.Events.OfType<RunEliminated>().Any() ? RunOutcome.Eliminated
                : run.Events.OfType<RunCompleted>().Any() ? RunOutcome.Completed
                : RunOutcome.Unfinished;
            return new RunSummary(run.RunId, outcome, Events(run), Calculate(new[] { run }));
        }

        /// <summary>여러 주행을 모은 지표.</summary>
        public static RunMetrics Calculate(IEnumerable<RunRecord> runs)
        {
            if (runs == null) throw new ArgumentNullException(nameof(runs));

            var runList = runs.ToList();
            var events = runList.SelectMany(Events).ToList();
            var answers = runList.SelectMany(r => r.Events.OfType<StepAnswered>()).ToList();

            var finished = events.Where(e => e.IsFinished).ToList();
            var accurate = new Ratio(finished.Count(e => e.AllCorrect), finished.Count);

            var stepAccuracy = new Dictionary<MetricStep, Ratio>();
            foreach (var step in AllMetricSteps)
            {
                var reached = events.Select(e => e.Steps[step]).Where(v => v.HasValue).ToList();
                stepAccuracy[step] = new Ratio(reached.Count(v => v.Value), reached.Count);
            }

            var errorCounts = answers.Where(a => a.ErrorCode != HypoErrorCode.None)
                .GroupBy(a => a.ErrorCode)
                .ToDictionary(g => g.Key, g => g.Count());

            // 시간 초과(선택지 없음)는 판단이 아니므로 판단 시간에서 뺀다.
            var medians = new Dictionary<HypoStep, double?>();
            foreach (var step in AllHypoSteps)
                medians[step] = Median(answers.Where(a => a.Step == step && a.ChoiceId != null)
                    .Select(a => a.DecisionSec));

            var recognize = answers.Where(a => a.Step == HypoStep.Recognize).ToList();
            var measuredFirst = new Ratio(recognize.Count(a => a.MeasuredBeforeWarning), recognize.Count);

            int unnecessary = runList.SelectMany(r => r.Events.OfType<MeterPressed>()).Count(m => m.Unnecessary);

            return new RunMetrics(accurate, stepAccuracy, errorCounts, medians, measuredFirst, unnecessary);
        }

        /// <summary>주행 안의 저혈당 이벤트를 발동 순서대로 묶는다.</summary>
        private static IReadOnlyList<EventResult> Events(RunRecord run)
        {
            var results = new List<EventResult>();
            foreach (var triggered in run.Events.OfType<HypoEventTriggered>())
            {
                int id = triggered.EventId;
                var answers = run.Events.OfType<StepAnswered>().Where(a => a.EventId == id).ToList();
                var resolved = run.Events.OfType<HypoResolved>().FirstOrDefault(r => r.EventId == id);
                bool eliminated = run.Events.OfType<RunEliminated>().Any(r => r.EventId == id);

                bool? Single(HypoStep step) => answers.FirstOrDefault(a => a.Step == step)?.IsCorrect;

                var steps = new Dictionary<MetricStep, bool?>
                {
                    [MetricStep.Recognize] = Single(HypoStep.Recognize),
                    [MetricStep.Respond] = Single(HypoStep.Respond),
                    [MetricStep.Dose] = Single(HypoStep.Dose),
                    [MetricStep.Recheck] = Recheck(Single(HypoStep.Remeasure), Single(HypoStep.Notify)),
                };

                results.Add(new EventResult(id, triggered.IsRecurrence, steps, resolved?.AllCorrect ?? false,
                    resolved != null || eliminated));
            }
            return results;
        }

        /// <summary>4단계는 재측정까지 갔으면 센다. 재측정과 알리기가 모두 기록되고 둘 다 맞아야 정답.</summary>
        private static bool? Recheck(bool? remeasure, bool? notify)
        {
            if (!remeasure.HasValue) return null;
            return remeasure.Value && notify == true;
        }

        private static double? Median(IEnumerable<double> values)
        {
            var sorted = values.OrderBy(v => v).ToList();
            if (sorted.Count == 0) return null;
            int mid = sorted.Count / 2;
            return sorted.Count % 2 == 1 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2;
        }
    }
}
