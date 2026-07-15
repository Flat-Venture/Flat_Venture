using System.Collections.Generic;
using FlatVenture.Enums;
using FlatVenture.Inventory;
using FlatVenture.ItemData;
using FlatVenture.SaveLoad;
using UnityEngine;

namespace FlatVenture.Reward
{
    // 던전 클리어 후 포탈이 생성된 다음 표시되는 임시 보상 선택 UI입니다.
    // 보상 선택과 넘기기는 저장하지 않으며, 다음 저장 지점은 노드 선택/포탈/사망/포기입니다.
    public sealed class DungeonRewardSelectionBehaviour : MonoBehaviour
    {
        private const string InventoryFullMessage = "인벤토리가 가득 찼습니다. 아이템을 버린 뒤 다시 선택하세요.";
        private const int RewardChoiceCount = 3;
        private const float CardWidth = 220f;
        private const float CardHeight = 290f;
        private const float CardGap = 18f;

        private static DungeonRewardSelectionBehaviour instance;

        [SerializeField] private GameDataLoaderBehaviour dataLoader;
        [SerializeField] private InventoryRuntimeBehaviour inventoryRuntime;
        [SerializeField] private InventoryDebugPanelBehaviour inventoryPanel;

        private readonly List<ItemRecord> rewards = new List<ItemRecord>();
        private bool isOpen;
        private bool isInventoryMode;
        private string statusMessage;
        private float previousTimeScale = 1f;
        private GUIStyle titleStyle;
        private GUIStyle headerStyle;
        private GUIStyle bodyStyle;
        private GUIStyle smallStyle;

        public static bool IsOpen
        {
            get { return instance != null && instance.isOpen; }
        }

        // 포탈 생성 이후 호출합니다. 데이터/인벤토리 객체가 없으면 팀원 단독 테스트를 막지 않도록 조용히 건너뜁니다.
        public static void ShowRewards(RoomType roomType, int nodeId)
        {
            var behaviour = EnsureInstance();
            behaviour.Open(roomType, nodeId);
        }

        private static DungeonRewardSelectionBehaviour EnsureInstance()
        {
            if (instance != null)
            {
                return instance;
            }

            instance = FindFirstObjectByType<DungeonRewardSelectionBehaviour>();
            if (instance != null)
            {
                return instance;
            }

            var gameObject = new GameObject("Dungeon Reward Selection");
            instance = gameObject.AddComponent<DungeonRewardSelectionBehaviour>();
            return instance;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
        }

        private void Open(RoomType roomType, int nodeId)
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

            if (inventoryPanel != null)
            {
                inventoryPanel.HidePanel();
            }
        }

        private void OnGUI()
        {
            if (!isOpen || PauseMenuBehaviour.IsAnyOpen)
            {
                return;
            }

            DrawInputBlocker();
            DrawRootPanel();
        }

        private void DrawInputBlocker()
        {
            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none);
        }

        private void DrawRootPanel()
        {
            if (isInventoryMode)
            {
                DrawInventoryModeOverlay();
                return;
            }

            EnsureStyles();

            var panelRect = GetPanelRect();

            GUI.Box(panelRect, GUIContent.none);
            GUI.Label(new Rect(panelRect.x, panelRect.y + 10f, panelRect.width, 24f), "보상 선택", titleStyle);

            if (!string.IsNullOrEmpty(statusMessage))
            {
                GUI.Label(new Rect(panelRect.x + 20f, panelRect.y + 36f, panelRect.width - 40f, 24f), statusMessage, bodyStyle);
            }

            DrawRewardCards(new Rect(panelRect.x + 30f, panelRect.y + 62f, panelRect.width - 60f, CardHeight));
            DrawBottomButtons(GetSharedButtonRect());
        }

        private void DrawInventoryModeOverlay()
        {
            EnsureStyles();

            var noticeRect = new Rect((Screen.width - 520f) * 0.5f, 34f, 520f, 44f);
            GUI.Box(noticeRect, GUIContent.none);
            GUI.Label(new Rect(noticeRect.x + 12f, noticeRect.y + 12f, noticeRect.width - 24f, 22f), "인벤토리에서 버릴 아이템을 정리한 뒤 보상 UI 확인 버튼을 눌러 돌아가세요.", bodyStyle);

            var buttonRect = GetSharedButtonRect();
            GUI.Box(new Rect(buttonRect.x - 18f, buttonRect.y - 8f, buttonRect.width + 36f, buttonRect.height + 16f), GUIContent.none);
            DrawBottomButtons(buttonRect);
        }

        private void DrawRewardCards(Rect areaRect)
        {
            var totalWidth = rewards.Count * CardWidth + Mathf.Max(0, rewards.Count - 1) * CardGap;
            var startX = areaRect.x + (areaRect.width - totalWidth) * 0.5f;

            for (int i = 0; i < rewards.Count; i++)
            {
                var cardRect = new Rect(startX + i * (CardWidth + CardGap), areaRect.y, CardWidth, CardHeight);
                DrawRewardCard(rewards[i], cardRect);
            }
        }

        private void DrawRewardCard(ItemRecord reward, Rect cardRect)
        {
            bool isHover = cardRect.Contains(Event.current.mousePosition);
            var drawRect = isHover ? ExpandRect(cardRect, 6f) : cardRect;

            GUI.Box(drawRect, GUIContent.none);
            GUI.Label(new Rect(drawRect.x + 10f, drawRect.y + 10f, drawRect.width - 20f, 22f), GetItemDisplayName(reward), headerStyle);
            GUI.Label(new Rect(drawRect.x + 10f, drawRect.y + 32f, drawRect.width - 20f, 18f), "등급: " + GetRarityDisplayName(reward.definition.rarityId), smallStyle);

            var icon = ItemIconResolver.ResolveIcon(reward.definition.itemId, null);
            DrawSprite(new Rect(drawRect.x + 10f, drawRect.y + 58f, 72f, 72f), icon);
            GUI.Label(new Rect(drawRect.x + 92f, drawRect.y + 58f, drawRect.width - 102f, 72f), BuildElementText(reward), smallStyle);
            GUI.Label(new Rect(drawRect.x + 10f, drawRect.y + 145f, drawRect.width - 20f, 20f), "효과: " + reward.effects.Count + "개", smallStyle);
            GUI.Label(new Rect(drawRect.x + 10f, drawRect.y + 168f, drawRect.width - 20f, 20f), "스탯: " + reward.stats.Count + "개", smallStyle);
            GUI.Label(new Rect(drawRect.x + 10f, drawRect.yMax - 28f, drawRect.width - 20f, 18f), "클릭하여 선택", smallStyle);

            if (GUI.Button(drawRect, GUIContent.none, GUIStyle.none))
            {
                SelectReward(reward);
            }
        }

        private void DrawBottomButtons(Rect rect)
        {
            var inventoryRect = new Rect(rect.x + (rect.width - 320f) * 0.5f, rect.y, 150f, 36f);
            var skipRect = new Rect(inventoryRect.xMax + 20f, rect.y, 150f, 36f);
            string inventoryButtonText = isInventoryMode ? "보상 UI 확인" : "인벤토리 확인";
            if (GUI.Button(inventoryRect, inventoryButtonText))
            {
                ToggleInventoryMode();
            }

            if (GUI.Button(skipRect, "넘기기"))
            {
                CloseRewardUI();
            }
        }

        private void SelectReward(ItemRecord reward)
        {
            InventoryPosition position;
            string message;
            if (!inventoryRuntime.TryGrantItem(reward, out position, out message))
            {
                statusMessage = IsInventoryFull() ? InventoryFullMessage : message;
                return;
            }

            statusMessage = message;
            CloseRewardUI();
        }

        private void ToggleInventoryMode()
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

        private void CloseRewardUI()
        {
            if (inventoryPanel != null)
            {
                inventoryPanel.HidePanel();
            }

            isInventoryMode = false;
            isOpen = false;
            Time.timeScale = previousTimeScale;
        }

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

        private static int GetDungeonSeed()
        {
            if (SaveGameSession.HasActiveSave && SaveGameSession.CurrentSaveData != null && SaveGameSession.CurrentSaveData.dungeon != null)
            {
                return SaveGameSession.CurrentSaveData.dungeon.dungeonSeed;
            }

            return 0;
        }

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

        private static string BuildElementText(ItemRecord reward)
        {
            if (reward.elements.Count == 0)
            {
                return "속성 없음";
            }

            var parts = new List<string>();
            for (int i = 0; i < reward.elements.Count; i++)
            {
                parts.Add(GetElementDisplayName(reward.elements[i].elementId) + " +" + reward.elements[i].elementValue);
            }

            return string.Join(", ", parts.ToArray());
        }

        private static string GetItemDisplayName(ItemRecord reward)
        {
            if (reward == null || reward.definition == null)
            {
                return "알 수 없는 아이템";
            }

            return string.IsNullOrWhiteSpace(reward.definition.displayName)
                ? reward.definition.itemId
                : reward.definition.displayName;
        }

        private static string GetRarityDisplayName(string rarityId)
        {
            var catalog = GetCatalog();
            RarityDefinition rarity;
            if (catalog != null && catalog.rarities.TryGetValue(rarityId, out rarity) && !string.IsNullOrWhiteSpace(rarity.displayName))
            {
                return rarity.displayName;
            }

            return string.IsNullOrWhiteSpace(rarityId) ? "-" : rarityId;
        }

        private static string GetElementDisplayName(string elementId)
        {
            var catalog = GetCatalog();
            ElementDefinition element;
            if (catalog != null && catalog.elements.TryGetValue(elementId, out element) && !string.IsNullOrWhiteSpace(element.displayName))
            {
                return element.displayName;
            }

            return string.IsNullOrWhiteSpace(elementId) ? "-" : elementId;
        }

        private static GameDataCatalog GetCatalog()
        {
            return instance != null && instance.dataLoader != null ? instance.dataLoader.Catalog : null;
        }

        private static Rect GetPanelRect()
        {
            var panelWidth = Mathf.Min(820f, Screen.width - 80f);
            var panelHeight = Mathf.Min(455f, Screen.height - 80f);
            return new Rect((Screen.width - panelWidth) * 0.5f, (Screen.height - panelHeight) * 0.5f, panelWidth, panelHeight);
        }

        private static Rect GetSharedButtonRect()
        {
            var panelRect = GetPanelRect();
            return new Rect(panelRect.x, panelRect.yMax - 58f, panelRect.width, 44f);
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 15,
                fontStyle = FontStyle.Bold
            };

            headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };

            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                wordWrap = true
            };

            smallStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                wordWrap = true
            };
        }

        private static Rect ExpandRect(Rect rect, float amount)
        {
            return new Rect(rect.x - amount, rect.y - amount, rect.width + amount * 2f, rect.height + amount * 2f);
        }

        private static void DrawSprite(Rect rect, Sprite sprite)
        {
            if (sprite == null || sprite.texture == null)
            {
                return;
            }

            var texture = sprite.texture;
            var textureRect = sprite.textureRect;
            var texCoords = new Rect(
                textureRect.x / texture.width,
                textureRect.y / texture.height,
                textureRect.width / texture.width,
                textureRect.height / texture.height);

            GUI.DrawTextureWithTexCoords(rect, texture, texCoords, true);
        }
    }
}
