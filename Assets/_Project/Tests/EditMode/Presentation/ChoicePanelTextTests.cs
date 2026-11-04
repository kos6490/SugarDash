using NUnit.Framework;

namespace SugarDash.Presentation.Tests
{
    public class ChoicePanelTextTests
    {
        private static readonly string[] Forbidden = { "나쁜", "죽", "생존", "치료" };

        private static string[] All() => new[]
        {
            ChoicePanelText.TellAdult, ChoicePanelText.KeepRunning, ChoicePanelText.NoSugar, ChoicePanelText.DoseUnder,
            ChoicePanelText.DoseProper, ChoicePanelText.DoseOver, ChoicePanelText.Recheck, ChoicePanelText.RunAgain,
            ChoicePanelText.Juice, ChoicePanelText.RespondTitle, ChoicePanelText.NextActionTitle, ChoicePanelText.HighTitle,
            ChoicePanelText.Type2TellTitle, ChoicePanelText.GuardianPromptTitle, ChoicePanelText.DoseTitle("cracker"),
            ChoicePanelText.RecheckResult(80),
        };

        [Test]
        public void EveryText_NotEmpty_AndHasNoForbiddenWords()
        {
            foreach (var text in All())
            {
                Assert.That(text, Is.Not.Empty);
                foreach (var word in Forbidden) Assert.That(text, Does.Not.Contain(word), text);
            }
        }

        [Test]
        public void TellAdult_IsFixed()
        {
            Assert.That(ChoicePanelText.TellAdult, Is.EqualTo("어른에게 알리기"));
        }

        [Test]
        public void DoseButtons_AreDistinct()
        {
            Assert.That(new[] { ChoicePanelText.DoseUnder, ChoicePanelText.DoseProper, ChoicePanelText.DoseOver },
                Is.Unique);
        }

        [Test]
        public void DoseTitle_ShowsSnackId()
        {
            Assert.That(ChoicePanelText.DoseTitle("cracker"), Does.Contain("cracker"));
        }

        [Test]
        public void RecheckResult_ShowsRoundedNumber()
        {
            Assert.That(ChoicePanelText.RecheckResult(79.6), Does.Contain("80"));
            Assert.That(ChoicePanelText.RecheckResult(79.6), Does.Not.Contain("."));
        }
    }
}
