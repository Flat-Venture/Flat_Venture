using System.Collections.Generic;
using FlatVenture.Inventory;
using FlatVenture.ItemData;
using FlatVenture.SaveLoad;
using UnityEngine;

// 상점 방 컨트롤러입니다. 임시 UI로 구매와 판매 흐름을 테스트합니다.
public sealed class ShopRoomController : SpecialRoomControllerBase
{
    private const int ShopItemCount = 5;
    private const string ShopWeightTableId = "shop_default";

    [SerializeField] private GameDataLoaderBehaviour dataLoader;

    private readonly List<ItemRecord> shopItems = new List<ItemRecord>();
    private readonly HashSet<int> soldShopIndices = new HashSet<int>();
    private bool isOpen;
    private bool isSellMode;
    private string message;

    public IReadOnlyList<ItemRecord> ShopItems
    {
        get { return shopItems; }
    }

    public bool IsOpen
    {
        get { return isOpen; }
    }

    public bool IsSellMode
    {
        get { return isSellMode; }
    }

    public string Message
    {
        get { return message; }
    }

    public override bool ConsumesInteraction
    {
        get { return false; }
    }

    // 방 입장 시 상품 목록을 만들고 포탈을 바로 생성합니다.
    // 특수 방은 포탈 생성 시 저장하지 않으므로 startFromPortalCheckpoint 값은 사용하지 않습니다.
    public override void StartRoomEvent(bool startFromPortalCheckpoint = false)
    {
        Debug.Log("<color=green>[Shop Room]</color> 상점 방에 입장했습니다.");
        ResolveReferences();
        BuildShopItems();
        soldShopIndices.Clear();
        SpawnPortal();
    }

    // NPC 상호작용으로 상점 UI를 엽니다.
    // 상점은 포탈이 처음부터 열려 있으므로 UI를 닫아도 방 진행 자체는 유지됩니다.
    public override void OpenInteractionUI()
    {
        ResolveReferences();
        if (shopItems.Count == 0)
        {
            BuildShopItems();
        }

        isOpen = true;
        DungeonUiInputBlocker.SetBlocked(this, true);
        isSellMode = false;
        message = string.Empty;
    }

    // 정식 UI 버튼에서 호출할 구매 메소드입니다.
    // 구매 결과는 런타임 인벤토리에만 반영하고, 저장은 다음 노드 선택 시점에 확정합니다.
    public void BuyItem(int shopIndex)
    {
        if (shopIndex < 0 || shopIndex >= shopItems.Count)
        {
            message = "구매할 상품이 없습니다.";
            return;
        }

        if (soldShopIndices.Contains(shopIndex))
        {
            message = "이미 구매한 상품입니다.";
            return;
        }

        var item = shopItems[shopIndex];
        int price = GetItemPrice(item);
        if (price <= 0)
        {
            message = "아이템 가격 정보를 찾지 못했습니다. " + item.definition.rarityId;
            return;
        }

        string spendMessage;
        if (!inventoryRuntime.Wallet.TrySpendGold(price, out spendMessage))
        {
            message = spendMessage;
            return;
        }

        InventoryPosition position;
        if (!inventoryRuntime.TryGrantItem(item, false, out position, out message))
        {
            inventoryRuntime.Wallet.AddGold(price);
            return;
        }

        message = item.definition.displayName + " 구매: " + price + " 골드";
        soldShopIndices.Add(shopIndex);
    }

    // 상점 판매 모드로 전환하고 인벤토리 슬롯 선택 화면을 엽니다.
    public void OpenSellMode()
    {
        // 판매는 기존 인벤토리 UI를 재사용합니다. F키 버리기 대신 슬롯 클릭 판매 흐름으로 전환합니다.
        isSellMode = true;
        isOpen = false;
        ShowInventorySellPanel();
        message = "판매할 아이템 슬롯을 선택하세요.";
    }

    // 인벤토리 판매 화면에서 다시 상점 카드 화면으로 돌아올 때 호출합니다.
    public void ReturnToShop()
    {
        isOpen = true;
        isSellMode = false;
        HideInventoryPanel();
        message = string.Empty;
    }

    // 상점 기능에 필요한 인벤토리와 데이터 로더 참조를 찾습니다.
    private void ResolveReferences()
    {
        FindInventoryIfNeeded();

        if (dataLoader == null)
        {
            dataLoader = FindFirstObjectByType<GameDataLoaderBehaviour>();
        }
    }

    // CSV 희귀도 가중치와 던전 시드로 이번 상점의 상품 목록을 생성합니다.
    private void BuildShopItems()
    {
        shopItems.Clear();
        if (dataLoader == null || dataLoader.Catalog == null)
        {
            message = "아이템 데이터를 찾지 못했습니다.";
            return;
        }

        // 같은 던전 시드와 같은 노드에서는 같은 상품 목록이 나오도록 상점 전용 스트림을 사용합니다.
        var random = CreateShopRandom();
        var usedItemIds = new HashSet<string>();
        for (int i = 0; i < ShopItemCount; i++)
        {
            ItemRecord item;
            if (TryPickShopItem(random, usedItemIds, out item))
            {
                shopItems.Add(item);
                usedItemIds.Add(item.definition.itemId);
            }
        }
    }

    // 던전 시드와 노드 ID를 이용해 상점 전용 랜덤 스트림을 만듭니다.
    private IRandomStream CreateShopRandom()
    {
        int seed = SaveGameSession.HasActiveSave && SaveGameSession.CurrentSaveData != null && SaveGameSession.CurrentSaveData.dungeon != null
            ? SaveGameSession.CurrentSaveData.dungeon.dungeonSeed
            : SeedValue.Generate();

        int nodeId = Context != null ? Context.NodeId : 0;
        return new SeedService(seed).GetStream(SeedStreamNames.Shop + "_" + nodeId);
    }

    // 희귀도를 먼저 뽑고, 해당 희귀도 후보 중 중복되지 않은 상품 하나를 선택합니다.
    private bool TryPickShopItem(IRandomStream random, HashSet<string> usedItemIds, out ItemRecord item)
    {
        item = null;

        List<RarityWeight> weights;
        if (!dataLoader.Catalog.rarityWeightTables.TryGetValue(ShopWeightTableId, out weights) || weights == null || weights.Count == 0)
        {
            return false;
        }

        // 현재 남아 있는 상품 후보가 존재하는 희귀도만 추려서 가중치 추첨합니다.
        var usableWeights = BuildUsableWeights(weights, usedItemIds);
        while (usableWeights.Count > 0)
        {
            var values = new List<float>();
            for (int i = 0; i < usableWeights.Count; i++)
            {
                values.Add(usableWeights[i].weight);
            }

            var rarityWeight = usableWeights[random.WeightedIndex(values)];
            var candidates = BuildCandidates(rarityWeight.rarityId, usedItemIds);
            if (candidates.Count > 0)
            {
                item = candidates[random.Range(0, candidates.Count)];
                return true;
            }

            usableWeights.Remove(rarityWeight);
        }

        return false;
    }

    // 현재 남은 아이템 후보가 있는 희귀도 가중치만 추려냅니다.
    private List<RarityWeight> BuildUsableWeights(List<RarityWeight> source, HashSet<string> usedItemIds)
    {
        var result = new List<RarityWeight>();
        for (int i = 0; i < source.Count; i++)
        {
            var rarityWeight = source[i];
            if (rarityWeight != null && rarityWeight.weight > 0 && BuildCandidates(rarityWeight.rarityId, usedItemIds).Count > 0)
            {
                result.Add(rarityWeight);
            }
        }

        result.Sort((left, right) => string.CompareOrdinal(left.rarityId, right.rarityId));
        return result;
    }

    // 특정 희귀도에서 아직 상점에 나오지 않은 아이템 후보 목록을 만듭니다.
    private List<ItemRecord> BuildCandidates(string rarityId, HashSet<string> usedItemIds)
    {
        var candidates = new List<ItemRecord>();
        List<ItemRecord> source;
        if (!dataLoader.Catalog.itemsByRarity.TryGetValue(rarityId, out source))
        {
            return candidates;
        }

        for (int i = 0; i < source.Count; i++)
        {
            var item = source[i];
            if (item != null && item.definition != null && !usedItemIds.Contains(item.definition.itemId))
            {
                candidates.Add(item);
            }
        }

        // 시드 랜덤 결과가 리스트 순서 차이에 흔들리지 않도록 item_id 기준으로 정렬합니다.
        candidates.Sort((left, right) => string.CompareOrdinal(left.definition.itemId, right.definition.itemId));
        return candidates;
    }

    // 카드 UI에 표시할 아이템 구매 가격을 반환합니다.
    public int GetItemPrice(ItemRecord item)
    {
        if (inventoryRuntime == null || inventoryRuntime.PriceService == null)
        {
            return 0;
        }

        return inventoryRuntime.PriceService.GetBasePriceByRarity(item != null && item.definition != null ? item.definition.rarityId : null);
    }

    // 해당 상점 슬롯이 이미 구매된 상태인지 확인합니다.
    public bool IsItemSold(int shopIndex)
    {
        return soldShopIndices.Contains(shopIndex);
    }

    // 상점 UI와 판매용 인벤토리 패널을 닫고 입력 차단을 해제합니다.
    public void CloseUI()
    {
        isOpen = false;
        isSellMode = false;
        HideInventoryPanel();
        DungeonUiInputBlocker.SetBlocked(this, false);
    }

    // 기존 인벤토리 패널을 상점 판매 슬롯 선택 모드로 엽니다.
    private void ShowInventorySellPanel()
    {
        // 임시 상점 UI와 별개로, 실제 슬롯 선택은 기존 인벤토리 디버그 패널을 재사용합니다.
        var panel = FindFirstObjectByType<InventoryDebugPanelBehaviour>(FindObjectsInactive.Include);
        if (panel != null)
        {
            panel.ShowShopSellPanel(ReturnToShop, null, false);
        }
    }

    // 열려 있는 인벤토리 패널을 닫습니다.
    private void HideInventoryPanel()
    {
        var panel = FindFirstObjectByType<InventoryDebugPanelBehaviour>(FindObjectsInactive.Include);
        if (panel != null)
        {
            panel.HidePanel();
        }
    }
}
