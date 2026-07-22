using System;

namespace FlatVenture.Inventory
{
    // 인벤토리 아이템 스탯을 전투/플레이어 시스템이 읽을 수 있는 결과로 계산합니다.
    public sealed class InventoryStatCalculator
    {
        // 인벤토리 슬롯을 훑어 아이템 스탯을 합산합니다.
        // 슬롯 강화와 시너지 스탯 보정은 기획 수치가 확정된 뒤 이 계산기에 추가하면 됩니다.
        public InventoryStatResult Calculate(InventoryGrid grid)
        {
            var result = new InventoryStatResult();
            if (grid == null)
            {
                return result;
            }

            for (int i = 0; i < grid.Slots.Count; i++)
            {
                AddSlotStats(grid.Slots[i], result);
            }

            return result;
        }

        // 봉인되지 않은 슬롯에 들어 있는 아이템의 스탯만 결과에 더합니다.
        private static void AddSlotStats(InventorySlot slot, InventoryStatResult result)
        {
            if (slot == null || slot.isSealed || slot.item == null)
            {
                return;
            }

            var item = slot.item;
            for (int i = 0; i < item.stats.Count; i++)
            {
                AddItemStat(slot, item, item.stats[i], result);
            }
        }

        // 스탯 operation과 condition_id에 따라 즉시 적용 또는 조건부 목록으로 분류합니다.
        private static void AddItemStat(InventorySlot slot, InventoryItem item, InventoryItemStat stat, InventoryStatResult result)
        {
            if (string.IsNullOrWhiteSpace(stat.statId))
            {
                return;
            }

            var source = new InventoryStatSource
            {
                slotIndex = slot.position.index,
                itemId = item.itemId,
                itemDisplayName = item.displayName,
                statId = stat.statId,
                operation = stat.operation,
                value = stat.value,
                appliesTo = stat.appliesTo,
                conditionId = stat.conditionId,
                description = stat.description
            };

            if (!string.IsNullOrWhiteSpace(stat.conditionId))
            {
                result.AddConditional(source);
                return;
            }

            if (IsMultiplyOperation(stat.operation))
            {
                result.AddMultiplier(stat.statId, stat.value, source);
            }
            else
            {
                result.AddAdditive(stat.statId, stat.value, source);
            }
        }

        // CSV operation 값이 multiply 계열인지 확인합니다.
        private static bool IsMultiplyOperation(string operation)
        {
            return string.Equals(operation, "multiply", StringComparison.OrdinalIgnoreCase)
                || string.Equals(operation, "multiplier", StringComparison.OrdinalIgnoreCase);
        }
    }
}
