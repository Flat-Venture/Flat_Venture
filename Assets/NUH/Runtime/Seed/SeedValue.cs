using System;
using System.Globalization;
using System.Security.Cryptography;

namespace FlatVenture.NUH.Seed
{
    /// <summary>
    /// 사용자에게 표시하는 6자리 시드의 생성, 검증, 변환을 담당합니다.
    /// </summary>
    public static class SeedValue
    {
        public const int MinimumValue = 0;
        public const int MaximumValue = 999999;
        public const int DigitCount = 6;

        private const uint GeneratedValueCount = 900000u;
        private const int GeneratedMinimumValue = 100000;

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

        public static bool IsValid(int seed)
        {
            return seed >= MinimumValue && seed <= MaximumValue;
        }

        public static string Format(int seed)
        {
            Validate(seed);
            return seed.ToString("D6", CultureInfo.InvariantCulture);
        }

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

        public static void Validate(int seed)
        {
            if (!IsValid(seed))
                throw new ArgumentOutOfRangeException(nameof(seed), "시드는 000000부터 999999까지 사용할 수 있습니다.");
        }
    }
}
