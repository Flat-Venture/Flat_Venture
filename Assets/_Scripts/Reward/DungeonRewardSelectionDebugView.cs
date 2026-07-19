using System.Collections.Generic;
using FlatVenture.ItemData;
using FlatVenture.SaveLoad;
using UnityEngine;

namespace FlatVenture.Reward
{
    // 던전 보상 선택을 테스트하기 위한 임시 OnGUI 화면입니다.
    // 정식 Canvas UI가 생기면 이 스크립트를 제거하고 Controller만 연결하면 됩니다.
    public sealed class DungeonRewardSelectionDebugView : MonoBehaviour
    {
        private const float CardWidth = 220f;
        private const float CardHeight = 290f;
        private const float CardGap = 18f;

        [SerializeField] private DungeonRewardSelectionController controller;

        private GUIStyle titleStyle;
        private GUIStyle headerStyle;
        private GUIStyle bodyStyle;
        private GUIStyle smallStyle;

        // 같은 오브젝트에 붙은 보상 컨트롤러를 자동으로 연결합니다.
        private void Awake()
        {
            if (controller == null)
            {
                controller = GetComponent<DungeonRewardSelectionController>();
            }
        }

        // 보상 UI가 열려 있으면 입력 차단 레이어와 보상 패널을 그립니다.
        private void OnGUI()
        {
            if (controller == null || !controller.IsOpenInstance || PauseMenuBehaviour.IsAnyOpen)
            {
                return;
            }

            DrawInputBlocker();
            DrawRootPanel();
        }

        // 보상 UI 뒤의 다른 OnGUI 버튼이 클릭되지 않도록 전체 화면 박스를 그립니다.
        private void DrawInputBlocker()
        {
            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none);
        }

        // 현재 모드에 맞춰 보상 카드 화면 또는 인벤토리 확인 화면을 그립니다.
        private void DrawRootPanel()
        {
            if (controller.IsInventoryMode)
            {
                DrawInventoryModeOverlay();
                return;
            }

            EnsureStyles();

            var panelRect = GetPanelRect();
            GUI.Box(panelRect, GUIContent.none);
            GUI.Label(new Rect(panelRect.x, panelRect.y + 10f, panelRect.width, 24f), "보상 선택", titleStyle);

            if (!string.IsNullOrEmpty(controller.StatusMessage))
            {
                GUI.Label(new Rect(panelRect.x + 20f, panelRect.y + 36f, panelRect.width - 40f, 24f), controller.StatusMessage, bodyStyle);
            }

            DrawRewardCards(new Rect(panelRect.x + 30f, panelRect.y + 62f, panelRect.width - 60f, CardHeight));
            DrawBottomButtons(GetSharedButtonRect());
        }

        // 인벤토리 확인 중에도 보상 UI 확인/넘기기 버튼은 같은 위치에 고정합니다.
        private void DrawInventoryModeOverlay()
        {
            EnsureStyles();

            var noticeRect = new Rect((Screen.width - 520f) * 0.5f, 34f, 520f, 44f);
            GUI.Box(noticeRect, GUIContent.none);
            GUI.Label(new Rect(noticeRect.x + 12f, noticeRect.y + 12f, noticeRect.width - 24f, 22f), "인벤토리에서 버릴 아이템을 정리한 뒤 보상 UI 확인 버튼이나 I키로 돌아가세요.", bodyStyle);

            var buttonRect = GetSharedButtonRect();
            GUI.Box(new Rect(buttonRect.x - 18f, buttonRect.y - 8f, buttonRect.width + 36f, buttonRect.height + 16f), GUIContent.none);
            DrawBottomButtons(buttonRect);
        }

        // 생성된 보상 후보들을 카드 형태로 가운데 정렬해 그립니다.
        private void DrawRewardCards(Rect areaRect)
        {
            var rewards = controller.Rewards;
            var totalWidth = rewards.Count * CardWidth + Mathf.Max(0, rewards.Count - 1) * CardGap;
            var startX = areaRect.x + (areaRect.width - totalWidth) * 0.5f;

            for (int i = 0; i < rewards.Count; i++)
            {
                var cardRect = new Rect(startX + i * (CardWidth + CardGap), areaRect.y, CardWidth, CardHeight);
                DrawRewardCard(i, rewards[i], cardRect);
            }
        }

        // 보상 카드 하나의 정보와 선택 버튼 영역을 그립니다.
        private void DrawRewardCard(int rewardIndex, ItemRecord reward, Rect cardRect)
        {
            bool isHover = cardRect.Contains(Event.current.mousePosition);
            var drawRect = isHover ? ExpandRect(cardRect, 6f) : cardRect;

            GUI.Box(drawRect, GUIContent.none);
            GUI.Label(new Rect(drawRect.x + 10f, drawRect.y + 10f, drawRect.width - 20f, 22f), GetItemDisplayName(reward), headerStyle);
            GUI.Label(new Rect(drawRect.x + 10f, drawRect.y + 32f, drawRect.width - 20f, 18f), "등급: " + GetRarityDisplayName(reward.definition.rarityId), smallStyle);

            var icon = ItemIconResolver.ResolveIcon(reward.definition.itemId, controller.ItemAssetDatabase);
            DrawSprite(new Rect(drawRect.x + 10f, drawRect.y + 58f, 72f, 72f), icon);
            GUI.Label(new Rect(drawRect.x + 92f, drawRect.y + 58f, drawRect.width - 102f, 72f), BuildElementText(reward), smallStyle);
            GUI.Label(new Rect(drawRect.x + 10f, drawRect.y + 145f, drawRect.width - 20f, 20f), "효과: " + reward.effects.Count + "개", smallStyle);
            GUI.Label(new Rect(drawRect.x + 10f, drawRect.y + 168f, drawRect.width - 20f, 20f), "스탯: " + reward.stats.Count + "개", smallStyle);
            GUI.Label(new Rect(drawRect.x + 10f, drawRect.yMax - 28f, drawRect.width - 20f, 18f), "클릭하여 선택", smallStyle);

            if (GUI.Button(drawRect, GUIContent.none, GUIStyle.none))
            {
                controller.SelectReward(rewardIndex);
            }
        }

        // 인벤토리 확인/보상 UI 확인 버튼과 넘기기 버튼을 그립니다.
        private void DrawBottomButtons(Rect rect)
        {
            var inventoryRect = new Rect(rect.x + (rect.width - 320f) * 0.5f, rect.y, 150f, 36f);
            var skipRect = new Rect(inventoryRect.xMax + 20f, rect.y, 150f, 36f);
            string inventoryButtonText = controller.IsInventoryMode ? "보상 UI 확인" : "인벤토리 확인";
            if (GUI.Button(inventoryRect, inventoryButtonText))
            {
                controller.ToggleInventoryMode();
            }

            if (GUI.Button(skipRect, "넘기기"))
            {
                controller.SkipReward();
            }
        }

        // 보상 카드에 표시할 속성 문자열을 만듭니다.
        private string BuildElementText(ItemRecord reward)
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

        // 아이템 표시명을 반환하고 없으면 item_id를 대신 사용합니다.
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

        // 희귀도 표시명을 카탈로그에서 찾아 반환합니다.
        private string GetRarityDisplayName(string rarityId)
        {
            RarityDefinition rarity;
            if (controller.Catalog != null && controller.Catalog.rarities.TryGetValue(rarityId, out rarity) && !string.IsNullOrWhiteSpace(rarity.displayName))
            {
                return rarity.displayName;
            }

            return string.IsNullOrWhiteSpace(rarityId) ? "-" : rarityId;
        }

        // 속성 표시명을 카탈로그에서 찾아 반환합니다.
        private string GetElementDisplayName(string elementId)
        {
            ElementDefinition element;
            if (controller.Catalog != null && controller.Catalog.elements.TryGetValue(elementId, out element) && !string.IsNullOrWhiteSpace(element.displayName))
            {
                return element.displayName;
            }

            return string.IsNullOrWhiteSpace(elementId) ? "-" : elementId;
        }

        // 화면 크기에 맞춰 보상 패널 영역을 계산합니다.
        private static Rect GetPanelRect()
        {
            var panelWidth = Mathf.Min(820f, Screen.width - 80f);
            var panelHeight = Mathf.Min(455f, Screen.height - 80f);
            return new Rect((Screen.width - panelWidth) * 0.5f, (Screen.height - panelHeight) * 0.5f, panelWidth, panelHeight);
        }

        // 보상 화면과 인벤토리 확인 화면이 공유하는 하단 버튼 영역을 계산합니다.
        private static Rect GetSharedButtonRect()
        {
            var panelRect = GetPanelRect();
            return new Rect(panelRect.x, panelRect.yMax - 58f, panelRect.width, 44f);
        }

        // OnGUI에서 사용할 텍스트 스타일을 한 번만 생성합니다.
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

        // 호버된 카드가 약간 커져 보이도록 Rect를 확장합니다.
        private static Rect ExpandRect(Rect rect, float amount)
        {
            return new Rect(rect.x - amount, rect.y - amount, rect.width + amount * 2f, rect.height + amount * 2f);
        }

        // 스프라이트의 아틀라스 textureRect를 반영해 OnGUI에 아이콘을 그립니다.
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
