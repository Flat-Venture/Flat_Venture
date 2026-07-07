using NUnit.Framework;
using FlatVenture.NUH.Seed;

namespace FlatVenture.NUH.Tests.EditMode
{
    public sealed class SeedValueTests
    {
        [Test]
        public void Format_AlwaysReturnsSixDigits()
        {
            Assert.That(SeedValue.Format(0), Is.EqualTo("000000"));
            Assert.That(SeedValue.Format(123456), Is.EqualTo("123456"));
            Assert.That(SeedValue.Format(999999), Is.EqualTo("999999"));
        }

        [Test]
        public void TryParse_AcceptsExactlySixNumericCharacters()
        {
            int seed;

            Assert.That(SeedValue.TryParse("000001", out seed), Is.True);
            Assert.That(seed, Is.EqualTo(1));
            Assert.That(SeedValue.TryParse("12345", out seed), Is.False);
            Assert.That(SeedValue.TryParse("1234567", out seed), Is.False);
            Assert.That(SeedValue.TryParse("12A456", out seed), Is.False);
            Assert.That(SeedValue.TryParse(" 12345", out seed), Is.False);
        }

        [Test]
        public void Generate_ReturnsDisplayableSixDigitValue()
        {
            for (int i = 0; i < 20; i++)
            {
                int seed = SeedValue.Generate();
                Assert.That(seed, Is.GreaterThanOrEqualTo(100000));
                Assert.That(seed, Is.LessThanOrEqualTo(999999));
                Assert.That(SeedValue.Format(seed).Length, Is.EqualTo(6));
            }
        }
    }
}
