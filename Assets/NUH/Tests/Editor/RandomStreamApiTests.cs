using System.Collections.Generic;
using NUnit.Framework;

/// <summary>정수·실수·확률·선택·셔플·가중치 공개 API의 경계 규칙을 검증합니다.</summary>
public sealed class RandomStreamApiTests
{
    // 정수 Range가 최솟값을 포함하고 최댓값을 제외하는지 확인합니다.
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

    // 실수 Range도 같은 포함/제외 규칙을 지키는지 확인합니다.
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

    // 확률 0과 1이 고정 결과를 내면서 호출 수는 각각 소비하는지 확인합니다.
    [Test]
    public void Chance_ZeroAndOne_ReturnExpectedResultAndConsumeCalls()
    {
        IRandomStream stream = new SeedService(123456).GetStream("Chance");

        Assert.That(stream.Chance(0f), Is.False);
        Assert.That(stream.Chance(1f), Is.True);
        Assert.That(stream.CallCount, Is.EqualTo(2));
    }

    // 같은 시드의 Pick 결과가 호출 순서별로 재현되는지 확인합니다.
    [Test]
    public void Pick_SameSeed_ReproducesSelection()
    {
        string[] candidates = { "A", "B", "C", "D" };
        IRandomStream first = new SeedService(123456).GetStream("Pick");
        IRandomStream second = new SeedService(123456).GetStream("Pick");

        for (int i = 0; i < 20; i++)
            Assert.That(first.Pick(candidates), Is.EqualTo(second.Pick(candidates)));
    }

    // Fisher-Yates 셔플이 같은 시드에서 같은 순서를 만드는지 확인합니다.
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

    // 가중치 0인 후보가 반복 실행에서도 선택되지 않는지 확인합니다.
    [Test]
    public void WeightedIndex_ZeroWeightCandidate_IsNeverSelected()
    {
        float[] weights = { 1f, 0f, 2f };
        IRandomStream stream = new SeedService(123456).GetStream("Weighted");

        for (int i = 0; i < 1000; i++)
            Assert.That(stream.WeightedIndex(weights), Is.Not.EqualTo(1));
    }

    // WeightedPick이 가중치로 선택한 인덱스의 후보를 반환하는지 확인합니다.
    [Test]
    public void WeightedPick_ReturnsCandidateAtWeightedIndex()
    {
        string[] candidates = { "Common", "Disabled", "Rare" };
        float[] weights = { 0f, 0f, 1f };
        IRandomStream stream = new SeedService(123456).GetStream("WeightedPick");

        Assert.That(stream.WeightedPick(candidates, weights), Is.EqualTo("Rare"));
    }
}
