using System;
using System.Collections.Generic;
using FlatVenture.ItemData;
using FlatVenture.NUH.Seed;
using FlatVenture.SaveLoad;
using UnityEngine;

namespace FlatVenture.Inventory
{
    // 한 씬에서 공유해서 사용하는 인벤토리 런타임 허브입니다.
    // 테스트 UI, 치트 지급창, 이후 정식 UI는 이 컴포넌트의 InventoryGrid를 함께 바라보면 됩니다.
    public sealed class InventoryRuntimeBehaviour : MonoBehaviour
    {
        private static readonly List<float> SlotUpgradeWeights = new List<float> { 80f, 19f, 1f };

        [SerializeField] private GameDataLoaderBehaviour dataLoader;
        [SerializeField] private int width = InventoryGrid.DefaultWidth;
        [SerializeField] private int height = InventoryGrid.DefaultHeight;

        private readonly List<ItemRecord> sortedItems = new List<ItemRecord>();
        private readonly List<string> sortedElementIds = new List<string>();
        private InventorySynergyCalculator synergyCalculator;
        private ISeedService seedService;

        public InventoryGrid Grid { get; private set; }
        public GameDataCatalog Catalog { get; private set; }
        public InventorySynergyResult SynergyResult { get; private set; } = new InventorySynergyResult();
        public int DebugGold { get; private set; }

        public IReadOnlyList<ItemRecord> SortedItems
        {
            get { return sortedItems; }
        }

        // 던전 입장/로드 쪽에서 현재 던전 SeedService를 넘겨주면 가격/강화 같은 재현 대상에 사용합니다.
        public void SetSeedService(ISeedService nextSeedService)
        {
            seedService = nextSeedService;
        }

        public IReadOnlyList<string> SortedElementIds
        {
            get { return sortedElementIds; }
        }

        public bool HasCursedItem
        {
            get
            {
                if (Grid == null)
                {
                    return false;
                }

                for (int i = 0; i < Grid.Slots.Count; i++)
                {
                    var item = Grid.Slots[i].item;
                    if (item != null && item.isCursed)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        // 인벤토리와 CSV 카탈로그 캐시를 준비합니다.
        private void Awake()
        {
            Grid = new InventoryGrid(width, height);
            RefreshCatalog();
            RecalculateSynergy();
        }

        // 저장 세션이 있으면 던전 인벤토리 상태를 복원합니다.
        private void Start()
        {
            RestoreFromActiveSave();
        }

        // 씬에 있는 GameDataLoaderBehaviour를 찾아 CSV 데이터에 연결합니다.
        public void RefreshCatalog()
        {
            if (dataLoader == null)
            {
                dataLoader = FindFirstObjectByType<GameDataLoaderBehaviour>();
            }

            Catalog = dataLoader != null ? dataLoader.Catalog : null;
            sortedItems.Clear();
            sortedElementIds.Clear();

            if (Catalog == null)
            {
                synergyCalculator = new InventorySynergyCalculator(null);
                return;
            }

            foreach (var pair in Catalog.items)
            {
                sortedItems.Add(pair.Value);
            }

            sortedItems.Sort(CompareItemId);

            foreach (var pair in Catalog.elements)
            {
                if (IsElementVisibleInInventory(pair.Key))
                {
                    sortedElementIds.Add(pair.Key);
                }
            }

            sortedElementIds.Sort(StringComparer.Ordinal);
            synergyCalculator = new InventorySynergyCalculator(Catalog);
        }

        // CSV 아이템을 인벤토리의 랜덤 빈 칸에 넣습니다.
        public bool TryGrantItem(ItemRecord record, out InventoryPosition position, out string message)
        {
            position = new InventoryPosition();

            var item = InventoryItem.FromItemRecord(record);
            if (item == null)
            {
                message = "아이템 데이터가 비어 있습니다.";
                return false;
            }

            item.sellPrice = CalculateSellPrice(item);

            if (item.isUnique && Grid.ContainsItemId(item.itemId))
            {
                message = "고유 아이템은 중복 획득할 수 없습니다: " + item.displayName;
                return false;
            }

            if (!Grid.TryAddItemRandom(item, out position))
            {
                message = "인벤토리가 가득 찼습니다.";
                return false;
            }

            message = item.displayName + " 획득: " + position;
            RecalculateSynergy();
            return true;
        }

        // 인벤토리의 시너지 결과를 다시 계산합니다.
        public void RecalculateSynergy()
        {
            if (synergyCalculator == null)
            {
                synergyCalculator = new InventorySynergyCalculator(Catalog);
            }

            SynergyResult = synergyCalculator.Calculate(Grid);
        }

        // 테스트용 골드를 증가시킵니다.
        public void AddDebugGold(int amount)
        {
            DebugGold += Mathf.Max(0, amount);

            if (SaveGameSession.HasActiveSave && SaveGameSession.CurrentSaveData != null && SaveGameSession.CurrentSaveData.dungeon != null)
            {
                SaveGameSession.CurrentSaveData.dungeon.gold = DebugGold;
            }
        }

        // 인벤토리와 테스트 골드를 초기화합니다.
        public void ClearInventoryForTest()
        {
            Grid.ClearAll();
            DebugGold = 0;
            RecalculateSynergy();
        }

        // 현재 활성 세이브에 인벤토리 상태를 기록합니다.
        public void CaptureToActiveSave()
        {
            if (!SaveGameSession.HasActiveSave || SaveGameSession.CurrentSaveData == null)
            {
                return;
            }

            CaptureToSaveData(SaveGameSession.CurrentSaveData);
        }

        // 지정한 세이브 데이터에 현재 인벤토리 상태를 기록합니다.
        public void CaptureToSaveData(SaveData saveData)
        {
            if (saveData == null || saveData.dungeon == null || Grid == null)
            {
                return;
            }

            saveData.dungeon.inventory = BuildInventorySaveData();
            saveData.dungeon.gold = DebugGold;
        }

        // 현재 활성 세이브에서 인벤토리 상태를 복원합니다.
        public void RestoreFromActiveSave()
        {
            if (!SaveGameSession.HasActiveSave || SaveGameSession.CurrentSaveData == null)
            {
                return;
            }

            RestoreFromSaveData(SaveGameSession.CurrentSaveData);
        }

        // 지정한 세이브 데이터에서 인벤토리 상태를 복원합니다.
        public void RestoreFromSaveData(SaveData saveData)
        {
            if (saveData == null || saveData.dungeon == null || saveData.dungeon.inventory == null || Grid == null)
            {
                return;
            }

            RefreshCatalog();
            Grid.ClearAll();

            var inventorySaveData = saveData.dungeon.inventory;
            DebugGold = inventorySaveData.debugGold > 0 ? inventorySaveData.debugGold : saveData.dungeon.gold;

            if (inventorySaveData.slots != null)
            {
                for (int i = 0; i < inventorySaveData.slots.Count; i++)
                {
                    RestoreSlot(inventorySaveData.slots[i]);
                }
            }

            RecalculateSynergy();
        }

        // 현재 인벤토리 상태를 저장 가능한 데이터로 변환합니다.
        private InventorySaveData BuildInventorySaveData()
        {
            var inventorySaveData = new InventorySaveData
            {
                width = Grid.Width,
                height = Grid.Height,
                debugGold = DebugGold,
                slots = new List<InventorySlotSaveData>()
            };

            for (int i = 0; i < Grid.Slots.Count; i++)
            {
                var slot = Grid.Slots[i];
                inventorySaveData.slots.Add(BuildSlotSaveData(slot));
            }

            return inventorySaveData;
        }

        // 슬롯 하나를 저장 가능한 데이터로 변환합니다.
        private static InventorySlotSaveData BuildSlotSaveData(InventorySlot slot)
        {
            return new InventorySlotSaveData
            {
                slotIndex = slot.position.index,
                frameElementId = slot.frameElementId,
                upgradeLevel = slot.upgradeLevel,
                isSealed = slot.isSealed,
                item = BuildItemSaveData(slot.item)
            };
        }

        // 아이템 인스턴스를 저장 가능한 데이터로 변환합니다.
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
                sellPrice = item.sellPrice,
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

        // 저장된 슬롯 데이터를 현재 인벤토리에 복원합니다.
        private void RestoreSlot(InventorySlotSaveData slotSaveData)
        {
            if (slotSaveData == null)
            {
                return;
            }

            var slot = Grid.GetSlot(slotSaveData.slotIndex);
            if (slot == null)
            {
                return;
            }

            slot.SetFrameElement(slotSaveData.frameElementId);
            slot.SetUpgradeLevel(slotSaveData.upgradeLevel);
            slot.SetSealed(slotSaveData.isSealed);

            if (IsValidSavedItem(slotSaveData.item))
            {
                var restoredItem = BuildInventoryItem(slotSaveData.item);
                if (restoredItem != null)
                {
                    Grid.TryAddItemAt(slotSaveData.slotIndex, restoredItem);
                }
            }
        }

        // 저장된 아이템 데이터를 런타임 인벤토리 아이템으로 복원합니다.
        private InventoryItem BuildInventoryItem(InventoryItemSaveData itemSaveData)
        {
            if (!IsValidSavedItem(itemSaveData))
            {
                return null;
            }

            InventoryItem item = null;

            ItemRecord itemRecord;
            if (Catalog != null && Catalog.TryGetItem(itemSaveData.itemId, out itemRecord))
            {
                item = InventoryItem.FromItemRecord(itemRecord);
            }

            if (item == null)
            {
                item = new InventoryItem();
            }

            item.instanceId = string.IsNullOrEmpty(itemSaveData.instanceId) ? Guid.NewGuid().ToString("N") : itemSaveData.instanceId;
            item.itemId = itemSaveData.itemId;
            item.displayName = itemSaveData.displayName;
            item.rarityId = itemSaveData.rarityId;
            item.isCursed = itemSaveData.isCursed;
            item.isUnique = itemSaveData.isUnique;
            item.sellPrice = itemSaveData.sellPrice;

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

        // 속성 ID의 표시 이름을 반환합니다.
        // JsonUtility가 빈 하위 객체를 만들더라도 실제 아이템 ID가 없으면 빈 칸으로 처리합니다.
        private static bool IsValidSavedItem(InventoryItemSaveData itemSaveData)
        {
            return itemSaveData != null && !string.IsNullOrWhiteSpace(itemSaveData.itemId);
        }

        public string GetElementDisplayName(string elementId)
        {
            if (Catalog != null && Catalog.elements != null)
            {
                ElementDefinition definition;
                if (Catalog.elements.TryGetValue(elementId, out definition))
                {
                    return definition.displayName;
                }
            }

            return elementId;
        }

        // 인벤토리 테스트 UI와 프레임 속성 선택에 표시할 수 있는 속성인지 확인합니다.
        public bool IsElementVisibleInInventory(string elementId)
        {
            if (string.IsNullOrEmpty(elementId))
            {
                return false;
            }

            if (Catalog == null || Catalog.elements == null)
            {
                return true;
            }

            ElementDefinition definition;
            return !Catalog.elements.TryGetValue(elementId, out definition)
                || definition.elementRole != "advanced";
        }

        // 등급 기준 판매가를 10% 오차 범위 안에서 계산합니다.
        public int CalculateSellPrice(InventoryItem item)
        {
            return CalculateSellPrice(item, seedService);
        }

        // SeedService가 연결되어 있으면 Shop 스트림으로 판매 가격 오차를 계산합니다.
        public int CalculateSellPrice(InventoryItem item, ISeedService sourceSeedService)
        {
            var random = sourceSeedService != null ? sourceSeedService.GetStream(SeedStreamNames.Shop) : null;
            return CalculateSellPrice(item, random);
        }

        // 판매 가격은 등급 기준 가격에 10% 오차를 더합니다.
        public int CalculateSellPrice(InventoryItem item, IRandomStream random)
        {
            if (item != null && item.sellPrice > 0)
            {
                return item.sellPrice;
            }

            int basePrice = GetBasePriceByRarity(item != null ? item.rarityId : null);
            float multiplier = random != null ? random.Range(0.9f, 1.1f) : UnityEngine.Random.Range(0.9f, 1.1f);
            return Mathf.RoundToInt(basePrice * multiplier);
        }

        // Forge 스트림으로 슬롯 강화 수치를 뽑고 선택된 슬롯에 적용합니다.
        public bool TryAddRolledSlotUpgrade(int index, out int upgradeAmount, out string message)
        {
            upgradeAmount = RollSlotUpgradeAmount(seedService);

            if (!Grid.TryAddSlotUpgrade(index, upgradeAmount))
            {
                message = "강화할 슬롯을 먼저 선택해주세요.";
                return false;
            }

            var slot = Grid.GetSlot(index);
            message = "슬롯 강화 변화량 +" + upgradeAmount + " / 현재 +" + (slot != null ? slot.upgradeLevel : 0);
            return true;
        }

        // SeedService가 연결되어 있으면 Forge 스트림을 사용하고, 없으면 테스트용 기본값 +1을 반환합니다.
        public int RollSlotUpgradeAmount(ISeedService sourceSeedService)
        {
            var random = sourceSeedService != null ? sourceSeedService.GetStream(SeedStreamNames.Forge) : null;
            return RollSlotUpgradeAmount(random);
        }

        // 슬롯 강화 확률은 +1 80%, +2 19%, +3 1%입니다.
        public int RollSlotUpgradeAmount(IRandomStream random)
        {
            if (random == null)
            {
                return 1;
            }

            return random.WeightedIndex(SlotUpgradeWeights) + 1;
        }

        // 등급 순서에 따라 1000, 2000, 3000, 4000 기준가를 반환합니다.
        private int GetBasePriceByRarity(string rarityId)
        {
            int sortOrder = 1;

            if (Catalog != null && Catalog.rarities != null)
            {
                RarityDefinition rarity;
                if (Catalog.rarities.TryGetValue(rarityId, out rarity))
                {
                    sortOrder = rarity.sortOrder;
                }
            }

            if (sortOrder < 1)
            {
                sortOrder = 1;
            }

            if (sortOrder > 4)
            {
                sortOrder = 4;
            }

            return sortOrder * 1000;
        }

        // item_id를 기준으로 아이템 목록을 정렬합니다.
        private static int CompareItemId(ItemRecord left, ItemRecord right)
        {
            string leftId = left != null && left.definition != null ? left.definition.itemId : string.Empty;
            string rightId = right != null && right.definition != null ? right.definition.itemId : string.Empty;
            return string.CompareOrdinal(leftId, rightId);
        }
    }
}
