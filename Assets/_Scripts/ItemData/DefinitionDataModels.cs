namespace FlatVenture.ItemData
{
    // 아이템 시스템이 공통으로 참조하는 정의 CSV 모델입니다.
    // 각 id는 로더 검증, 핸들러 선택, UI 표시 등에 사용됩니다.
    public sealed class RarityDefinition
    {
        public string rarityId;
        public string displayName;
        public int sortOrder;
        public string colorHex;
        public int basePrice;
        public float sellRate;
        public string description;
    }

    public sealed class RarityWeight
    {
        public string tableId;
        public string rarityId;
        public int weight;
    }

    public sealed class ElementDefinition
    {
        public string elementId;
        public string displayName;
        public string elementRole;
        public bool countsForBingo;
        public string advancedElementId;
        public string description;
    }

    public sealed class StatDefinition
    {
        public string statId;
        public string displayName;
        public string statGroup;
        public string valueType;
        public string stackRule;
        public string minValue;
        public string maxValue;
        public string description;
    }

    public sealed class StatusEffectDefinition
    {
        public string statusId;
        public string displayName;
        public string stackRule;
        public int maxStack;
        public float duration;
        public float baseTickDamage;
        public float tickInterval;
        public string description;
    }

    public sealed class ConditionDefinition
    {
        public string conditionId;
        public string conditionType;
        public string paramId;
        public string value;
        public string description;
    }

    public sealed class TriggerDefinition
    {
        public string triggerId;
        public string displayName;
        public bool needsCounter;
        public bool usesChance;
        public bool usesCooldown;
        public bool resetsOnFloorEnd;
        public string description;
    }

    public sealed class ActionDefinition
    {
        public string actionId;
        public string displayName;
        public string requiredRef;
        public string description;
    }
}
