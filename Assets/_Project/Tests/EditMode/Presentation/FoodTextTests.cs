using NUnit.Framework;

namespace SugarDash.Presentation.Tests
{
    public class FoodTextTests
    {
        private static readonly string[] Foods = { "water", "juice", "soda", "apple", "cracker", "chicken", "pizza", "burger" };

        [Test]
        public void EveryFood_HasNameOtherThanId_AndNamesAreUnique()
        {
            var names = new System.Collections.Generic.HashSet<string>();
            foreach (var id in Foods)
            {
                string name = FoodText.Name(id);
                Assert.That(name, Is.Not.Empty.And.Not.EqualTo(id), id);
                names.Add(name);
            }
            Assert.That(names.Count, Is.EqualTo(Foods.Length));
        }

        [Test]
        public void UnknownId_ReturnsId()
        {
            Assert.That(FoodText.Name("mystery"), Is.EqualTo("mystery"));
        }

        [TestCase("물", "을")]
        [TestCase("주스", "를")]
        public void Particle_FollowsFinalConsonant(string expectedName, string particle)
        {
            string id = expectedName == "물" ? "water" : "juice";
            // 이름은 문구 파일이라 바뀔 수 있으므로, 이름이 초안과 같을 때만 조사를 확인한다.
            if (FoodText.Name(id) != expectedName) Assert.Ignore("이름이 바뀜");
            Assert.That(FoodText.WithObjectParticle(id), Is.EqualTo(expectedName + particle));
        }

        [Test]
        public void NonHangulName_UsesBothParticles()
        {
            Assert.That(FoodText.WithObjectParticle("mystery"), Is.EqualTo("mystery을(를)"));
        }
    }
}
