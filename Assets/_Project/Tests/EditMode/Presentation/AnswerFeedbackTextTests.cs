using SugarDash.Domain.Events;
using SugarDash.Domain.Hypo;
using NUnit.Framework;

namespace SugarDash.Presentation.Tests
{
    public class AnswerFeedbackTextTests
    {
        private static StepAnswered Answer(string choiceId, bool helpInstead = false) =>
            new StepAnswered(1, HypoStep.Respond, choiceId, false, HypoErrorCode.None, helpInstead, 1, false, 0, 0);

        private static readonly string[] Forbidden = { "나쁜", "죽", "생존", "치료" };

        [TestCase("meter")]
        [TestCase("keep_running")]
        [TestCase("no_sugar")]
        [TestCase("under")]
        [TestCase("proper")]
        [TestCase("over")]
        [TestCase("recheck")]
        [TestCase("run_again")]
        [TestCase("help")]
        [TestCase("juice")]
        public void For_KnownChoice_HasTextWithoutForbiddenWords(string choiceId)
        {
            var text = AnswerFeedbackText.For(Answer(choiceId));
            Assert.That(text, Is.Not.Empty);
            foreach (var word in Forbidden) Assert.That(text, Does.Not.Contain(word), text);
        }

        [Test]
        public void For_KnownChoices_AreDistinguishable()
        {
            var ids = new[] { "meter", "keep_running", "no_sugar", "under", "proper", "over", "recheck", "run_again", "juice" };
            var texts = new System.Collections.Generic.HashSet<string>();
            foreach (var id in ids) texts.Add(AnswerFeedbackText.For(Answer(id)));
            Assert.That(texts.Count, Is.EqualTo(ids.Length));
        }

        [Test]
        public void For_SnackId_ShowsFoodName()
        {
            Assert.That(AnswerFeedbackText.For(Answer("cracker")), Does.Contain(FoodText.Name("cracker")));
        }

        [Test]
        public void For_Timeout()
        {
            var text = AnswerFeedbackText.For(Answer(null));
            Assert.That(text, Is.Not.Empty);
            Assert.That(text, Is.Not.EqualTo(AnswerFeedbackText.For(Answer("meter"))));
        }

        [Test]
        public void For_HelpInstead_SaysNotified()
        {
            Assert.That(AnswerFeedbackText.For(Answer("help", helpInstead: true)), Is.Not.Empty);
            Assert.That(AnswerFeedbackText.For(Answer("help", helpInstead: true)),
                Is.EqualTo(AnswerFeedbackText.ForGuardianNotified()));
            Assert.That(AnswerFeedbackText.For(Answer("meter", helpInstead: true)),
                Is.EqualTo(AnswerFeedbackText.ForGuardianNotified()));
        }

        [Test]
        public void ForGuardianNotified()
        {
            Assert.That(AnswerFeedbackText.ForGuardianNotified(), Is.Not.Empty);
            Assert.That(AnswerFeedbackText.For(Answer("help")), Is.EqualTo(AnswerFeedbackText.ForGuardianNotified()));
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
