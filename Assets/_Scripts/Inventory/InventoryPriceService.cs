using FlatVenture.ItemData;
using UnityEngine;

namespace FlatVenture.Inventory
{
    // 희귀도 CSV를 기준으로 아이템 구매/판매 가격을 계산합니다.
    public sealed class InventoryPriceService
    {
        private readonly GameDataCatalog catalog;

        // 가격 계산에 사용할 게임 데이터 카탈로그를 보관합니다.
        public InventoryPriceService(GameDataCatalog catalog)
        {
            this.catalog = catalog;
        }

        // 희귀도 기본 가격을 반환합니다. CSV 값이 없으면 경고 후 0을 반환합니다.
        public int GetBasePriceByRarity(string rarityId)
        {
            int price;
            return TryGetBasePriceByRarity(rarityId, out price) ? price : 0;
        }

        // 상점 판매 가격을 계산합니다.
        public int CalculateShopSellPrice(InventoryItem item)
        {
            if (item == null)
            {
                return 0;
            }

            RarityDefinition rarity;
            if (!TryGetRarityDefinition(item.rarityId, out rarity))
            {
                return 0;
            }

            int basePrice;
            if (!TryGetBasePriceByRarity(item.rarityId, out basePrice))
            {
                return 0;
            }

            if (rarity.sellRate <= 0f)
            {
                Debug.LogWarning("[InventoryPriceService] sell_rate가 0 이하입니다: " + item.rarityId);
                return 0;
            }

            // sellRate는 rarity_definitions.csv에서 관리합니다.
            return Mathf.RoundToInt(basePrice * rarity.sellRate);
        }

        // 희귀도 정의에서 base_price를 안전하게 읽어옵니다.
        private bool TryGetBasePriceByRarity(string rarityId, out int price)
        {
            price = 0;

            RarityDefinition rarity;
            if (!TryGetRarityDefinition(rarityId, out rarity))
            {
                return false;
            }

            if (rarity.basePrice <= 0)
            {
                Debug.LogWarning("[InventoryPriceService] base_price가 없거나 0 이하입니다: " + rarityId);
                return false;
            }

            price = rarity.basePrice;
            return true;
        }

        // 희귀도 ID로 RarityDefinition을 찾고 실패 사유를 로그로 남깁니다.
        private bool TryGetRarityDefinition(string rarityId, out RarityDefinition rarity)
        {
            rarity = null;
            if (string.IsNullOrEmpty(rarityId))
            {
                Debug.LogWarning("[InventoryPriceService] rarityId가 비어 있습니다.");
                return false;
            }

            if (catalog == null || catalog.rarities == null)
            {
                Debug.LogWarning("[InventoryPriceService] 희귀도 카탈로그가 없습니다.");
                return false;
            }

            if (!catalog.rarities.TryGetValue(rarityId, out rarity))
            {
                Debug.LogWarning("[InventoryPriceService] 희귀도 정의를 찾지 못했습니다: " + rarityId);
                return false;
            }

            return true;
        }
    }
}
