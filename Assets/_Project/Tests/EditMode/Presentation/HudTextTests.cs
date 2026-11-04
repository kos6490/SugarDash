using NUnit.Framework;

namespace SugarDash.Presentation.Tests
{
    public class HudTextTests
    {
        [Test]
        public void Measured_ShowsRoundedNumber()
        {
            var text = HudText.Measured(63.4);
            Assert.That(text, Does.Contain("63"));
            Assert.That(text, Does.Not.Contain("."));
        }

        [Test]
        public void Score_ShowsNumber()
        {
            Assert.That(HudText.Score(1234), Does.Contain("1234"));
        }

        [Test]
        public void Paused_NotEmpty()
        {
            Assert.That(HudText.Paused, Is.Not.Empty);
        }
    }
}
