using System.Linq;
using SugarDash.Domain.Events;
using SugarDash.Domain.Hypo;
using SugarDash.Domain.Metrics;
using SugarDash.Domain.Records;
using NUnit.Framework;

namespace SugarDash.Domain.Tests
{
    public class MetricsCalculatorTests
    {
        private static RunRecord Run(params object[] events) => new RunRecord("run", "test", null, null, events, 0);

        private static HypoEventTriggered Triggered(int id, bool recurrence = false) =>
            new HypoEventTriggered(id, recurrence, 0, 0, 68);

        private static StepAnswered Ok(int id, HypoStep step, double decisionSec = 1, bool measuredFirst = false) =>
            new StepAnswered(id, step, "choice", true, HypoErrorCode.None, false, decisionSec, measuredFirst, 0, 0);

        private static StepAnswered Wrong(int id, HypoStep step, HypoErrorCode code, double decisionSec = 1) =>
            new StepAnswered(id, step, "choice", false, code, code == HypoErrorCode.HelpInstead, decisionSec, false,
                0, 0);

        private static StepAnswered Timeout(int id, HypoStep step, HypoErrorCode code, double decisionSec = 5) =>
            new StepAnswered(id, step, null, false, code, false, decisionSec, false, 0, 0);

        private static object[] AllCorrect(int id, bool recurrence = false, bool measuredFirst = true) => new object[]
        {
            Triggered(id, recurrence),
            Ok(id, HypoStep.Recognize, measuredFirst: measuredFirst),
            Ok(id, HypoStep.Respond),
            Ok(id, HypoStep.Dose),
            Ok(id, HypoStep.Remeasure),
            Ok(id, HypoStep.Notify),
            new HypoResolved(id, true, 0, 0),
        };

        [Test]
        public void AllCorrectEvent_CountsAsAccurate()
        {
            var run = Run(AllCorrect(1).Append(new RunCompleted(40, 40, 50)).ToArray());

            var summary = MetricsCalculator.Summarize(run);

            Assert.That(summary.Outcome, Is.EqualTo(RunOutcome.Completed));
            var e = summary.Events.Single();
            Assert.That(e.AllCorrect, Is.True);
            Assert.That(e.IsFinished, Is.True);
            Assert.That(e.Steps.Values, Is.All.True);
            Assert.That(summary.Metrics.AccurateResponse.ToString(), Is.EqualTo("1/1"));
            Assert.That(summary.Metrics.StepAccuracy.Values.Select(r => r.ToString()), Is.All.EqualTo("1/1"));
            Assert.That(summary.Metrics.MeasuredFirst.Rate, Is.EqualTo(1));
            Assert.That(summary.Metrics.ErrorCounts, Is.Empty);
        }

        [Test]
        public void EliminatedEvent_IsInaccurate_AndUnreachedStepsAreExcluded()
        {
            var run = Run(
                Triggered(1),
                Timeout(1, HypoStep.Recognize, HypoErrorCode.SMissed),
                new RunEliminated(1, HypoStep.Recognize, HypoErrorCode.SMissed, EliminationCause.PromptTimeout, 0, 0));

            var summary = MetricsCalculator.Summarize(run);

            Assert.That(summary.Outcome, Is.EqualTo(RunOutcome.Eliminated));
            var e = summary.Events.Single();
            Assert.That(e.IsFinished, Is.True);
            Assert.That(e.AllCorrect, Is.False);
            Assert.That(e.Steps[MetricStep.Recognize], Is.False);
            Assert.That(e.Steps[MetricStep.Respond], Is.Null);
            Assert.That(e.Steps[MetricStep.Recheck], Is.Null);

            var m = summary.Metrics;
            Assert.That(m.AccurateResponse.ToString(), Is.EqualTo("0/1"));
            Assert.That(m.StepAccuracy[MetricStep.Recognize].ToString(), Is.EqualTo("0/1"));
            Assert.That(m.StepAccuracy[MetricStep.Respond].Rate, Is.Null, "가지 못한 단계는 분모에서 뺀다");
            Assert.That(m.ErrorCounts[HypoErrorCode.SMissed], Is.EqualTo(1));
        }

        [Test]
        public void CorrectedEvent_IsNotAccurate_EvenIfResolved()
        {
            var run = Run(
                Triggered(1),
                Ok(1, HypoStep.Recognize),
                Wrong(1, HypoStep.Respond, HypoErrorCode.RsSlow),
                Ok(1, HypoStep.Dose),
                Ok(1, HypoStep.Remeasure),
                Ok(1, HypoStep.Notify),
                new HypoResolved(1, false, 0, 0));

            var m = MetricsCalculator.Summarize(run).Metrics;

            Assert.That(m.AccurateResponse.ToString(), Is.EqualTo("0/1"));
            Assert.That(m.StepAccuracy[MetricStep.Respond].ToString(), Is.EqualTo("0/1"));
            Assert.That(m.StepAccuracy[MetricStep.Dose].ToString(), Is.EqualTo("1/1"));
            Assert.That(m.ErrorCounts[HypoErrorCode.RsSlow], Is.EqualTo(1));
        }

        [Test]
        public void Recheck_NeedsBothRemeasureAndNotify()
        {
            var notifyMissed = Run(Triggered(1), Ok(1, HypoStep.Remeasure),
                Wrong(1, HypoStep.Notify, HypoErrorCode.RcNoNotify), new HypoResolved(1, false, 0, 0));
            var remeasureMissed = Run(Triggered(1), Wrong(1, HypoStep.Remeasure, HypoErrorCode.RcNoRecheck),
                Ok(1, HypoStep.Notify), new HypoResolved(1, false, 0, 0));
            var cutAfterRemeasure = Run(Triggered(1), Ok(1, HypoStep.Remeasure));

            Assert.That(MetricsCalculator.Summarize(notifyMissed).Events[0].Steps[MetricStep.Recheck], Is.False);
            Assert.That(MetricsCalculator.Summarize(remeasureMissed).Events[0].Steps[MetricStep.Recheck], Is.False);
            Assert.That(MetricsCalculator.Summarize(cutAfterRemeasure).Events[0].Steps[MetricStep.Recheck], Is.False);
        }

        [Test]
        public void RecurrenceEvents_AreIncludedInAccurateResponse()
        {
            var run = Run(AllCorrect(1).Concat(AllCorrect(2, true)).ToArray());

            var summary = MetricsCalculator.Summarize(run);

            Assert.That(summary.Events.Select(e => e.IsRecurrence), Is.EqualTo(new[] { false, true }));
            Assert.That(summary.Metrics.AccurateResponse.ToString(), Is.EqualTo("2/2"));
        }

        [Test]
        public void UnfinishedEvent_IsExcludedFromAccurateResponse_ButAnsweredStepsCount()
        {
            var run = Run(Triggered(1), Ok(1, HypoStep.Recognize));

            var summary = MetricsCalculator.Summarize(run);

            Assert.That(summary.Outcome, Is.EqualTo(RunOutcome.Unfinished));
            Assert.That(summary.Events.Single().IsFinished, Is.False);
            Assert.That(summary.Metrics.AccurateResponse.Rate, Is.Null);
            Assert.That(summary.Metrics.StepAccuracy[MetricStep.Recognize].ToString(), Is.EqualTo("1/1"));
        }

        [Test]
        public void MedianDecisionSec_ExcludesTimeouts()
        {
            var run = Run(
                Triggered(1), Ok(1, HypoStep.Recognize, 1), Ok(1, HypoStep.Respond, 1),
                Triggered(2), Ok(2, HypoStep.Recognize, 10), Wrong(2, HypoStep.Respond, HypoErrorCode.RsSlow, 3),
                Triggered(3), Ok(3, HypoStep.Recognize, 2), Timeout(3, HypoStep.Respond, HypoErrorCode.RsTimeout, 5));

            var medians = MetricsCalculator.Summarize(run).Metrics.MedianDecisionSec;

            Assert.That(medians[HypoStep.Recognize], Is.EqualTo(2), "1, 2, 10의 중앙값");
            Assert.That(medians[HypoStep.Respond], Is.EqualTo(2), "시간 초과 5를 빼고 1, 3의 중앙값");
            Assert.That(medians[HypoStep.Dose], Is.Null);
        }

        [Test]
        public void MeasuredFirst_CountsRecognizeAnswersBeforeWarning()
        {
            var run = Run(AllCorrect(1, measuredFirst: true)
                .Concat(AllCorrect(2, measuredFirst: false))
                .Concat(new object[] { Triggered(3), Timeout(3, HypoStep.Recognize, HypoErrorCode.SMissed) })
                .ToArray());

            Assert.That(MetricsCalculator.Summarize(run).Metrics.MeasuredFirst.ToString(), Is.EqualTo("1/3"));
        }

        [Test]
        public void Calculate_AggregatesRuns_AndCountsUnnecessaryMeasures()
        {
            var first = Run(new object[] { new MeterPressed(0, 100, true, false, 0, 0) }.Concat(AllCorrect(1)).ToArray());
            var second = Run(
                new MeterPressed(0, 90, true, false, 0, 0),
                new MeterPressed(0, 60, false, false, 0, 0),
                Triggered(1), Timeout(1, HypoStep.Recognize, HypoErrorCode.SMissed),
                new RunEliminated(1, HypoStep.Recognize, HypoErrorCode.SMissed, EliminationCause.Declined, 0, 0));

            var m = MetricsCalculator.Calculate(new[] { first, second });

            Assert.That(m.UnnecessaryMeasureCount, Is.EqualTo(2));
            Assert.That(m.AccurateResponse.ToString(), Is.EqualTo("1/2"));
        }

        [Test]
        public void EmptyRun_HasNoRates()
        {
            var summary = MetricsCalculator.Summarize(Run(new RunCompleted(40, 40, 40)));

            Assert.That(summary.Events, Is.Empty);
            Assert.That(summary.Metrics.AccurateResponse.Rate, Is.Null);
            Assert.That(summary.Metrics.MeasuredFirst.Rate, Is.Null);
            Assert.That(summary.Metrics.UnnecessaryMeasureCount, Is.EqualTo(0));
        }
    }
}
