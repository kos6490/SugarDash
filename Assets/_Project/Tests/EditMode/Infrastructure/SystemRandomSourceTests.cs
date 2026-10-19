using NUnit.Framework;

namespace SugarDash.Infrastructure.Tests
{
    public class SystemRandomSourceTests
    {
        [Test]
        public void SameSeed_GivesSameSequenceInUnitRange()
        {
            var a = new SystemRandomSource(42);
            var b = new SystemRandomSource(42);

            for (int i = 0; i < 100; i++)
            {
                double value = a.NextDouble();
                Assert.That(value, Is.EqualTo(b.NextDouble()));
                Assert.That(value, Is.GreaterThanOrEqualTo(0).And.LessThan(1));
            }
        }
    }
}
