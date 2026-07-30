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
            GUI.Label(new Rect(drawRect.x + 10f, drawRect.y + 145f, drawRect.width - 20f, 92f), BuildStatText(reward), smallStyle);
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

        // 보상 카드에 표시할 스탯 상세 문자열을 만듭니다.
        private string BuildStatText(ItemRecord reward)
        {
            if (reward == null || reward.stats.Count == 0)
            {
                return "스탯 없음";
            }

            var parts = new List<string>();
            for (int i = 0; i < reward.stats.Count; i++)
            {
                parts.Add("- " + BuildSingleStatText(reward.stats[i]));
            }

            return string.Join("\n", parts.ToArray());
        }

        // 스탯 하나를 표시명, 수치, 조건 정보가 포함된 한 줄 문자열로 변환합니다.
        private string BuildSingleStatText(ItemStatModifier stat)
        {
            string statName = GetStatDisplayName(stat.statId);
            string valueText = FormatStatValue(stat);
            string conditionText = GetConditionSuffix(stat.conditionId);
            return statName + " " + valueText + conditionText;
        }

        // 스탯 id를 사람이 읽기 쉬운 표시명으로 변환합니다.
        private string GetStatDisplayName(string statId)
        {
            StatDefinition definition;
            if (controller.Catalog != null && controller.Catalog.stats.TryGetValue(statId, out definition) && !string.IsNullOrWhiteSpace(definition.displayName))
            {
                return definition.displayName;
            }

            return GetFallbackStatDisplayName(statId);
        }

        // 스탯 수치를 operation과 value_type에 맞춰 표시합니다.
        private string FormatStatValue(ItemStatModifier stat)
        {
            StatDefinition definition = null;
            if (controller.Catalog != null)
            {
                controller.Catalog.stats.TryGetValue(stat.statId, out definition);
            }

            if (string.Equals(stat.operation, "multiply", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(stat.operation, "multiplier", System.StringComparison.OrdinalIgnoreCase))
            {
                return "x" + stat.value.ToString("0.##");
            }

            string sign = stat.value > 0f ? "+" : string.Empty;
            string suffix = IsPercentStat(stat.statId, definition) ? "%" : string.Empty;
            return sign + stat.value.ToString("0.##") + suffix;
        }

        // 카탈로그 조회가 실패해도 임시 보상 UI에서는 사람이 읽을 수 있는 스탯명을 보여줍니다.
        private static string GetFallbackStatDisplayName(string statId)
        {
            if (string.IsNullOrWhiteSpace(statId))
            {
                return "-";
            }

            switch (statId)
            {
                case "attack": return "공격력";
                case "skill_damage_percent": return "스킬 피해";
                case "damage_over_time_percent": return "지속 피해";
                case "basic_attack_damage_percent": return "기본 공격 피해";
                case "attack_speed_percent": return "공격 속도";
                case "move_speed_percent": return "이동 속도";
                case "max_hp": return "최대 HP";
                case "damage_taken_percent": return "받는 피해";
                case "shield": return "보호막";
                case "defense": return "방어력";
                case "evasion": return "회피율";
                case "cooldown_reduction_percent": return "쿨타임 감소";
                case "proc_chance_percent": return "발동 확률";
                case "crit_chance_percent": return "치명타 확률";
                case "crit_damage_percent": return "치명타 피해";
                case "exp_gain_percent": return "경험치 획득";
                case "gold_gain_percent": return "골드 획득";
                case "projectile_count": return "투사체 개수";
                case "projectile_damage_percent": return "투사체 피해";
                case "projectile_speed_percent": return "투사체 속도";
                case "projectile_size_percent": return "투사체 크기";
                case "area_percent": return "스킬/공격 범위";
                case "casting_speed_percent": return "시전 속도";
                case "dash_cooldown_reduction_percent": return "대시 쿨타임 감소";
                case "dash_count": return "대시 횟수";
                case "dash_distance_percent": return "대시 거리";
                case "life_steal_percent": return "피흡";
                case "toughness": return "강인함";
                case "luck": return "행운";
                case "reroll_cost_percent": return "리롤 비용";
                case "all_damage_percent": return "모든 피해";
                case "final_damage_percent": return "최종 피해";
                case "final_damage_multiplier": return "최종 피해 배율";
                case "fire_power": return "불 속성 계수";
                case "water_power": return "물 속성 계수";
                case "nature_power": return "풀 속성 계수";
                case "earth_power": return "땅 속성 계수";
                case "lightning_power": return "번개 속성 계수";
                case "poison_power": return "독 속성 계수";
                case "dark_power": return "어둠 속성 계수";
                case "neutral_power": return "무 속성 계수";
                case "curse_power": return "저주 속성 계수";
                default: return statId;
            }
        }

        // 퍼센트형 스탯인지 확인해 수치 뒤에 %를 붙일지 결정합니다.
        private static bool IsPercentStat(string statId, StatDefinition definition)
        {
            if (definition != null)
            {
                return string.Equals(definition.valueType, "percent", System.StringComparison.OrdinalIgnoreCase);
            }

            return !string.IsNullOrWhiteSpace(statId) && statId.EndsWith("_percent", System.StringComparison.OrdinalIgnoreCase);
        }

        // 조건부 스탯이면 조건 설명을 괄호로 붙입니다.
        private string GetConditionSuffix(string conditionId)
        {
            if (string.IsNullOrWhiteSpace(conditionId))
            {
                return string.Empty;
            }

            ConditionDefinition condition;
            if (controller.Catalog != null && controller.Catalog.conditions.TryGetValue(conditionId, out condition) && !string.IsNullOrWhiteSpace(condition.description))
            {
                return " (" + condition.description + ")";
            }

            return " (" + conditionId + ")";
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
