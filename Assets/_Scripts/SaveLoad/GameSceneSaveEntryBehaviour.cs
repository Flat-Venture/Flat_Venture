using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace FlatVenture.SaveLoad
{
    // 게임 씬 입장 시 타이틀에서 선택한 세이브 데이터를 받는 진입점입니다.
    // 이 컴포넌트 자체는 씬 오브젝트라서 씬이 바뀌면 사라집니다.
    // 실제로 마을/던전 씬 전환 사이에서 세이브 데이터를 들고 있는 것은 SaveGameSession입니다.
    // 나중에 게임 매니저가 생기면 여기서 받은 SaveData를 각 시스템에 전달하면 됩니다.
    public sealed class GameSceneSaveEntryBehaviour : MonoBehaviour
    {
        [SerializeField] private bool logOnStart = true;
        [SerializeField] private bool warnIfNoActiveSave = true;
        [SerializeField] private bool enableDebugSaveKeys = true;
        [SerializeField] private Key saveKey = Key.F5;
        [SerializeField] private Key printKey = Key.F9;
        [SerializeField] private int debugGoldAddAmount = 10;

        // 게임 씬의 다른 시스템이 세이브 데이터를 받을 수 있게 Inspector나 코드에서 연결합니다.
        // Start에서 SaveGameSession.CurrentSaveData를 읽은 뒤 Invoke합니다.
        public UnityEvent<SaveData> onSaveDataReady = new UnityEvent<SaveData>();

        public SaveData CurrentSaveData { get; private set; }
        public int CurrentSlotIndex { get; private set; }
        public SaveGameSession.StartReason StartReason { get; private set; }

        // 게임 씬이 시작되면 TitleMenuBehaviour가 SaveGameSession에 넣어둔 세이브 세션을 읽습니다.
        private void Start()
        {
            if (!SaveGameSession.HasActiveSave)
            {
                if (warnIfNoActiveSave)
                    Debug.LogWarning("[GameSceneSaveEntry] 활성 세이브 없이 게임 씬에 진입했습니다.");

                return;
            }

            CurrentSaveData = SaveGameSession.CurrentSaveData;
            CurrentSlotIndex = SaveGameSession.CurrentSlotIndex;
            StartReason = SaveGameSession.CurrentStartReason;

            if (logOnStart)
                Debug.Log(BuildEntryLog());

            onSaveDataReady?.Invoke(CurrentSaveData);
        }

        // 임시 테스트용 저장/출력 키를 처리합니다.
        private void Update()
        {
            if (!enableDebugSaveKeys || Keyboard.current == null)
                return;

            if (Keyboard.current[saveKey].wasPressedThisFrame)
                SaveCurrentSessionWithDebugProgress();

            if (Keyboard.current[printKey].wasPressedThisFrame && CurrentSaveData != null)
            {
                SaveGameSession.SyncPlayTime();
                Debug.Log(BuildEntryLog());
            }
        }

        // 현재 세션 데이터를 파일에 저장합니다.
        public void SaveCurrentSession()
        {
            if (CurrentSaveData == null)
            {
                Debug.LogWarning("[GameSceneSaveEntry] 저장할 세이브 데이터가 없습니다.");
                return;
            }

            SaveLoadService.Save(CurrentSlotIndex, CurrentSaveData);
            Debug.Log("[GameSceneSaveEntry] 슬롯 " + CurrentSlotIndex + " 저장 완료");
        }

        // 테스트 확인용으로 골드를 조금 바꾼 뒤 저장합니다.
        public void SaveCurrentSessionWithDebugProgress()
        {
            if (CurrentSaveData == null)
            {
                Debug.LogWarning("[GameSceneSaveEntry] 저장할 세이브 데이터가 없습니다.");
                return;
            }

            CurrentSaveData.user.gold += debugGoldAddAmount;
            SaveCurrentSession();
            Debug.Log("[GameSceneSaveEntry] 테스트 진행값 반영: +" + debugGoldAddAmount + "골드");
        }

        // 게임 씬 입장 로그를 만듭니다.
        private string BuildEntryLog()
        {
            var user = CurrentSaveData.user;
            var dungeon = CurrentSaveData.dungeon;
            return "[GameSceneSaveEntry] 세이브 데이터 수신"
                + "\n시작 방식: " + StartReason
                + "\n슬롯: " + CurrentSlotIndex
                + "\n프로필: " + user.profileName
                + "\n플레이 시간: " + CurrentSaveData.playTimeSeconds.ToString("0.##") + "초"
                + "\n레벨: " + user.userLevel
                + "\n선택 캐릭터: " + user.selectedCharacterId
                + "\n던전 여부: " + dungeon.isInDungeon
                + "\n던전 상태: " + dungeon.dungeonState;
        }
    }
}
