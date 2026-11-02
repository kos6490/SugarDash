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
            new RunSummary("run", outcome, events, new HyperEventResult[0], null);

        private static RunSummary WithHyper(RunSummary hypo, params bool[] hyperAllCorrect)
        {
            var hyper = new List<HyperEventResult>();
            for (int i = 0; i < hyperAllCorrect.Length; i++) hyper.Add(new HyperEventResult(10 + i, hyperAllCorrect[i], true));
            return new RunSummary(hypo.RunId, hypo.Outcome, hypo.Events, hyper, null);
        }

        [Test]
        public void Title_Eliminated()
        {
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
            var result = Summary(RunOutcome.Eliminated, Event(true), Event(false));

            Assert.That(ResultScreenText.Summary(result), Is.EqualTo("저혈당 2번 중 1번 모두 잘 해냈어요"));
        }

        [Test]
        public void Summary_WithNoAllCorrectEvent_Encourages()
        {
            var result = Summary(RunOutcome.Eliminated, Event(false), Event(false));

            Assert.That(ResultScreenText.Summary(result), Is.EqualTo("저혈당 2번 모두 아쉬웠어요\n다음에 더 잘해봐요!"));
        }

        [Test]
        public void Summary_AddsHyperLine_CountingImmediateNotify()
        {
            var result = WithHyper(Summary(RunOutcome.Eliminated, Event(true)), true, false);

            Assert.That(ResultScreenText.Summary(result),
                Is.EqualTo("저혈당 1번 모두 잘 해냈어요\n고혈당 2번 중 1번 바로 알렸어요"));
        }

        [Test]
        public void Summary_WithHyperButNoImmediateNotify_SaysCountOnly()
        {
            var result = WithHyper(Summary(RunOutcome.Eliminated), false);

            Assert.That(ResultScreenText.Summary(result), Is.EqualTo("이번에는 저혈당이 없었어요\n고혈당 1번 모두 아쉬웠어요\n다음에 더 잘해봐요!"));
        }

        [Test]
        public void Summary_WithBothMissed_AddsEncouragementOnceAtBottom()
        {
            var result = WithHyper(Summary(RunOutcome.Eliminated, Event(false)), false);

            Assert.That(ResultScreenText.Summary(result),
                Is.EqualTo("저혈당 1번 모두 아쉬웠어요\n고혈당 1번 모두 아쉬웠어요\n다음에 더 잘해봐요!"));
        }

        [Test]
        public void Summary_WithHypoMissedButHyperNotified_EncouragesAtBottom()
        {
            var result = WithHyper(Summary(RunOutcome.Eliminated, Event(false)), true);

            Assert.That(ResultScreenText.Summary(result),
                Is.EqualTo("저혈당 1번 모두 아쉬웠어요\n고혈당 1번 모두 바로 알렸어요\n다음에 더 잘해봐요!"));
        }

        [Test]
        public void Summary_WhenEveryEventWentWell_SaysAllInsteadOfCount()
        {
            var result = WithHyper(Summary(RunOutcome.Eliminated, Event(true), Event(true)), true, true);

            Assert.That(ResultScreenText.Summary(result),
                Is.EqualTo("저혈당 2번 모두 잘 해냈어요\n고혈당 2번 모두 바로 알렸어요"));
        }

        [Test]
        public void Summary_WithoutEvents_SaysNoHypo()
        {
            Assert.That(ResultScreenText.Summary(Summary(RunOutcome.Eliminated)), Is.EqualTo("이번에는 저혈당이 없었어요"));
        }
    }
}
