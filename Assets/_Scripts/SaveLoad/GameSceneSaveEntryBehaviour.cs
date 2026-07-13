using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using FlatVenture.Inventory;

namespace FlatVenture.SaveLoad
{
    // 게임 씬 입장 시 타이틀에서 선택한 세이브 데이터를 받는 진입점입니다.
    // 실제로 마을/던전 씬 전환 사이에서 세이브 데이터를 들고 있는 것은 SaveGameSession입니다.
    public sealed class GameSceneSaveEntryBehaviour : MonoBehaviour
    {
        [SerializeField] private bool logOnStart = true;
        [SerializeField] private bool warnIfNoActiveSave = true;
        [SerializeField] private bool enableDebugSaveKeys = true;
        [SerializeField] private Key saveKey = Key.F5;
        [SerializeField] private Key printKey = Key.F9;
        [SerializeField] private int debugDungeonGoldAddAmount = 10;

        // Start에서 SaveGameSession.CurrentSaveData를 읽은 뒤 호출됩니다.
        // 다른 임시 테스트 컴포넌트가 현재 세이브 데이터를 받아야 할 때 Inspector나 코드에서 연결합니다.
        public UnityEvent<SaveData> onSaveDataReady = new UnityEvent<SaveData>();

        public SaveData CurrentSaveData { get; private set; }
        public int CurrentSlotIndex { get; private set; }
        public SaveGameSession.StartReason StartReason { get; private set; }

        // 씬이 시작되면 현재 활성화된 세이브 세션을 읽습니다.
        private void Start()
        {
            if (!SaveGameSession.HasActiveSave)
            {
                if (warnIfNoActiveSave)
                    Debug.LogWarning("[GameSceneSaveEntry] Active save does not exist.");

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
                Debug.LogWarning("[GameSceneSaveEntry] Save data does not exist.");
                return;
            }

            CaptureInventoryIfExists(CurrentSaveData);
            SaveLoadService.Save(CurrentSlotIndex, CurrentSaveData);
            Debug.Log("[GameSceneSaveEntry] Save complete. Slot " + CurrentSlotIndex);
        }

        // 씬에 인벤토리 런타임이 있으면 저장 직전에 현재 인벤토리 상태를 SaveData에 반영합니다.
        private static void CaptureInventoryIfExists(SaveData saveData)
        {
            var inventoryRuntime = FindFirstObjectByType<InventoryRuntimeBehaviour>();
            if (inventoryRuntime != null)
            {
                inventoryRuntime.CaptureToSaveData(saveData);
            }
        }

        // 테스트 확인용으로 던전 골드를 조금 올린 뒤 저장합니다.
        public void SaveCurrentSessionWithDebugProgress()
        {
            if (CurrentSaveData == null)
            {
                Debug.LogWarning("[GameSceneSaveEntry] Save data does not exist.");
                return;
            }

            var inventoryRuntime = FindFirstObjectByType<InventoryRuntimeBehaviour>();
            if (inventoryRuntime != null)
                inventoryRuntime.AddDebugGold(debugDungeonGoldAddAmount);
            else
                CurrentSaveData.dungeon.gold += debugDungeonGoldAddAmount;

            SaveCurrentSession();
            Debug.Log("[GameSceneSaveEntry] Debug dungeon gold added: +" + debugDungeonGoldAddAmount);
        }

        // 게임 씬 입장 로그를 만듭니다.
        private string BuildEntryLog()
        {
            var user = CurrentSaveData.user;
            var dungeon = CurrentSaveData.dungeon;
            return "[GameSceneSaveEntry] Save data received"
                + "\nStart Reason: " + StartReason
                + "\nSlot: " + CurrentSlotIndex
                + "\nProfile: " + user.profileName
                + "\nPlay Time: " + CurrentSaveData.playTimeSeconds.ToString("0.##") + " sec"
                + "\nUser Level: " + user.userLevel
                + "\nJewel: " + user.jewel
                + "\nSelected Character: " + user.selectedCharacterId
                + "\nIn Dungeon: " + dungeon.isInDungeon
                + "\nDungeon Player Level: " + dungeon.playerLevel
                + "\nDungeon Player Exp: " + dungeon.playerExp
                + "\nDungeon Gold: " + dungeon.gold
                + "\nDungeon State: " + dungeon.dungeonState;
        }
    }
}
