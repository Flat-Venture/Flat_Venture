using System.Collections.Generic;
using FlatVenture.Enums;
using FlatVenture.Inventory;
using FlatVenture.ItemData;
using FlatVenture.SaveLoad;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FlatVenture.Reward
{
    // 던전 클리어 보상 선택의 상태와 기능을 담당합니다.
    // 정식 Canvas UI에서는 이 컨트롤러의 public 메소드를 버튼에 연결하면 됩니다.
    public class DungeonRewardSelectionController : MonoBehaviour
    {
        private const string InventoryFullMessage = "인벤토리가 가득 찼습니다. 아이템을 버린 뒤 다시 선택하세요.";
        private const int RewardChoiceCount = 3;

        private static DungeonRewardSelectionController instance;

        [SerializeField] private GameDataLoaderBehaviour dataLoader;
        [SerializeField] private InventoryRuntimeBehaviour inventoryRuntime;
        [SerializeField] private InventoryDebugPanelBehaviour inventoryPanel;
        [SerializeField] private Key inventoryToggleKey = Key.I;

        private readonly List<ItemRecord> rewards = new List<ItemRecord>();
        private bool isOpen;
        private bool isInventoryMode;
        private string statusMessage;
        private float previousTimeScale = 1f;

        public static bool IsOpen
        {
            get { return instance != null && instance.isOpen; }
        }

        public IReadOnlyList<ItemRecord> Rewards
        {
            get { return rewards; }
        }

        public bool IsOpenInstance
        {
            get { return isOpen; }
        }

        public bool IsInventoryMode
        {
            get { return isInventoryMode; }
        }

        public string StatusMessage
        {
            get { return statusMessage; }
        }

        public GameDataCatalog Catalog
        {
            get { return dataLoader != null ? dataLoader.Catalog : null; }
        }

        public ItemAssetDatabaseSO ItemAssetDatabase
        {
            get { return inventoryPanel != null ? inventoryPanel.ItemAssetDatabase : null; }
        }

        // 포탈 생성 이후 호출합니다. 데이터나 인벤토리 객체가 없으면 테스트를 막지 않도록 조용히 건너뜁니다.
        public static void ShowRewards(RoomType roomType, int nodeId)
        {
            var controller = EnsureInstance();
            controller.OpenRewards(roomType, nodeId);
        }

        // 씬에 보상 컨트롤러가 없으면 임시 오브젝트를 만들어 사용합니다.
        private static DungeonRewardSelectionController EnsureInstance()
        {
            if (instance != null)
            {
                EnsureDebugView(instance);
                return instance;
            }

            instance = FindFirstObjectByType<DungeonRewardSelectionController>();
            if (instance != null)
            {
                EnsureDebugView(instance);
                return instance;
            }

            var gameObject = new GameObject("Dungeon Reward Selection");
            instance = gameObject.AddComponent<DungeonRewardSelectionController>();
            EnsureDebugView(instance);
            return instance;
        }

        // 임시 OnGUI 화면이 없으면 자동으로 붙입니다.
        private static void EnsureDebugView(DungeonRewardSelectionController controller)
        {
            if (controller != null && controller.GetComponent<DungeonRewardSelectionDebugView>() == null)
            {
                controller.gameObject.AddComponent<DungeonRewardSelectionDebugView>();
            }
        }

        // 씬에 배치된 보상 컨트롤러 인스턴스를 전역 참조로 등록합니다.
        protected virtual void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
        }

        // 보상 UI가 열려 있을 때 I키로 보상 화면과 인벤토리 화면을 전환합니다.
        private void Update()
        {
            var keyboard = Keyboard.current;
            if (!isOpen || keyboard == null || PauseMenuBehaviour.IsAnyOpen)
            {
                return;
            }

            if (keyboard[inventoryToggleKey].wasPressedThisFrame)
            {
                ToggleInventoryMode();
            }
        }

        // 보상 후보를 생성하고 보상 선택 상태를 엽니다.
        public void OpenRewards(RoomType roomType, int nodeId)
        {
            ResolveReferences();

            if (dataLoader == null || dataLoader.Catalog == null || inventoryRuntime == null)
            {
                Debug.LogWarning("[DungeonReward] 보상 UI를 열 수 없습니다. GameDataLoaderBehaviour 또는 InventoryRuntimeBehaviour가 없습니다.");
                return;
            }

            var seed = GetDungeonSeed();
            var rewardService = new DungeonRewardService(dataLoader.Catalog);
            List<ItemRecord> createdRewards;
            string message;
            if (!rewardService.TryCreateRewards(roomType, seed, nodeId, inventoryRuntime.HasCursedItem, out createdRewards, out message))
            {
                Debug.LogWarning("[DungeonReward] 보상 후보 생성 실패: " + message);
                return;
            }

            rewards.Clear();
            for (int i = 0; i < createdRewards.Count && i < RewardChoiceCount; i++)
            {
                rewards.Add(createdRewards[i]);
            }

            if (rewards.Count == 0)
            {
                return;
            }

            previousTimeScale = Mathf.Approximately(Time.timeScale, 0f) ? 1f : Time.timeScale;
            Time.timeScale = 0f;
            statusMessage = string.Empty;
            isInventoryMode = false;
            isOpen = true;
            global::DungeonUiInputBlocker.SetBlocked(this, true);

            if (inventoryPanel != null)
            {
                inventoryPanel.HidePanel();
            }
        }

        // 보상 카드 인덱스로 선택한 아이템을 인벤토리에 넣습니다.
        public void SelectReward(int rewardIndex)
        {
            if (rewardIndex < 0 || rewardIndex >= rewards.Count)
            {
                statusMessage = "선택한 보상이 없습니다.";
                return;
            }

            SelectReward(rewards[rewardIndex]);
        }

        // 인벤토리 확인 화면과 보상 카드 화면을 전환합니다.
        public void ToggleInventoryMode()
        {
            isInventoryMode = !isInventoryMode;
            statusMessage = string.Empty;

            if (inventoryPanel == null)
            {
                ResolveReferences();
            }

            if (inventoryPanel == null)
            {
                statusMessage = "InventoryDebugPanelBehaviour를 찾지 못했습니다.";
                return;
            }

            if (isInventoryMode)
            {
                inventoryPanel.ShowPanel();
            }
            else
            {
                inventoryPanel.HidePanel();
            }
        }

        // 보상을 받지 않고 보상 선택을 끝냅니다. 추후 스킵 보상 골드, 업적, 통계 처리를 이 지점에 추가할 수 있습니다.
        public void SkipReward()
        {
            Close();
        }

        // 보상 UI를 닫고 입력 차단과 인벤토리 패널을 정리합니다.
        public void Close()
        {
            if (inventoryPanel != null)
            {
                inventoryPanel.HidePanel();
            }

            isInventoryMode = false;
            isOpen = false;
            global::DungeonUiInputBlocker.SetBlocked(this, false);
            if (!global::DungeonUiInputBlocker.BlocksGameplayInput)
            {
                Time.timeScale = previousTimeScale;
            }
        }

        // 오브젝트가 제거될 때 입력 차단 상태가 남지 않도록 정리합니다.
        private void OnDestroy()
        {
            global::DungeonUiInputBlocker.SetBlocked(this, false);
        }

        // 선택한 보상 아이템을 인벤토리에 넣습니다.
        private void SelectReward(ItemRecord reward)
        {
            InventoryPosition position;
            string message;
            if (!inventoryRuntime.TryGrantItem(reward, false, out position, out message))
            {
                statusMessage = IsInventoryFull() ? InventoryFullMessage : message;
                return;
            }

            statusMessage = message;
            Close();
        }

        // 보상 기능에 필요한 참조를 씬에서 찾습니다.
        private void ResolveReferences()
        {
            if (dataLoader == null)
            {
                dataLoader = FindFirstObjectByType<GameDataLoaderBehaviour>();
            }

            if (inventoryRuntime == null)
            {
                inventoryRuntime = FindFirstObjectByType<InventoryRuntimeBehaviour>();
            }

            if (inventoryPanel == null)
            {
                inventoryPanel = FindFirstObjectByType<InventoryDebugPanelBehaviour>();
            }
        }

        // 현재 활성 세이브에서 던전 시드를 가져옵니다.
        private static int GetDungeonSeed()
        {
            if (SaveGameSession.HasActiveSave && SaveGameSession.CurrentSaveData != null && SaveGameSession.CurrentSaveData.dungeon != null)
            {
                return SaveGameSession.CurrentSaveData.dungeon.dungeonSeed;
            }

            return 0;
        }

        // 인벤토리에 빈 슬롯이 남아 있는지 확인합니다.
        private bool IsInventoryFull()
        {
            if (inventoryRuntime == null || inventoryRuntime.Grid == null)
            {
                return false;
            }

            var slots = inventoryRuntime.Grid.Slots;
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i] != null && !slots[i].HasItem)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
