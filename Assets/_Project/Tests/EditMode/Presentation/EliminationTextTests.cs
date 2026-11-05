using System;
using SugarDash.Domain.Hypo;
using NUnit.Framework;

namespace SugarDash.Presentation.Tests
{
    public class EliminationTextTests
    {
        [Test]
        public void EveryCause_HasText()
        {
            foreach (EliminationCause cause in Enum.GetValues(typeof(EliminationCause)))
                Assert.That(EliminationText.For(cause), Is.Not.Empty);
        }

        [Test]
        public void NoCause_IsEmpty()
        {
            Assert.That(EliminationText.For(null), Is.Empty);
        }

        [Test]
        public void CausesShareThreeLines()
        {
            Assert.That(EliminationText.For(EliminationCause.PromptTimeout),
                Is.EqualTo(EliminationText.For(EliminationCause.Declined)), "알리지 않음");
            Assert.That(EliminationText.For(EliminationCause.GlucoseHigh),
                Is.EqualTo(EliminationText.For(EliminationCause.GlucoseLow)), "혈당 범위");
            Assert.That(EliminationText.For(EliminationCause.ObstacleHit),
                Is.Not.EqualTo(EliminationText.For(EliminationCause.Declined)));
            Assert.That(EliminationText.For(EliminationCause.ObstacleHit),
                Is.Not.EqualTo(EliminationText.For(EliminationCause.GlucoseLow)));
            Assert.That(EliminationText.For(EliminationCause.Declined),
                Is.Not.EqualTo(EliminationText.For(EliminationCause.GlucoseLow)));
        }
    }
}
