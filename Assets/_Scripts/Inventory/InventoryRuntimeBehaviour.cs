using System;
using System.Collections.Generic;
using FlatVenture.ItemData;
using FlatVenture.SaveLoad;
using UnityEngine;

namespace FlatVenture.Inventory
{
    // 씬에서 공유해서 사용하는 인벤토리 런타임 허브입니다.
    // Grid, Wallet, 시너지 계산을 묶고, 세부 계산/저장 변환은 전용 클래스로 위임합니다.
    public sealed class InventoryRuntimeBehaviour : MonoBehaviour
    {
        private static readonly List<float> SlotUpgradeWeights = new List<float> { 80f, 19f, 1f };

        [SerializeField] private GameDataLoaderBehaviour dataLoader;
        [SerializeField] private int width = InventoryGrid.DefaultWidth;
        [SerializeField] private int height = InventoryGrid.DefaultHeight;

        private readonly List<ItemRecord> sortedItems = new List<ItemRecord>();
        private readonly List<string> sortedElementIds = new List<string>();
        private InventorySynergyCalculator synergyCalculator;
        private InventoryStatCalculator statCalculator;
        private ISeedService seedService;

        public InventoryGrid Grid { get; private set; }
        // 던전 안에서 쓰는 골드입니다. 저장도 InventorySaveData 안에 함께 들어갑니다.
        public InventoryWallet Wallet { get; private set; }

        // 희귀도 CSV 기준 가격 계산은 전용 서비스에 맡깁니다.
        public InventoryPriceService PriceService { get; private set; }
        public GameDataCatalog Catalog { get; private set; }
        public InventorySynergyResult SynergyResult { get; private set; } = new InventorySynergyResult();
        public InventoryStatResult StatResult { get; private set; } = new InventoryStatResult();

        public IReadOnlyList<ItemRecord> SortedItems
        {
            get { return sortedItems; }
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

        // 던전 입장/로드 쪽에서 현재 던전 SeedService를 넘겨주면 강화 확률 같은 재현 대상에 사용합니다.
        public void SetSeedService(ISeedService nextSeedService)
        {
            seedService = nextSeedService;
        }

        // 인벤토리와 CSV 카탈로그 캐시를 준비합니다.
        private void Awake()
        {
            Grid = new InventoryGrid(width, height);
            Wallet = new InventoryWallet();
            RefreshCatalog();
            RecalculateSynergy();
            RecalculateStats();
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
            PriceService = new InventoryPriceService(Catalog);

            if (Catalog == null)
            {
                synergyCalculator = new InventorySynergyCalculator(null);
                statCalculator = new InventoryStatCalculator();
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
            statCalculator = new InventoryStatCalculator();
        }

        // CSV 아이템을 인벤토리의 무작위 빈 칸에 넣습니다.
        public bool TryGrantItem(ItemRecord record, out InventoryPosition position, out string message)
        {
            return TryGrantItem(record, true, out position, out message);
        }

        // CSV 아이템을 인벤토리에 넣고, 필요할 때만 활성 세이브에 즉시 반영합니다.
        // 보상/특수 방처럼 다음 체크포인트까지 저장하면 안 되는 흐름에서는 syncActiveSave를 false로 넘깁니다.
        public bool TryGrantItem(ItemRecord record, bool syncActiveSave, out InventoryPosition position, out string message)
        {
            position = new InventoryPosition();

            var item = InventoryItem.FromItemRecord(record);
            if (item == null)
            {
                message = "아이템 데이터가 비어 있습니다.";
                return false;
            }

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
            RecalculateStats();
            if (syncActiveSave)
            {
                SyncActiveSaveData();
            }
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

        // 인벤토리 아이템의 현재 적용 스탯을 다시 계산합니다.
        public void RecalculateStats()
        {
            if (statCalculator == null)
            {
                statCalculator = new InventoryStatCalculator();
            }

            StatResult = statCalculator.Calculate(Grid);
        }

        // add operation으로 합산된 스탯 값을 반환합니다.
        public float GetAdditiveStatValue(string statId)
        {
            return StatResult != null ? StatResult.GetAdditiveValue(statId) : 0f;
        }

        // multiply operation으로 곱해진 스탯 값을 반환합니다.
        public float GetMultiplierStatValue(string statId)
        {
            return StatResult != null ? StatResult.GetMultiplierValue(statId) : 1f;
        }

        // add operation 스탯이 존재하면 값을 반환합니다.
        public bool TryGetAdditiveStatValue(string statId, out float value)
        {
            value = GetAdditiveStatValue(statId);
            return StatResult != null && StatResult.AdditiveValues.ContainsKey(statId);
        }

        // multiply operation 스탯이 존재하면 값을 반환합니다.
        public bool TryGetMultiplierStatValue(string statId, out float value)
        {
            value = GetMultiplierStatValue(statId);
            return StatResult != null && StatResult.MultiplierValues.ContainsKey(statId);
        }

        // element_id에 해당하는 속성 계수 스탯 값을 반환합니다.
        public float GetElementPowerValue(string elementId)
        {
            return GetAdditiveStatValue(GetElementPowerStatId(elementId));
        }

        // element_id에 해당하는 속성 계수 스탯이 있으면 값을 반환합니다.
        public bool TryGetElementPowerValue(string elementId, out float value)
        {
            return TryGetAdditiveStatValue(GetElementPowerStatId(elementId), out value);
        }

        // element_id를 item_stats.csv에서 사용하는 속성 계수 stat_id로 변환합니다.
        public static string GetElementPowerStatId(string elementId)
        {
            if (string.IsNullOrWhiteSpace(elementId))
            {
                return "neutral_power";
            }

            switch (elementId)
            {
                case "fire": return "fire_power";
                case "water": return "water_power";
                case "nature": return "nature_power";
                case "earth": return "earth_power";
                case "lightning": return "lightning_power";
                case "poison": return "poison_power";
                case "dark": return "dark_power";
                case "curse": return "curse_power";
                case "neutral": return "neutral_power";
                default: return elementId + "_power";
            }
        }

        // 인벤토리와 골드를 초기화합니다.
        public void ClearInventoryForTest()
        {
            Grid.ClearAll();
            Wallet.Clear();
            RecalculateSynergy();
            RecalculateStats();
            SyncActiveSaveData();
        }

        // 지정한 세이브 데이터에 현재 인벤토리 상태를 기록합니다.
        // DungeonMapSaveBridge나 게임 종료 저장처럼 명확한 저장 시점에서 호출합니다.
        public void CaptureToSaveData(SaveData saveData)
        {
            if (saveData == null || saveData.dungeon == null || Grid == null)
            {
                return;
            }

            saveData.dungeon.inventory = CaptureSnapshot();
        }

        // 현재 런타임 인벤토리를 저장 가능한 스냅샷으로 만듭니다.
        public InventorySaveData CaptureSnapshot()
        {
            return InventorySaveMapper.Capture(Grid, Wallet);
        }

        // 저장 파일을 건드리지 않고 임시 스냅샷 상태로 인벤토리를 되돌립니다.
        public void RestoreSnapshot(InventorySaveData inventorySaveData)
        {
            if (inventorySaveData == null || Grid == null)
            {
                return;
            }

            RefreshCatalog();
            InventorySaveMapper.Restore(inventorySaveData, Grid, Catalog, Wallet);
            RecalculateSynergy();
            RecalculateStats();
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
            InventorySaveMapper.Restore(saveData.dungeon.inventory, Grid, Catalog, Wallet);
            RecalculateSynergy();
            RecalculateStats();
        }

        // 속성 ID의 표시 이름을 반환합니다.
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

        // 인벤토리 테스트 UI에서 프레임 속성 선택지로 표시해도 되는 속성인지 확인합니다.
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

        // Forge 스트림으로 슬롯 강화 수치를 뽑고 선택한 슬롯에 적용합니다.
        public bool TryAddRolledSlotUpgrade(int index, out int upgradeAmount, out string message)
        {
            return TryAddRolledSlotUpgrade(index, true, out upgradeAmount, out message);
        }

        // Forge 스트림으로 슬롯 강화 수치를 뽑고, 필요할 때만 활성 세이브에 즉시 반영합니다.
        public bool TryAddRolledSlotUpgrade(int index, bool syncActiveSave, out int upgradeAmount, out string message)
        {
            upgradeAmount = RollSlotUpgradeAmount(seedService);

            if (!Grid.TryAddSlotUpgrade(index, upgradeAmount))
            {
                message = "강화할 슬롯을 먼저 선택해주세요.";
                return false;
            }

            var slot = Grid.GetSlot(index);
            message = "슬롯 강화 변화량 +" + upgradeAmount + " / 현재 +" + (slot != null ? slot.upgradeLevel : 0);
            if (syncActiveSave)
            {
                SyncActiveSaveData();
            }
            return true;
        }

        // 골드를 지불하고 선택 슬롯을 강화합니다.
        public bool TryPurchaseSlotUpgrade(int index, int cost, out int upgradeAmount, out string message)
        {
            return TryPurchaseSlotUpgrade(index, cost, true, out upgradeAmount, out message);
        }

        // 골드를 지불하고 선택 슬롯을 강화한 뒤, 필요할 때만 활성 세이브에 즉시 반영합니다.
        // 특수 방 제련소에서는 syncActiveSave=false로 호출해 다음 노드 선택 전까지 저장하지 않습니다.
        public bool TryPurchaseSlotUpgrade(int index, int cost, bool syncActiveSave, out int upgradeAmount, out string message)
        {
            upgradeAmount = 0;
            string spendMessage;
            if (!Wallet.TrySpendGold(cost, out spendMessage))
            {
                message = spendMessage;
                return false;
            }

            if (!TryAddRolledSlotUpgrade(index, false, out upgradeAmount, out message))
            {
                Wallet.AddGold(cost);
                return false;
            }

            RecalculateSynergy();
            RecalculateStats();
            message += " / 비용 " + cost + " 골드";
            if (syncActiveSave)
            {
                SyncActiveSaveData();
            }
            return true;
        }

        // 골드를 지불하고 슬롯 프레임 속성을 지정합니다. 빈 elementId를 넘기면 속성을 제거합니다.
        public bool TryPurchaseFrameElement(int index, string elementId, int cost, out string message)
        {
            return TryPurchaseFrameElement(index, elementId, cost, true, out message);
        }

        // 골드를 지불하고 슬롯 프레임 속성을 지정한 뒤, 필요할 때만 활성 세이브에 즉시 반영합니다.
        // 같은 슬롯에 다시 속성을 부여하면 기존 frameElementId를 새 값으로 덮어씁니다.
        public bool TryPurchaseFrameElement(int index, string elementId, int cost, bool syncActiveSave, out string message)
        {
            var slot = Grid.GetSlot(index);
            if (slot == null)
            {
                message = "프레임 속성을 적용할 슬롯을 선택해주세요.";
                return false;
            }

            string spendMessage;
            if (!Wallet.TrySpendGold(cost, out spendMessage))
            {
                message = spendMessage;
                return false;
            }

            slot.SetFrameElement(elementId);
            RecalculateSynergy();
            RecalculateStats();
            message = string.IsNullOrEmpty(elementId)
                ? "프레임 속성을 제거했습니다. / 비용 " + cost + " 골드"
                : "프레임 속성 적용: " + GetElementDisplayName(elementId) + " / 비용 " + cost + " 골드";
            if (syncActiveSave)
            {
                SyncActiveSaveData();
            }
            return true;
        }

        // 지정 슬롯 아이템을 판매하고 가격 서비스의 판매 비율에 맞춰 골드를 지급합니다.
        public bool TrySellItemAt(int index, out int earnedGold, out string message)
        {
            return TrySellItemAt(index, true, out earnedGold, out message);
        }

        // 지정 슬롯 아이템을 판매하고, 필요할 때만 활성 세이브에 즉시 반영합니다.
        // 상점 판매 모드에서는 특수 방 정책에 맞춰 syncActiveSave=false로 호출합니다.
        public bool TrySellItemAt(int index, bool syncActiveSave, out int earnedGold, out string message)
        {
            earnedGold = 0;

            InventoryItem removedItem;
            if (!Grid.TryRemoveItem(index, out removedItem))
            {
                message = "판매할 아이템이 없습니다.";
                return false;
            }

            earnedGold = PriceService != null ? PriceService.CalculateShopSellPrice(removedItem) : 0;
            Wallet.AddGold(earnedGold);
            RecalculateSynergy();
            RecalculateStats();
            message = removedItem.displayName + " 판매: +" + earnedGold + " 골드";
            if (syncActiveSave)
            {
                SyncActiveSaveData();
            }
            return true;
        }

        // 지정 슬롯의 아이템을 새 아이템 데이터로 교체합니다. 제련소 등급업 테스트에 사용합니다.
        public bool TryReplaceItemAt(int index, ItemRecord record, int cost, out string message)
        {
            return TryReplaceItemAt(index, record, cost, true, out message);
        }

        // 지정 슬롯의 아이템을 새 아이템으로 교체하고, 필요할 때만 활성 세이브에 즉시 반영합니다.
        // 제련소 등급업은 기존 슬롯 위치를 유지한 채 아이템 내용만 교체합니다.
        public bool TryReplaceItemAt(int index, ItemRecord record, int cost, bool syncActiveSave, out string message)
        {
            var slot = Grid.GetSlot(index);
            if (slot == null || !slot.HasItem)
            {
                message = "등급업할 아이템을 선택해주세요.";
                return false;
            }

            var nextItem = InventoryItem.FromItemRecord(record);
            if (nextItem == null)
            {
                message = "교체할 아이템 데이터가 없습니다.";
                return false;
            }

            if (nextItem.isUnique && Grid.ContainsItemId(nextItem.itemId))
            {
                message = "고유 아이템은 중복 획득할 수 없습니다: " + nextItem.displayName;
                return false;
            }

            string spendMessage;
            if (!Wallet.TrySpendGold(cost, out spendMessage))
            {
                message = spendMessage;
                return false;
            }

            slot.item = nextItem;
            RecalculateSynergy();
            RecalculateStats();
            message = "아이템 등급업: " + nextItem.displayName + " / 비용 " + cost + " 골드";
            if (syncActiveSave)
            {
                SyncActiveSaveData();
            }
            return true;
        }

        // 기능 결과를 현재 메모리 세이브에만 반영합니다. 파일 저장은 포탈 생성/게임 종료 시점에서 처리합니다.
        // 단, PortalGenerated 체크포인트 이후의 보상 선택 결과는 저장에 섞이면 안 되므로 내부에서 방어합니다.
        public void SyncActiveSaveData()
        {
            if (!SaveGameSession.HasActiveSave || SaveGameSession.CurrentSaveData == null)
            {
                return;
            }

            var dungeon = SaveGameSession.CurrentSaveData.dungeon;
            if (dungeon != null && dungeon.dungeonState == DungeonSaveState.PortalGenerated)
            {
                return;
            }

            CaptureToSaveData(SaveGameSession.CurrentSaveData);
        }

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

        // item_id를 기준으로 아이템 목록을 정렬합니다.
        private static int CompareItemId(ItemRecord left, ItemRecord right)
        {
            string leftId = left != null && left.definition != null ? left.definition.itemId : string.Empty;
            string rightId = right != null && right.definition != null ? right.definition.itemId : string.Empty;
            return string.CompareOrdinal(leftId, rightId);
        }
    }
}
