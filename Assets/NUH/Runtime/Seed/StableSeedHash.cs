using System;
using System.Text;

namespace FlatVenture.NUH.Seed
{
    /// <summary>
    /// 런 시드와 스트림 이름을 플랫폼에 독립적인 32비트 시드로 조합합니다.
    /// FNV-1a 처리 순서는 알고리즘 버전 1의 고정 규칙입니다.
    /// </summary>
    internal static class StableSeedHash
    {
        private const uint OffsetBasis = 2166136261u;
        private const uint Prime = 16777619u;

        internal static uint Derive(int runSeed, string streamName)
        {
            if (string.IsNullOrWhiteSpace(streamName))
                throw new ArgumentException("스트림 이름은 비어 있을 수 없습니다.", nameof(streamName));

            uint hash = OffsetBasis;
            hash = AddByte(hash, (byte)runSeed);
            hash = AddByte(hash, (byte)(runSeed >> 8));
            hash = AddByte(hash, (byte)(runSeed >> 16));
            hash = AddByte(hash, (byte)(runSeed >> 24));
            hash = AddByte(hash, 0xFF);

            byte[] nameBytes = Encoding.UTF8.GetBytes(streamName);
            for (int i = 0; i < nameBytes.Length; i++)
                hash = AddByte(hash, nameBytes[i]);

            return hash;
        }

        private static uint AddByte(uint hash, byte value)
        {
            return unchecked((hash ^ value) * Prime);
        }
    }
}
