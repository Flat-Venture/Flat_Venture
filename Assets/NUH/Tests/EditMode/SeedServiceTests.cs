using System.Collections.Generic;
using NUnit.Framework;
using FlatVenture.NUH.Seed;

namespace FlatVenture.NUH.Tests.EditMode
{
    public sealed class SeedServiceTests
    {
        [Test]
        public void GetStream_SameSeedAndName_ProducesSameSequence()
        {
            IRandomStream first = new SeedService(123456).GetStream(SeedStreamNames.Shop);
            IRandomStream second = new SeedService(123456).GetStream(SeedStreamNames.Shop);

            for (int i = 0; i < 100; i++)
                Assert.That(first.NextUInt32(), Is.EqualTo(second.NextUInt32()));
        }

        [Test]
        public void GetStream_DifferentRunSeed_ProducesDifferentSequence()
        {
            IRandomStream first = new SeedService(123456).GetStream(SeedStreamNames.Shop);
            IRandomStream second = new SeedService(654321).GetStream(SeedStreamNames.Shop);

            Assert.That(first.NextUInt32(), Is.Not.EqualTo(second.NextUInt32()));
        }

        [Test]
        public void GetStream_DifferentNames_ProduceIndependentSequences()
        {
            SeedService service = new SeedService(123456);
            IRandomStream map = service.GetStream(SeedStreamNames.Map);
            IRandomStream shop = service.GetStream(SeedStreamNames.Shop);

            Assert.That(map.NextUInt32(), Is.Not.EqualTo(shop.NextUInt32()));
        }

        [Test]
        public void GetStream_ExtraCallInOneStream_DoesNotAffectOtherStream()
        {
            SeedService firstService = new SeedService(123456);
            SeedService secondService = new SeedService(123456);
            IRandomStream firstMap = firstService.GetStream(SeedStreamNames.Map);
            IRandomStream firstShop = firstService.GetStream(SeedStreamNames.Shop);
            IRandomStream secondShop = secondService.GetStream(SeedStreamNames.Shop);

            for (int i = 0; i < 50; i++)
                firstMap.NextUInt32();

            for (int i = 0; i < 20; i++)
                Assert.That(firstShop.NextUInt32(), Is.EqualTo(secondShop.NextUInt32()));
        }

        [Test]
        public void GetStream_SameName_ReturnsSameInstance()
        {
            SeedService service = new SeedService(123456);

            IRandomStream first = service.GetStream(SeedStreamNames.Item);
            IRandomStream second = service.GetStream(SeedStreamNames.Item);

            Assert.That(second, Is.SameAs(first));
        }

        [Test]
        public void ShopStream_AlgorithmVersionOne_MatchesFixedRegressionValues()
        {
            uint[] expected =
            {
                2857546384u,
                4238085333u,
                2201865542u,
                2819321446u,
                3862857544u
            };

            IRandomStream stream = new SeedService(123456).GetStream(SeedStreamNames.Shop);

            Assert.That(SeedService.SeedAlgorithmVersion, Is.EqualTo(1));
            for (int i = 0; i < expected.Length; i++)
                Assert.That(stream.NextUInt32(), Is.EqualTo(expected[i]));
        }

        [Test]
        public void CaptureAndRestore_ContinuesFromExactPosition()
        {
            IRandomStream stream = new SeedService(123456).GetStream(SeedStreamNames.Event);
            for (int i = 0; i < 17; i++)
                stream.NextUInt32();

            RandomStreamState savedState = stream.CaptureState();
            uint[] expected = new uint[20];
            for (int i = 0; i < expected.Length; i++)
                expected[i] = stream.NextUInt32();

            stream.RestoreState(savedState);

            Assert.That(stream.CallCount, Is.EqualTo(savedState.CallCount));
            for (int i = 0; i < expected.Length; i++)
                Assert.That(stream.NextUInt32(), Is.EqualTo(expected[i]));
        }

        [Test]
        public void CaptureAllAndRestore_RecreatesEveryCreatedStream()
        {
            SeedService source = new SeedService(123456);
            source.GetStream(SeedStreamNames.Map).NextUInt32();
            source.GetStream(SeedStreamNames.Map).NextUInt32();
            source.GetStream(SeedStreamNames.Forge).NextUInt32();
            IReadOnlyList<RandomStreamState> states = source.CaptureAllStreamStates();

            uint expectedMap = source.GetStream(SeedStreamNames.Map).NextUInt32();
            uint expectedForge = source.GetStream(SeedStreamNames.Forge).NextUInt32();
            SeedService restored = new SeedService(123456);
            restored.RestoreStreamStates(states);

            Assert.That(restored.GetStream(SeedStreamNames.Map).NextUInt32(), Is.EqualTo(expectedMap));
            Assert.That(restored.GetStream(SeedStreamNames.Forge).NextUInt32(), Is.EqualTo(expectedForge));
        }

        [Test]
        public void RestoreState_DifferentRunSeed_IsRejected()
        {
            IRandomStream source = new SeedService(123456).GetStream(SeedStreamNames.Map);
            RandomStreamState savedState = source.CaptureState();
            IRandomStream wrongRun = new SeedService(654321).GetStream(SeedStreamNames.Map);
            bool rejected = false;

            try
            {
                wrongRun.RestoreState(savedState);
            }
            catch (System.ArgumentException)
            {
                rejected = true;
            }

            Assert.That(rejected, Is.True);
        }
    }
}
