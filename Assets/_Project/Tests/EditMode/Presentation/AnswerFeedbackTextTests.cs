using SugarDash.Domain.Events;
using SugarDash.Domain.Hypo;
using NUnit.Framework;

namespace SugarDash.Presentation.Tests
{
    public class AnswerFeedbackTextTests
    {
        private static StepAnswered Answer(string choiceId, bool helpInstead = false) =>
            new StepAnswered(1, HypoStep.Respond, choiceId, false, HypoErrorCode.None, helpInstead, 1, false, 0, 0);

        [TestCase("meter", "측정했어요")]
        [TestCase("keep_running", "계속 달리기를 골랐어요")]
        [TestCase("no_sugar", "먹을 게 없다고 했어요")]
        [TestCase("under", "조금 먹었어요")]
        [TestCase("proper", "알맞게 먹었어요")]
        [TestCase("over", "많이 먹었어요")]
        [TestCase("recheck", "다시 쟀어요")]
        [TestCase("run_again", "다시 달리기를 골랐어요")]
        [TestCase("help", "어른에게 알렸어요")]
        [TestCase("juice", "주스를 마셨어요")]
        [TestCase("cracker", "cracker을(를) 골랐어요")]
        public void For_SaysWhatWasChosen(string choiceId, string expected)
        {
            Assert.That(AnswerFeedbackText.For(Answer(choiceId)), Is.EqualTo(expected));
        }

        [Test]
        public void For_Timeout()
        {
            Assert.That(AnswerFeedbackText.For(Answer(null)), Is.EqualTo("시간이 다 됐어요"));
        }

        [Test]
        public void For_HelpInstead_SaysNotified()
        {
            Assert.That(AnswerFeedbackText.For(Answer("help", helpInstead: true)), Is.EqualTo("어른에게 알렸어요"));
        }

        [Test]
        public void ForGuardianNotified()
        {
            Assert.That(AnswerFeedbackText.ForGuardianNotified(), Is.EqualTo("어른에게 알렸어요"));
        }

        [TestCase(3.0, 3)]
        [TestCase(2.01, 3)]
        [TestCase(2.0, 2)]
        [TestCase(0.001, 1)]
        public void CountdownNumber_UsesCeiling(double remaining, int expected)
        {
            Assert.That(AnswerFeedbackText.CountdownNumber(remaining), Is.EqualTo(expected));
        }

        [TestCase(0.0)]
        [TestCase(-1.0)]
        [TestCase(double.NaN)]
        public void CountdownNumber_HiddenAtZeroOrBelow(double remaining)
        {
            Assert.That(AnswerFeedbackText.CountdownNumber(remaining), Is.Null);
        }
    }
}
