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
        // 복원 호환성을 확인할 알고리즘 버전입니다.
        public int AlgorithmVersion;
        // 이 상태가 생성된 런의 6자리 정수 시드입니다.
        public int RunSeed;
        // 다른 콘텐츠 스트림 상태와 섞이지 않게 하는 이름입니다.
        public string StreamName;
        // MT19937의 624개 내부 상태 복사본입니다.
        public uint[] GeneratorState;
        // GeneratorState에서 다음에 소비할 위치입니다.
        public int GeneratorIndex;
        // 디버깅과 재현 추적에 사용할 누적 호출 횟수입니다.
        public ulong CallCount;

        /// <summary>배열까지 깊은 복사해 원본과 독립적인 상태 DTO를 만듭니다.</summary>
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
