using System;
using System.Collections.Generic;
using FlatVenture.ItemData;

namespace FlatVenture.Inventory
{
    // 인벤토리에 실제로 들어온 아이템 1개의 런타임 데이터입니다.
    [Serializable]
    public sealed class InventoryItem
    {
        public string instanceId;   // 인벤토리에 같은 아이템이 있을 때, 구분하기 위한 고유 ID
        public string itemId;
        public string displayName;
        public string rarityId;
        public bool isCursed;
        public bool isUnique;
        public readonly List<InventoryItemElement> elements = new List<InventoryItemElement>();
        public readonly List<InventoryItemStat> stats = new List<InventoryItemStat>();

        // CSV 아이템 레코드에서 인벤토리용 아이템 인스턴스를 생성합니다.
        public static InventoryItem FromItemRecord(ItemRecord record)
        {
            if (record == null || record.definition == null)
            {
                return null;
            }

            var item = new InventoryItem
            {
                instanceId = Guid.NewGuid().ToString("N"),
                itemId = record.definition.itemId,
                displayName = record.definition.displayName,
                rarityId = record.definition.rarityId,
                isCursed = record.definition.isCursed,
                isUnique = record.definition.isUnique
            };

            for (int i = 0; i < record.elements.Count; i++)
            {
                var element = record.elements[i];
                item.elements.Add(new InventoryItemElement(element.elementId, element.elementValue));
            }

            for (int i = 0; i < record.stats.Count; i++)
            {
                var stat = record.stats[i];
                item.stats.Add(new InventoryItemStat(stat.statId, stat.operation, stat.value, stat.appliesTo, stat.conditionId, stat.description));
            }

            return item;
        }

        // 아이템의 대표 속성을 반환합니다. 빙고 계산에서 프레임 속성이 없을 때 사용합니다.
        public bool TryGetPrimaryElementId(out string elementId)
        {
            elementId = null;
            var bestValue = int.MinValue;

            for (int i = 0; i < elements.Count; i++)
            {
                var element = elements[i];
                if (string.IsNullOrEmpty(element.elementId))
                {
                    continue;
                }

                if (element.elementValue > bestValue)
                {
                    bestValue = element.elementValue;
                    elementId = element.elementId;
                }
            }

            return !string.IsNullOrEmpty(elementId);
        }
    }

    // 아이템 하나가 가진 속성 포인트입니다.
    [Serializable]
    public struct InventoryItemElement
    {
        public string elementId;
        public int elementValue;

        public InventoryItemElement(string elementId, int elementValue)
        {
            this.elementId = elementId;
            this.elementValue = elementValue;
        }
    }

    // 아이템 하나가 가진 스탯 보정 정보입니다.
    [Serializable]
    public struct InventoryItemStat
    {
        public string statId;
        public string operation;
        public float value;
        public string appliesTo;
        public string conditionId;
        public string description;

        public InventoryItemStat(string statId, string operation, float value, string appliesTo, string conditionId, string description)
        {
            this.statId = statId;
            this.operation = operation;
            this.value = value;
            this.appliesTo = appliesTo;
            this.conditionId = conditionId;
            this.description = description;
        }
    }
}
