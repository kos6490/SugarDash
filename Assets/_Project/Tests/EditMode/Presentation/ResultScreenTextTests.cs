using System.Collections.Generic;
using SugarDash.Domain.Metrics;
using NUnit.Framework;

namespace SugarDash.Presentation.Tests
{
    public class ResultScreenTextTests
    {
        private static EventResult Event(bool allCorrect)
        {
            var steps = new Dictionary<MetricStep, bool?>
            {
                [MetricStep.Recognize] = true,
                [MetricStep.Respond] = allCorrect,
                [MetricStep.Dose] = true,
                [MetricStep.Recheck] = true,
            };
            return new EventResult(1, false, steps, allCorrect, true);
        }

        private static RunSummary Summary(RunOutcome outcome, params EventResult[] events) =>
            new RunSummary("run", outcome, events, null);

        [Test]
        public void Title_DependsOnOutcome()
        {
            Assert.That(ResultScreenText.Title(Summary(RunOutcome.Completed)), Is.EqualTo("끝까지 달렸어요!"));
            Assert.That(ResultScreenText.Title(Summary(RunOutcome.Eliminated)), Is.EqualTo("어른이 도와주러 왔어요"));
        }

        [Test]
        public void MissingOrUnfinishedResult_ShowsLoadFailure_WithoutSummary()
        {
            foreach (var result in new[] { null, Summary(RunOutcome.Unfinished, Event(true)) })
            {
                Assert.That(ResultScreenText.Title(result), Is.EqualTo("결과를 불러오지 못했어요"));
                Assert.That(ResultScreenText.Summary(result), Is.Empty);
            }
        }

        [Test]
        public void Summary_CountsAllCorrectEvents()
        {
            var result = Summary(RunOutcome.Completed, Event(true), Event(false));

            Assert.That(ResultScreenText.Summary(result), Is.EqualTo("저혈당 2번 중 1번 모두 잘 해냈어요"));
        }

        [Test]
        public void Summary_WithNoAllCorrectEvent_Encourages()
        {
            var result = Summary(RunOutcome.Eliminated, Event(false), Event(false));

            Assert.That(ResultScreenText.Summary(result), Is.EqualTo("저혈당이 2번 왔어요.\n다음에는 꼭 해낼 수 있어요!"));
        }

        [Test]
        public void Summary_WithoutEvents_SaysNoHypo()
        {
            Assert.That(ResultScreenText.Summary(Summary(RunOutcome.Completed)), Is.EqualTo("이번에는 저혈당이 없었어요"));
        }
    }
}
