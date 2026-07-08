using System.Collections.Generic;
using NUnit.Framework;
using FlatVenture.NUH.Seed;

namespace FlatVenture.NUH.Tests.EditMode
{
    /// <summary>RunSeed와 이름별 독립 스트림의 생성·재현·상태 복원을 검증합니다.</summary>
    public sealed class SeedServiceTests
    {
        // 같은 RunSeed와 스트림 이름이 같은 난수열을 만드는지 확인합니다.
        [Test]
        public void GetStream_SameSeedAndName_ProducesSameSequence()
        {
            IRandomStream first = new SeedService(123456).GetStream(SeedStreamNames.Shop);
            IRandomStream second = new SeedService(123456).GetStream(SeedStreamNames.Shop);

            for (int i = 0; i < 100; i++)
                Assert.That(first.NextUInt32(), Is.EqualTo(second.NextUInt32()));
        }

        // RunSeed가 달라지면 파생 난수열도 달라지는지 확인합니다.
        [Test]
        public void GetStream_DifferentRunSeed_ProducesDifferentSequence()
        {
            IRandomStream first = new SeedService(123456).GetStream(SeedStreamNames.Shop);
            IRandomStream second = new SeedService(654321).GetStream(SeedStreamNames.Shop);

            Assert.That(first.NextUInt32(), Is.Not.EqualTo(second.NextUInt32()));
        }

        // 같은 RunSeed라도 스트림 이름이 다르면 독립 난수열인지 확인합니다.
        [Test]
        public void GetStream_DifferentNames_ProduceIndependentSequences()
        {
            SeedService service = new SeedService(123456);
            IRandomStream map = service.GetStream(SeedStreamNames.Map);
            IRandomStream shop = service.GetStream(SeedStreamNames.Shop);

            Assert.That(map.NextUInt32(), Is.Not.EqualTo(shop.NextUInt32()));
        }

        // 한 스트림의 추가 호출이 다른 스트림 소비 위치를 바꾸지 않는지 확인합니다.
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

        // 같은 이름을 다시 요청할 때 새 객체가 아닌 기존 스트림이 반환되는지 확인합니다.
        [Test]
        public void GetStream_SameName_ReturnsSameInstance()
        {
            SeedService service = new SeedService(123456);

            IRandomStream first = service.GetStream(SeedStreamNames.Item);
            IRandomStream second = service.GetStream(SeedStreamNames.Item);

            Assert.That(second, Is.SameAs(first));
        }

        // 알고리즘 버전 1의 고정 Shop 결과가 업데이트 후에도 유지되는지 확인합니다.
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

        // 한 스트림의 상태 저장·복원이 정확히 다음 위치를 재현하는지 확인합니다.
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

        // 여러 스트림 상태를 한꺼번에 저장·복원해도 각각 이어지는지 확인합니다.
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

        // 다른 RunSeed에서 만든 상태가 잘못 복원되지 않게 거부하는지 확인합니다.
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
