using System.Linq;
using SugarDash.Domain.Config;
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

        private static SnackConfig Snack(string id, SnackKind kind) => new SnackConfig(id, kind, 30, 1, 2, "test");

        private static readonly SnackConfig[] Snacks =
        {
            Snack("juice", SnackKind.Fast), Snack("soda", SnackKind.Fast),
            Snack("cracker", SnackKind.Slow), Snack("apple", SnackKind.Slow),
        };

        [Test]
        public void RespondSnacks_PicksOneFastAndOneSlow()
        {
            for (int seed = 0; seed < 20; seed++)
            {
                var picked = ChoiceOrder.RespondSnacks(Snacks, new System.Random(seed));
                Assert.That(picked.Select(s => s.Kind), Is.EqualTo(new[] { SnackKind.Fast, SnackKind.Slow }));
            }
        }

        [Test]
        public void RespondSnacks_VariesAcrossSeeds_AndCoversEverySnack()
        {
            var seen = Enumerable.Range(0, 50)
                .SelectMany(seed => ChoiceOrder.RespondSnacks(Snacks, new System.Random(seed)))
                .Select(s => s.Id).Distinct().OrderBy(x => x);
            Assert.That(seen, Is.EqualTo(new[] { "apple", "cracker", "juice", "soda" }));
        }

        [Test]
        public void RespondSnacks_WithoutSlow_PicksOnlyFast()
        {
            var picked = ChoiceOrder.RespondSnacks(new[] { Snack("juice", SnackKind.Fast) }, new System.Random(1));
            Assert.That(picked.Single().Id, Is.EqualTo("juice"));
        }
    }
}
