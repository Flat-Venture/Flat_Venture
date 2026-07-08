using NUnit.Framework;
using FlatVenture.NUH.Seed;

namespace FlatVenture.NUH.Tests.EditMode
{
    /// <summary>MT19937 핵심 구현이 공식 기준 난수열과 일치하는지 검증합니다.</summary>
    public sealed class MersenneTwister19937Tests
    {
        // 공식 기준 시드 5489의 첫 10개 값이 바뀌면 알고리즘 호환성이 깨진 것입니다.
        [Test]
        public void NextUInt32_OfficialReferenceSeed_MatchesReferenceValues()
        {
            uint[] expected =
            {
                3499211612u,
                581869302u,
                3890346734u,
                3586334585u,
                545404204u,
                4161255391u,
                3922919429u,
                949333985u,
                2715962298u,
                1323567403u
            };

            MersenneTwister19937 generator = new MersenneTwister19937(5489u);

            for (int i = 0; i < expected.Length; i++)
                Assert.That(generator.NextUInt32(), Is.EqualTo(expected[i]));
        }
    }
}
