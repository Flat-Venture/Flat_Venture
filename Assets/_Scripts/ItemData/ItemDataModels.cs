using System.Collections.Generic;

namespace FlatVenture.ItemData
{
    // 아이템 CSV 행을 런타임에서 사용하기 위한 데이터 모델입니다.
    // 이 클래스들은 데이터만 담고, 실제 계산/효과 실행은 별도 시스템이 담당합니다.
    public sealed class ItemDefinition
    {
        public string itemId;
        public string displayName;
        public string rarityId;
        public string iconPath;
        public bool isCursed;
        public bool isUnique;
        public string dropPool;
        public string unlockId;
    }

    public sealed class ItemElement
    {
        public string itemId;
        public string elementId;
        public int elementValue;
    }

    public sealed class ItemStatModifier
    {
        public string itemId;
        public string statId;
        public string operation;
        public float value;
        public string appliesTo;
        public string conditionId;
        public string description;
    }

    public sealed class ItemEffectDefinition
    {
        public string itemId;
        public string effectId;
        public string triggerId;
        public string actionId;
        public int triggerCount;
        public float baseChance;
        public float baseCooldown;
        public string projectileId;
        public string statusId;
        public string buffId;
        public string target;
        public string aimRule;
    }

    public sealed class EffectParameter
    {
        public string effectId;
        public string paramId;
        public string value;
    }

    public sealed class ProjectileDefinition
    {
        public string projectileId;
        public string displayName;
        public string moveType;
        public string spawnPattern;
        public float speed;
        public float damageMultiplier;
        public int pierceCount;
        public int splitCount;
        public float splitAngle;
        public string onHitActionId;
        public string nextProjectileId;
        public string description;
    }

    public sealed class ItemRecord
    {
        public ItemDefinition definition;
        public readonly List<ItemElement> elements = new List<ItemElement>();
        public readonly List<ItemStatModifier> stats = new List<ItemStatModifier>();
        public readonly List<ItemEffectDefinition> effects = new List<ItemEffectDefinition>();
    }
}
