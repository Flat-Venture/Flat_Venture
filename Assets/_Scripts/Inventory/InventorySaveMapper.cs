using System;
using System.Collections.Generic;
using FlatVenture.ItemData;
using FlatVenture.SaveLoad;

namespace FlatVenture.Inventory
{
    // 인벤토리 런타임 상태와 SaveData 사이의 변환을 담당합니다.
    public static class InventorySaveMapper
    {
        // 현재 인벤토리 상태를 저장 데이터로 변환합니다.
        public static InventorySaveData Capture(InventoryGrid grid, InventoryWallet wallet)
        {
            var inventorySaveData = new InventorySaveData
            {
                width = grid.Width,
                height = grid.Height,
                gold = wallet != null ? wallet.Gold : 0,
                slots = new List<InventorySlotSaveData>()
            };

            for (int i = 0; i < grid.Slots.Count; i++)
            {
                inventorySaveData.slots.Add(BuildSlotSaveData(grid.Slots[i]));
            }

            return inventorySaveData;
        }

        // 아이템, 슬롯 상태, 골드 중 하나라도 있으면 의미 있는 인벤토리 상태로 봅니다.
        public static bool HasMeaningfulState(InventorySaveData inventorySaveData)
        {
            if (inventorySaveData == null)
            {
                return false;
            }

            if (inventorySaveData.gold > 0)
            {
                return true;
            }

            if (inventorySaveData.slots == null)
            {
                return false;
            }

            for (int i = 0; i < inventorySaveData.slots.Count; i++)
            {
                var slot = inventorySaveData.slots[i];
                if (slot == null)
                {
                    continue;
                }

                if (slot.item != null
                    || !string.IsNullOrEmpty(slot.frameElementId)
                    || slot.upgradeLevel > 0
                    || slot.isSealed)
                {
                    return true;
                }
            }

            return false;
        }

        // 저장된 인벤토리를 런타임 상태로 복원합니다.
        public static void Restore(InventorySaveData inventorySaveData, InventoryGrid grid, GameDataCatalog catalog, InventoryWallet wallet)
        {
            if (inventorySaveData == null || grid == null)
            {
                return;
            }

            // 복원 시에는 기존 런타임 상태를 지운 뒤 저장된 슬롯 상태를 다시 채웁니다.
            grid.ClearAll();
            if (wallet != null)
            {
                wallet.SetGold(inventorySaveData.gold);
            }

            if (inventorySaveData.slots == null)
            {
                return;
            }

            for (int i = 0; i < inventorySaveData.slots.Count; i++)
            {
                RestoreSlot(inventorySaveData.slots[i], grid, catalog);
            }
        }

        // 런타임 슬롯 하나를 저장용 슬롯 데이터로 변환합니다.
        private static InventorySlotSaveData BuildSlotSaveData(InventorySlot slot)
        {
            return new InventorySlotSaveData
            {
                slotIndex = slot.position.index,
                frameElementId = slot.frameElementId,
                upgradeLevel = slot.upgradeLevel,
                isSealed = slot.isSealed,
                // 슬롯 자체의 상태와 안에 들어있는 아이템을 분리해서 저장합니다.
                item = BuildItemSaveData(slot.item)
            };
        }

        // 런타임 아이템 하나를 저장용 아이템 데이터로 변환합니다.
        private static InventoryItemSaveData BuildItemSaveData(InventoryItem item)
        {
            if (item == null)
            {
                return null;
            }

            var itemSaveData = new InventoryItemSaveData
            {
                instanceId = item.instanceId,
                itemId = item.itemId,
                displayName = item.displayName,
                rarityId = item.rarityId,
                isCursed = item.isCursed,
                isUnique = item.isUnique,
                elements = new List<InventoryItemElementSaveData>()
            };

            for (int i = 0; i < item.elements.Count; i++)
            {
                itemSaveData.elements.Add(new InventoryItemElementSaveData
                {
                    elementId = item.elements[i].elementId,
                    elementValue = item.elements[i].elementValue
                });
            }

            return itemSaveData;
        }

        // 저장된 슬롯 데이터를 런타임 슬롯에 복원합니다.
        private static void RestoreSlot(InventorySlotSaveData slotSaveData, InventoryGrid grid, GameDataCatalog catalog)
        {
            if (slotSaveData == null)
            {
                return;
            }

            var slot = grid.GetSlot(slotSaveData.slotIndex);
            if (slot == null)
            {
                return;
            }

            slot.SetFrameElement(slotSaveData.frameElementId);
            slot.SetUpgradeLevel(slotSaveData.upgradeLevel);
            slot.SetSealed(slotSaveData.isSealed);

            if (IsValidSavedItem(slotSaveData.item))
            {
                // item_id가 있으면 CSV 원본 데이터를 먼저 사용하고, 저장된 instanceId/속성으로 덮어씁니다.
                var restoredItem = RestoreInventoryItem(slotSaveData.item, catalog);
                if (restoredItem != null)
                {
                    grid.TryAddItemAt(slotSaveData.slotIndex, restoredItem);
                }
            }
        }

        // 저장된 아이템 데이터를 런타임 아이템으로 복원합니다.
        private static InventoryItem RestoreInventoryItem(InventoryItemSaveData itemSaveData, GameDataCatalog catalog)
        {
            if (!IsValidSavedItem(itemSaveData))
            {
                return null;
            }

            InventoryItem item = null;
            ItemRecord itemRecord;
            if (catalog != null && catalog.TryGetItem(itemSaveData.itemId, out itemRecord))
            {
                item = InventoryItem.FromItemRecord(itemRecord);
            }

            if (item == null)
            {
                item = new InventoryItem();
            }

            // 저장된 instanceId가 없으면 구버전/임시 데이터로 보고 새 인스턴스 ID를 부여합니다.
            item.instanceId = string.IsNullOrEmpty(itemSaveData.instanceId) ? Guid.NewGuid().ToString("N") : itemSaveData.instanceId;
            item.itemId = itemSaveData.itemId;
            item.displayName = itemSaveData.displayName;
            item.rarityId = itemSaveData.rarityId;
            item.isCursed = itemSaveData.isCursed;
            item.isUnique = itemSaveData.isUnique;

            item.elements.Clear();
            if (itemSaveData.elements != null)
            {
                for (int i = 0; i < itemSaveData.elements.Count; i++)
                {
                    var element = itemSaveData.elements[i];
                    item.elements.Add(new InventoryItemElement(element.elementId, element.elementValue));
                }
            }

            return item;
        }

        // 저장된 아이템 데이터가 복원 가능한 최소 정보를 가지고 있는지 확인합니다.
        private static bool IsValidSavedItem(InventoryItemSaveData itemSaveData)
        {
            return itemSaveData != null && !string.IsNullOrWhiteSpace(itemSaveData.itemId);
        }
    }
}
