using NUnit.Framework;

/// <summary>6자리 시드의 표시·입력·자동 생성 규칙을 검증합니다.</summary>
public sealed class SeedValueTests
{
    // 정수 크기와 관계없이 앞자리 0을 채워 정확히 6자리인지 확인합니다.
    [Test]
    public void Format_AlwaysReturnsSixDigits()
    {
        Assert.That(SeedValue.Format(0), Is.EqualTo("000000"));
        Assert.That(SeedValue.Format(123456), Is.EqualTo("123456"));
        Assert.That(SeedValue.Format(999999), Is.EqualTo("999999"));
    }

    // 숫자 6개 이외의 길이·문자·공백 입력이 거부되는지 확인합니다.
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

    // 자동 생성값이 사용자가 볼 수 있는 6자리 범위에 들어오는지 반복 확인합니다.
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
