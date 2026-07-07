using System;

namespace FlatVenture.NUH.Seed
{
    /// <summary>
    /// 난수 스트림을 정확한 소비 위치에서 이어가기 위한 저장용 상태입니다.
    /// 세이브 파일에 기록하는 작업은 세이브 담당 시스템에서 수행합니다.
    /// </summary>
    [Serializable]
    public sealed class RandomStreamState
    {
        public int AlgorithmVersion;
        public int RunSeed;
        public string StreamName;
        public uint[] GeneratorState;
        public int GeneratorIndex;
        public ulong CallCount;

        public RandomStreamState Clone()
        {
            uint[] stateCopy = null;
            if (GeneratorState != null)
            {
                stateCopy = new uint[GeneratorState.Length];
                Array.Copy(GeneratorState, stateCopy, GeneratorState.Length);
            }

            return new RandomStreamState
            {
                AlgorithmVersion = AlgorithmVersion,
                RunSeed = RunSeed,
                StreamName = StreamName,
                GeneratorState = stateCopy,
                GeneratorIndex = GeneratorIndex,
                CallCount = CallCount
            };
        }
    }
}
