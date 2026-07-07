using System.Collections.Generic;
using NUnit.Framework;
using FlatVenture.NUH.Seed;

namespace FlatVenture.NUH.Tests.EditMode
{
    public sealed class RandomStreamApiTests
    {
        [Test]
        public void Range_Int_UsesInclusiveMinimumAndExclusiveMaximum()
        {
            IRandomStream stream = new SeedService(123456).GetStream("IntegerRange");

            for (int i = 0; i < 1000; i++)
            {
                int value = stream.Range(-7, 13);
                Assert.That(value, Is.GreaterThanOrEqualTo(-7));
                Assert.That(value, Is.LessThan(13));
            }
        }

        [Test]
        public void Range_Float_UsesInclusiveMinimumAndExclusiveMaximum()
        {
            IRandomStream stream = new SeedService(123456).GetStream("FloatRange");

            for (int i = 0; i < 1000; i++)
            {
                float value = stream.Range(-2.5f, 9.25f);
                Assert.That(value, Is.GreaterThanOrEqualTo(-2.5f));
                Assert.That(value, Is.LessThan(9.25f));
            }
        }

        [Test]
        public void Chance_ZeroAndOne_ReturnExpectedResultAndConsumeCalls()
        {
            IRandomStream stream = new SeedService(123456).GetStream("Chance");

            Assert.That(stream.Chance(0f), Is.False);
            Assert.That(stream.Chance(1f), Is.True);
            Assert.That(stream.CallCount, Is.EqualTo(2));
        }

        [Test]
        public void Pick_SameSeed_ReproducesSelection()
        {
            string[] candidates = { "A", "B", "C", "D" };
            IRandomStream first = new SeedService(123456).GetStream("Pick");
            IRandomStream second = new SeedService(123456).GetStream("Pick");

            for (int i = 0; i < 20; i++)
                Assert.That(first.Pick(candidates), Is.EqualTo(second.Pick(candidates)));
        }

        [Test]
        public void Shuffle_SameSeed_ReproducesOrder()
        {
            List<int> firstValues = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            List<int> secondValues = new List<int>(firstValues);
            IRandomStream first = new SeedService(123456).GetStream("Shuffle");
            IRandomStream second = new SeedService(123456).GetStream("Shuffle");

            first.Shuffle(firstValues);
            second.Shuffle(secondValues);

            CollectionAssert.AreEqual(firstValues, secondValues);
            CollectionAssert.AreNotEqual(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, firstValues);
        }

        [Test]
        public void WeightedIndex_ZeroWeightCandidate_IsNeverSelected()
        {
            float[] weights = { 1f, 0f, 2f };
            IRandomStream stream = new SeedService(123456).GetStream("Weighted");

            for (int i = 0; i < 1000; i++)
                Assert.That(stream.WeightedIndex(weights), Is.Not.EqualTo(1));
        }

        [Test]
        public void WeightedPick_ReturnsCandidateAtWeightedIndex()
        {
            string[] candidates = { "Common", "Disabled", "Rare" };
            float[] weights = { 0f, 0f, 1f };
            IRandomStream stream = new SeedService(123456).GetStream("WeightedPick");

            Assert.That(stream.WeightedPick(candidates, weights), Is.EqualTo("Rare"));
        }
    }
}
