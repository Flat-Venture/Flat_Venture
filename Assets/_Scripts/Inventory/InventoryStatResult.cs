using System;
using System.Collections.Generic;

namespace FlatVenture.Inventory
{
    // 인벤토리에 들어온 아이템들이 제공하는 스탯 계산 결과입니다.
    // 조건이 없는 스탯은 즉시 적용 값으로 합산하고, 조건부 스탯은 별도 목록에 보관합니다.
    public sealed class InventoryStatResult
    {
        private readonly Dictionary<string, float> additiveValues = new Dictionary<string, float>();
        private readonly Dictionary<string, float> multiplierValues = new Dictionary<string, float>();
        private readonly List<InventoryStatSource> appliedSources = new List<InventoryStatSource>();
        private readonly List<InventoryStatSource> conditionalSources = new List<InventoryStatSource>();

        public IReadOnlyDictionary<string, float> AdditiveValues
        {
            get { return additiveValues; }
        }

        public IReadOnlyDictionary<string, float> MultiplierValues
        {
            get { return multiplierValues; }
        }

        public IReadOnlyList<InventoryStatSource> AppliedSources
        {
            get { return appliedSources; }
        }

        public IReadOnlyList<InventoryStatSource> ConditionalSources
        {
            get { return conditionalSources; }
        }

        // 조건 없는 add 스탯 값을 더합니다.
        public void AddAdditive(string statId, float value, InventoryStatSource source)
        {
            if (string.IsNullOrWhiteSpace(statId))
            {
                return;
            }

            float current;
            additiveValues.TryGetValue(statId, out current);
            additiveValues[statId] = current + value;
            appliedSources.Add(source);
        }

        // 조건 없는 multiply 스탯 값을 곱합니다.
        public void AddMultiplier(string statId, float value, InventoryStatSource source)
        {
            if (string.IsNullOrWhiteSpace(statId))
            {
                return;
            }

            float current;
            if (!multiplierValues.TryGetValue(statId, out current))
            {
                current = 1f;
            }

            multiplierValues[statId] = current * value;
            appliedSources.Add(source);
        }

        // 현재 조건을 판단할 수 없는 스탯은 따로 보관합니다.
        public void AddConditional(InventoryStatSource source)
        {
            conditionalSources.Add(source);
        }

        // add 방식으로 합산된 스탯 값을 반환합니다.
        public float GetAdditiveValue(string statId)
        {
            float value;
            return additiveValues.TryGetValue(statId, out value) ? value : 0f;
        }

        // multiply 방식으로 곱해진 스탯 값을 반환합니다.
        public float GetMultiplierValue(string statId)
        {
            float value;
            return multiplierValues.TryGetValue(statId, out value) ? value : 1f;
        }

        // stat_id에 해당하는 적용 스탯이 있는지 확인합니다.
        public bool HasStat(string statId)
        {
            return additiveValues.ContainsKey(statId) || multiplierValues.ContainsKey(statId);
        }
    }

    // 어떤 아이템/슬롯에서 스탯이 나왔는지 추적하기 위한 정보입니다.
    [Serializable]
    public sealed class InventoryStatSource
    {
        public int slotIndex;
        public string itemId;
        public string itemDisplayName;
        public string statId;
        public string operation;
        public float value;
        public string appliesTo;
        public string conditionId;
        public string description;
    }
}
