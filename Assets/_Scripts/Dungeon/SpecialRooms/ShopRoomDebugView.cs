using FlatVenture.ItemData;
using FlatVenture.Inventory;
using UnityEngine;

// 상점 테스트용 임시 OnGUI 화면입니다. 실제 구매/판매 기능은 ShopRoomController가 담당합니다.
public sealed class ShopRoomDebugView : MonoBehaviour
{
    private const float CardWidth = 178f;
    private const float CardHeight = 295f;
    private const float CardGap = 8f;

    [SerializeField] private ShopRoomController controller;
    [SerializeField] private GameDataLoaderBehaviour dataLoader;
    [SerializeField] private InventoryDebugPanelBehaviour inventoryPanel;

    // 같은 오브젝트에 붙은 상점 컨트롤러를 자동으로 연결합니다.
    private void Awake()
    {
        if (controller == null)
        {
            controller = GetComponent<ShopRoomController>();
        }

        ResolveReferences();
    }

    // 상점 임시 UI를 화면에 그립니다.
    private void OnGUI()
    {
        if (controller == null || !controller.IsOpen || controller.InventoryRuntime == null || FlatVenture.SaveLoad.PauseMenuBehaviour.IsAnyOpen)
        {
            return;
        }

        ResolveReferences();

        var panelWidth = Mathf.Min(1040f, Screen.width - 24f);
        var panelHeight = Mathf.Min(540f, Screen.height - 24f);
        var rect = new Rect((Screen.width - panelWidth) * 0.5f, (Screen.height - panelHeight) * 0.5f, panelWidth, panelHeight);
        GUI.Box(rect, controller.IsSellMode ? "\uC544\uC774\uD15C \uD310\uB9E4" : "\uC0C1\uC810");

        if (controller.IsSellMode)
        {
            DrawSellMode(rect);
        }
        else
        {
            DrawShopMode(rect);
        }
    }

    // 상품 카드와 판매/닫기 버튼이 있는 기본 상점 화면을 그립니다.
    private void DrawShopMode(Rect rect)
    {
        GUILayout.BeginArea(new Rect(rect.x + 20f, rect.y + 34f, rect.width - 40f, rect.height - 50f));
        GUILayout.Label("\uACE8\uB4DC: " + controller.InventoryRuntime.Wallet.Gold);
        GUILayout.Space(8f);
        GUILayout.BeginHorizontal();

        for (int i = 0; i < controller.ShopItems.Count; i++)
        {
            DrawShopCard(i, controller.ShopItems[i]);
            GUILayout.Space(CardGap);
        }

        GUILayout.EndHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("\uC544\uC774\uD15C \uD310\uB9E4", GUILayout.Width(150f), GUILayout.Height(36f)))
        {
            controller.OpenSellMode();
        }

        if (GUILayout.Button("\uB2EB\uAE30", GUILayout.Width(150f), GUILayout.Height(36f)))
        {
            controller.CloseUI();
        }
        GUILayout.EndHorizontal();
        GUILayout.Label(controller.Message ?? string.Empty);
        GUILayout.EndArea();
    }

    // 상품 하나의 카드 UI를 그리고 구매 버튼을 컨트롤러에 연결합니다.
    private void DrawShopCard(int index, ItemRecord item)
    {
        GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(CardWidth), GUILayout.Height(CardHeight));
        GUILayout.Label(item.definition.displayName);
        GUILayout.Label("등급: " + GetRarityDisplayName(item.definition.rarityId));
        GUILayout.Label("가격: " + controller.GetItemPrice(item));

        GUILayout.BeginHorizontal();
        DrawItemIcon(item);
        GUILayout.Label(BuildElementText(item), GUILayout.Width(96f), GUILayout.Height(60f));
        GUILayout.EndHorizontal();

        GUILayout.Label(BuildStatText(item), GUILayout.Height(94f));
        GUILayout.FlexibleSpace();

        if (controller.IsItemSold(index))
        {
            GUILayout.Label("SOLD");
            GUI.enabled = false;
            GUILayout.Button("\uAD6C\uB9E4");
            GUI.enabled = true;
        }
        else if (GUILayout.Button("\uAD6C\uB9E4"))
        {
            controller.BuyItem(index);
        }

        GUILayout.EndVertical();
    }

    // 상점 카드에 표시할 아이템 아이콘을 그립니다.
    private void DrawItemIcon(ItemRecord item)
    {
        var sprite = ItemIconResolver.ResolveIcon(item.definition.itemId, inventoryPanel != null ? inventoryPanel.ItemAssetDatabase : null);
        var rect = GUILayoutUtility.GetRect(58f, 58f, GUILayout.Width(58f), GUILayout.Height(58f));
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

    // 상점 카드에 표시할 속성 문자열을 만듭니다.
    private string BuildElementText(ItemRecord item)
    {
        if (item.elements.Count == 0)
        {
            return "속성 없음";
        }

        var parts = new string[item.elements.Count];
        for (int i = 0; i < item.elements.Count; i++)
        {
            parts[i] = GetElementDisplayName(item.elements[i].elementId) + " +" + item.elements[i].elementValue;
        }

        return string.Join("\n", parts);
    }

    // 상점 카드에 표시할 스탯 상세 문자열을 만듭니다.
    private string BuildStatText(ItemRecord item)
    {
        if (item.stats.Count == 0)
        {
            return "스탯 없음";
        }

        var parts = new string[item.stats.Count];
        for (int i = 0; i < item.stats.Count; i++)
        {
            parts[i] = "- " + BuildSingleStatText(item.stats[i]);
        }

        return string.Join("\n", parts);
    }

    // 스탯 하나를 표시명과 수치가 포함된 한 줄 문자열로 변환합니다.
    private string BuildSingleStatText(ItemStatModifier stat)
    {
        return GetStatDisplayName(stat.statId) + " " + FormatStatValue(stat) + GetConditionSuffix(stat.conditionId);
    }

    // 판매 모드 안내와 상점으로 돌아가기 버튼을 그립니다.
    private void DrawSellMode(Rect rect)
    {
        GUILayout.BeginArea(new Rect(rect.x + 20f, rect.y + 34f, rect.width - 40f, rect.height - 50f));
        GUILayout.Label("\uACE8\uB4DC: " + controller.InventoryRuntime.Wallet.Gold);
        GUILayout.Label("\uC5F4\uB9B0 \uC778\uBCA4\uD1A0\uB9AC\uC5D0\uC11C \uD310\uB9E4\uD560 \uC544\uC774\uD15C\uC744 \uD074\uB9AD\uD558\uC138\uC694.");
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("\uC0C1\uC810\uC73C\uB85C \uB3CC\uC544\uAC00\uAE30", GUILayout.Width(180f), GUILayout.Height(36f)))
        {
            controller.ReturnToShop();
        }

        GUILayout.Label(controller.Message ?? string.Empty);
        GUILayout.EndArea();
    }

    // 상점 UI 표시용 데이터 참조를 찾습니다.
    private void ResolveReferences()
    {
        if (dataLoader == null)
        {
            dataLoader = FindFirstObjectByType<GameDataLoaderBehaviour>();
        }

        if (inventoryPanel == null)
        {
            inventoryPanel = FindFirstObjectByType<InventoryDebugPanelBehaviour>();
        }
    }

    // 희귀도 id를 표시명으로 변환합니다.
    private string GetRarityDisplayName(string rarityId)
    {
        RarityDefinition definition;
        if (dataLoader != null && dataLoader.Catalog != null && dataLoader.Catalog.rarities.TryGetValue(rarityId, out definition) && !string.IsNullOrWhiteSpace(definition.displayName))
        {
            return definition.displayName;
        }

        return string.IsNullOrWhiteSpace(rarityId) ? "-" : rarityId;
    }

    // 속성 id를 표시명으로 변환합니다.
    private string GetElementDisplayName(string elementId)
    {
        ElementDefinition definition;
        if (dataLoader != null && dataLoader.Catalog != null && dataLoader.Catalog.elements.TryGetValue(elementId, out definition) && !string.IsNullOrWhiteSpace(definition.displayName))
        {
            return definition.displayName;
        }

        return string.IsNullOrWhiteSpace(elementId) ? "-" : elementId;
    }

    // 스탯 id를 표시명으로 변환합니다.
    private string GetStatDisplayName(string statId)
    {
        StatDefinition definition;
        if (dataLoader != null && dataLoader.Catalog != null && dataLoader.Catalog.stats.TryGetValue(statId, out definition) && !string.IsNullOrWhiteSpace(definition.displayName))
        {
            return definition.displayName;
        }

        return GetFallbackStatDisplayName(statId);
    }

    // 스탯 수치를 operation과 value_type에 맞춰 표시합니다.
    private string FormatStatValue(ItemStatModifier stat)
    {
        StatDefinition definition = null;
        if (dataLoader != null && dataLoader.Catalog != null)
        {
            dataLoader.Catalog.stats.TryGetValue(stat.statId, out definition);
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

    // 조건부 스탯이면 조건 설명을 괄호로 붙입니다.
    private string GetConditionSuffix(string conditionId)
    {
        if (string.IsNullOrWhiteSpace(conditionId))
        {
            return string.Empty;
        }

        ConditionDefinition condition;
        if (dataLoader != null && dataLoader.Catalog != null && dataLoader.Catalog.conditions.TryGetValue(conditionId, out condition) && !string.IsNullOrWhiteSpace(condition.description))
        {
            return " (" + condition.description + ")";
        }

        return " (" + conditionId + ")";
    }

    // 카탈로그 조회가 실패해도 임시 상점 UI에서는 사람이 읽을 수 있는 스탯명을 보여줍니다.
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
}
