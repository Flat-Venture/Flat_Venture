using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace FlatVenture.NUH.Seed.Debugging
{
    /// <summary>
    /// 시드 생성, 직접 입력, 복사와 스트림 재현을 확인하는 테스트 전용 패널입니다.
    /// </summary>
    public sealed class SeedDebugPanel : MonoBehaviour
    {
        private const int ValuesPerRequest = 5;
        private const int MaximumOutputLength = 5000;
        private const int MaximumOutputLines = 14;

        [SerializeField] private InputField seedInput;
        [SerializeField] private InputField streamNameInput;
        [SerializeField] private Text statusOutput;
        [SerializeField] private Text streamOutput;

        private readonly StringBuilder outputBuilder = new StringBuilder();
        private SeedService seedService;
        private int outputLineCount;

        public int CurrentRunSeed
        {
            get
            {
                if (seedService == null)
                    return -1;
                return seedService.RunSeed;
            }
        }

        private void Awake()
        {
            if (seedInput != null)
            {
                seedInput.characterLimit = SeedValue.DigitCount;
                seedInput.onValidateInput = ValidateSeedCharacter;
            }

            if (streamNameInput != null && string.IsNullOrWhiteSpace(streamNameInput.text))
                streamNameInput.text = SeedStreamNames.Shop;
        }

        private void Start()
        {
            GenerateRandomSeed();
        }

        public void GenerateRandomSeed()
        {
            ApplySeed(SeedValue.Generate(), true);
            AppendSystemMessage("새 무작위 시드를 생성하고 적용했습니다.");
        }

        public void ApplyInputSeed()
        {
            if (seedInput == null)
            {
                SetError("시드 입력 UI가 연결되지 않았습니다.");
                return;
            }

            if (string.IsNullOrEmpty(seedInput.text))
            {
                GenerateRandomSeed();
                return;
            }

            int parsedSeed;
            if (!SeedValue.TryParse(seedInput.text, out parsedSeed))
            {
                SetError("시드는 숫자 6자리로 입력해야 합니다.");
                return;
            }

            ApplySeed(parsedSeed, true);
            AppendSystemMessage("입력한 시드를 적용했습니다.");
        }

        public void CopyCurrentSeed()
        {
            if (!EnsureService())
                return;

            GUIUtility.systemCopyBuffer = seedService.FormattedRunSeed;
            AppendSystemMessage("현재 시드를 클립보드에 복사했습니다.");
        }

        public void ReplayCurrentSeed()
        {
            if (!EnsureService())
                return;

            int replaySeed = seedService.RunSeed;
            ApplySeed(replaySeed, false);
            AppendSystemMessage("동일 시드의 모든 스트림을 처음 상태로 되돌렸습니다.");
            DrawNextValues();
        }

        public void DrawNextValues()
        {
            if (!EnsureService())
                return;

            string streamName = GetStreamName();
            if (string.IsNullOrEmpty(streamName))
            {
                SetError("스트림 이름을 입력해야 합니다.");
                return;
            }

            IRandomStream stream = seedService.GetStream(streamName);
            ulong firstCall = stream.CallCount + 1;
            StringBuilder line = new StringBuilder();
            line.Append('[');
            line.Append(stream.Name);
            line.Append("] #");
            line.Append(firstCall);
            line.Append("~#");
            line.Append(firstCall + (ulong)ValuesPerRequest - 1UL);
            line.Append(" : ");

            for (int i = 0; i < ValuesPerRequest; i++)
            {
                if (i > 0)
                    line.Append(", ");
                line.Append(stream.NextUInt32());
            }

            AppendOutput(line.ToString());
            RefreshStatus(stream);
            Debug.Log($"[SeedDebug] RunSeed={seedService.FormattedRunSeed}, Version={SeedService.SeedAlgorithmVersion}, {line}");
        }

        public void ClearOutput()
        {
            outputBuilder.Clear();
            outputLineCount = 0;
            RefreshOutput();
            RefreshStatus(null);
        }

        public void SelectMapStream() { SelectStream(SeedStreamNames.Map); }
        public void SelectMonsterStream() { SelectStream(SeedStreamNames.Monster); }
        public void SelectItemStream() { SelectStream(SeedStreamNames.Item); }
        public void SelectShopStream() { SelectStream(SeedStreamNames.Shop); }
        public void SelectForgeStream() { SelectStream(SeedStreamNames.Forge); }
        public void SelectEventStream() { SelectStream(SeedStreamNames.Event); }

        private char ValidateSeedCharacter(string text, int characterIndex, char addedCharacter)
        {
            if (addedCharacter >= '0' && addedCharacter <= '9')
                return addedCharacter;
            return '\0';
        }

        private void ApplySeed(int seed, bool clearOutput)
        {
            seedService = new SeedService(seed);
            if (seedInput != null)
                seedInput.SetTextWithoutNotify(seedService.FormattedRunSeed);

            if (clearOutput)
            {
                outputBuilder.Clear();
                outputLineCount = 0;
            }

            RefreshOutput();
            RefreshStatus(null);
            Debug.Log($"[SeedDebug] RunSeed 적용: {seedService.FormattedRunSeed}, Version={SeedService.SeedAlgorithmVersion}");
        }

        private void SelectStream(string streamName)
        {
            if (streamNameInput != null)
                streamNameInput.SetTextWithoutNotify(streamName);
            RefreshStatus(null);
        }

        private string GetStreamName()
        {
            if (streamNameInput == null)
                return string.Empty;

            string streamName = streamNameInput.text.Trim();
            if (!string.Equals(streamName, streamNameInput.text, System.StringComparison.Ordinal))
                streamNameInput.SetTextWithoutNotify(streamName);
            return streamName;
        }

        private bool EnsureService()
        {
            if (seedService != null)
                return true;

            SetError("먼저 시드를 생성하거나 적용해야 합니다.");
            return false;
        }

        private void AppendSystemMessage(string message)
        {
            AppendOutput($"- {message}");
            RefreshStatus(null);
        }

        private void AppendOutput(string line)
        {
            if (outputBuilder.Length > MaximumOutputLength)
            {
                outputBuilder.Clear();
                outputLineCount = 0;
            }

            if (outputLineCount >= MaximumOutputLines)
            {
                int firstLineEnd = IndexOfFirstLineEnd();
                if (firstLineEnd >= 0)
                {
                    outputBuilder.Remove(0, firstLineEnd + 1);
                    outputLineCount--;
                }
                else
                {
                    outputBuilder.Clear();
                    outputLineCount = 0;
                }
            }

            outputBuilder.AppendLine(line);
            outputLineCount++;
            RefreshOutput();
        }

        private int IndexOfFirstLineEnd()
        {
            for (int i = 0; i < outputBuilder.Length; i++)
            {
                if (outputBuilder[i] == '\n')
                    return i;
            }

            return -1;
        }

        private void RefreshOutput()
        {
            if (streamOutput != null)
                streamOutput.text = outputBuilder.ToString();
        }

        private void RefreshStatus(IRandomStream stream)
        {
            if (statusOutput == null)
                return;

            if (seedService == null)
            {
                statusOutput.text = "RunSeed가 아직 적용되지 않았습니다.";
                return;
            }

            string selectedName = GetStreamName();
            if (stream == null && !string.IsNullOrEmpty(selectedName))
                stream = seedService.GetStream(selectedName);

            string callText = stream == null ? "스트림 미선택" : $"호출 {stream.CallCount}회";
            statusOutput.text =
                $"RunSeed {seedService.FormattedRunSeed} | Algorithm V{SeedService.SeedAlgorithmVersion}\n" +
                $"선택 스트림: {selectedName} | {callText}";
        }

        private void SetError(string message)
        {
            if (statusOutput != null)
                statusOutput.text = $"오류: {message}";
            Debug.LogWarning($"[SeedDebug] {message}");
        }
    }
}
