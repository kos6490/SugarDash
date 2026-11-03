using System.Linq;
using NUnit.Framework;

namespace SugarDash.Presentation.Tests
{
    public class ChoiceOrderTests
    {
        private static readonly string[] Items = { "a", "b", "c", "d", "e" };

        [Test]
        public void Shuffle_SameSeed_SameOrder()
        {
            var first = ChoiceOrder.Shuffle(Items, new System.Random(7));
            var second = ChoiceOrder.Shuffle(Items, new System.Random(7));
            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void Shuffle_IsPermutation()
        {
            var result = ChoiceOrder.Shuffle(Items, new System.Random(3));
            Assert.That(result.OrderBy(x => x), Is.EqualTo(Items));
        }

        [Test]
        public void Shuffle_DoesNotMutateInput()
        {
            var input = (string[])Items.Clone();
            ChoiceOrder.Shuffle(input, new System.Random(1));
            Assert.That(input, Is.EqualTo(Items));
        }

        [Test]
        public void Shuffle_VariesAcrossSeeds()
        {
            var orders = Enumerable.Range(0, 50)
                .Select(seed => string.Join("", ChoiceOrder.Shuffle(Items, new System.Random(seed))))
                .Distinct().Count();
            Assert.That(orders, Is.GreaterThanOrEqualTo(2));
        }
    }
}
