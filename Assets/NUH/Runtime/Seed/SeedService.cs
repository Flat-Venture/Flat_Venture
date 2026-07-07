using System;
using System.Collections.Generic;

namespace FlatVenture.NUH.Seed
{
    /// <summary>
    /// 던전 입장 시 확정한 런 시드로 이름별 독립 난수 스트림을 제공합니다.
    /// 인스턴스를 새로 만들기 전까지 런 시드는 변경되지 않습니다.
    /// </summary>
    public sealed class SeedService : ISeedService
    {
        public const int SeedAlgorithmVersion = 1;

        private readonly Dictionary<string, RandomStream> streams =
            new Dictionary<string, RandomStream>(StringComparer.Ordinal);

        public int RunSeed { get; private set; }
        public string FormattedRunSeed { get { return SeedValue.Format(RunSeed); } }

        public SeedService(int runSeed)
        {
            SeedValue.Validate(runSeed);
            RunSeed = runSeed;
        }

        public IRandomStream GetStream(string streamName)
        {
            if (string.IsNullOrWhiteSpace(streamName))
                throw new ArgumentException("스트림 이름은 비어 있을 수 없습니다.", nameof(streamName));

            RandomStream stream;
            if (streams.TryGetValue(streamName, out stream))
                return stream;

            uint derivedSeed = StableSeedHash.Derive(RunSeed, streamName);
            stream = new RandomStream(streamName, derivedSeed, RunSeed);
            streams.Add(streamName, stream);
            return stream;
        }

        public IReadOnlyList<RandomStreamState> CaptureAllStreamStates()
        {
            List<string> names = new List<string>(streams.Keys);
            names.Sort(StringComparer.Ordinal);

            List<RandomStreamState> states = new List<RandomStreamState>(names.Count);
            for (int i = 0; i < names.Count; i++)
                states.Add(streams[names[i]].CaptureState());

            return states;
        }

        public void RestoreStreamStates(IReadOnlyList<RandomStreamState> savedStates)
        {
            if (savedStates == null)
                throw new ArgumentNullException(nameof(savedStates));

            HashSet<string> restoredNames = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < savedStates.Count; i++)
            {
                RandomStreamState savedState = savedStates[i];
                if (savedState == null)
                    throw new ArgumentException("스트림 상태 목록에 null이 포함될 수 없습니다.", nameof(savedStates));

                if (!restoredNames.Add(savedState.StreamName))
                    throw new ArgumentException("같은 이름의 스트림 상태가 중복되었습니다.", nameof(savedStates));

                IRandomStream stream = GetStream(savedState.StreamName);
                stream.RestoreState(savedState);
            }
        }
    }
}
