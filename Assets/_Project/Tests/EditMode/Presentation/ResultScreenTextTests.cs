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

        private static RunSummary Type2Summary(bool[] high, bool[] low)
        {
            var hyper = new List<HyperEventResult>();
            for (int i = 0; i < high.Length; i++) hyper.Add(new HyperEventResult(10 + i, high[i], true));
            var lows = new List<LowEventResult>();
            for (int i = 0; i < low.Length; i++) lows.Add(new LowEventResult(20 + i, low[i], true));
            return new RunSummary("run", RunOutcome.Eliminated, new EventResult[0], hyper, null, lows,
                SugarDash.Domain.Config.TrackType.Type2);
        }

        [Test]
        public void TutorialTitle_Text()
        {
            Assert.That(ResultScreenText.TutorialTitle, Is.EqualTo("튜토리얼 끝!"));
        }

        [Test]
        public void Type2_SummaryLines()
        {
            Assert.That(ResultScreenText.Summary(Type2Summary(new[] { true, false }, new[] { true })),
                Is.EqualTo("혈당이 높을 때 2번 중 1번 바로 알렸어요\n혈당이 낮을 때 1번 모두 바로 알렸어요"));
            Assert.That(ResultScreenText.Summary(Type2Summary(new[] { true, true }, new[] { true, false, false })),
                Is.EqualTo("혈당이 높을 때 2번 모두 바로 알렸어요\n혈당이 낮을 때 3번 중 1번 바로 알렸어요"));
            Assert.That(ResultScreenText.Summary(Type2Summary(new[] { false, false }, new[] { false })),
                Is.EqualTo("혈당이 높을 때 2번 모두 아쉬웠어요\n혈당이 낮을 때 1번 모두 아쉬웠어요\n다음에 더 잘해봐요!"));
        }

        [Test]
        public void Type2_NoHypoLineWhenNoEvents()
        {
            var none = ResultScreenText.Summary(Type2Summary(new bool[0], new bool[0]));
            Assert.That(none, Is.Empty);
            Assert.That(none, Does.Not.Contain("저혈당"));

            var onlyLow = ResultScreenText.Summary(Type2Summary(new bool[0], new[] { true }));
            Assert.That(onlyLow, Is.EqualTo("혈당이 낮을 때 1번 모두 바로 알렸어요"));
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
    
        [Test]
        public void Summary_WithScore_StartsWithScoreLines()
        {
            var lines = ResultScreenText.Summary(Summary(RunOutcome.Eliminated), 123.9, 200, false).Split('\n');

            Assert.That(lines[0], Is.EqualTo("점수 123점"));
            Assert.That(lines[1], Is.EqualTo("최고 점수 200점"));
            Assert.That(lines[2], Is.EqualTo("이번에는 저혈당이 없었어요"));
        }

        [Test]
        public void Summary_NewBest_AddsLine()
        {
            var lines = ResultScreenText.Summary(Summary(RunOutcome.Eliminated), 300.2, 300.2, true).Split('\n');

            Assert.That(lines[2], Is.EqualTo("새 최고 기록!"));
        }

        [Test]
        public void Summary_NoScore_HasNoScoreLines()
        {
            Assert.That(ResultScreenText.Summary(Summary(RunOutcome.Eliminated), null, 200, false),
                Is.EqualTo("이번에는 저혈당이 없었어요"));
        }
}
}
