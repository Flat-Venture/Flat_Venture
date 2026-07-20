using System;
using System.Globalization;
using System.Security.Cryptography;

/// <summary>
/// 사용자에게 표시하는 6자리 시드의 생성, 검증, 변환을 담당합니다.
/// </summary>
public static class SeedValue
{
    // 사용자가 직접 입력할 수 있는 6자리 표현 범위입니다. 000000도 유효합니다.
    public const int MinimumValue = 0;
    public const int MaximumValue = 999999;
    public const int DigitCount = 6;

    // 자동 생성은 화면에 항상 6자리가 보이도록 100000부터 시작합니다.
    private const uint GeneratedValueCount = 900000u;
    private const int GeneratedMinimumValue = 100000;

    /// <summary>암호학적 난수로 편향 없는 100000~999999 값을 생성합니다.</summary>
    public static int Generate()
    {
        uint threshold = unchecked(0u - GeneratedValueCount) % GeneratedValueCount;
        byte[] bytes = new byte[sizeof(uint)];

        using (RandomNumberGenerator generator = RandomNumberGenerator.Create())
        {
            uint value;
            do
            {
                generator.GetBytes(bytes);
                value = BitConverter.ToUInt32(bytes, 0);
            }
            while (value < threshold);

            return GeneratedMinimumValue + (int)(value % GeneratedValueCount);
        }
    }

    /// <summary>정수가 6자리 표시 가능한 범위인지 확인합니다.</summary>
    public static bool IsValid(int seed)
    {
        return seed >= MinimumValue && seed <= MaximumValue;
    }

    /// <summary>정수를 앞자리 0을 포함한 정확한 6자리 문자열로 바꿉니다.</summary>
    public static string Format(int seed)
    {
        Validate(seed);
        return seed.ToString("D6", CultureInfo.InvariantCulture);
    }

    /// <summary>공백·부호 없이 숫자 6개로만 이루어진 문자열을 파싱합니다.</summary>
    public static bool TryParse(string text, out int seed)
    {
        seed = 0;
        if (string.IsNullOrEmpty(text) || text.Length != DigitCount)
            return false;

        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] < '0' || text[i] > '9')
                return false;
        }

        return int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out seed)
            && IsValid(seed);
    }

    /// <summary>유효 범위가 아니면 호출자의 잘못된 사용을 알리는 예외를 발생시킵니다.</summary>
    public static void Validate(int seed)
    {
        if (!IsValid(seed))
            throw new ArgumentOutOfRangeException(nameof(seed), "시드는 000000부터 999999까지 사용할 수 있습니다.");
    }
}
