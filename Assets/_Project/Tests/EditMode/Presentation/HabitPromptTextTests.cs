using System;
using SugarDash.Domain.Config;
using NUnit.Framework;

namespace SugarDash.Presentation.Tests
{
    public class HabitPromptTextTests
    {
        [Test]
        public void EveryMoment_HasText()
        {
            foreach (HabitMoment moment in Enum.GetValues(typeof(HabitMoment)))
                Assert.That(HabitPromptText.For(moment), Is.Not.Empty);
        }

        [Test]
        public void NoMoment_IsEmpty()
        {
            Assert.That(HabitPromptText.For(null), Is.Empty);
        }
    }
}
