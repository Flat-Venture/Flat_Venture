using System;
using System.Collections.Generic;
using FlatVenture.Enums;
using FlatVenture.Inventory;
using FlatVenture.ItemData;
using FlatVenture.NUH.Seed;

namespace FlatVenture.Reward
{
    // 던전 방 타입과 CSV 희귀도 가중치를 기준으로 보상 후보 아이템을 생성합니다.
    // 같은 던전 시드와 같은 노드에서는 같은 보상 후보가 나오도록 노드별 보상 스트림을 사용합니다.
    public sealed class DungeonRewardService
    {
        private const int RewardChoiceCount = 3;
        private const string NormalRewardTableId = "normal_reward";
        private const string EliteRewardTableId = "elite_reward";
        private const string BossRewardTableId = "boss_reward";

        private readonly GameDataCatalog catalog;

        // 로드된 게임 데이터 카탈로그를 받아 보상 후보 생성에 사용합니다.
        public DungeonRewardService(GameDataCatalog catalog)
        {
            this.catalog = catalog;
        }

        // 방 타입에 맞는 보상 3개를 생성합니다.
        // 보상 후보는 포탈 생성 이후에 다시 열릴 수 있어야 하므로 저장 데이터에는 넣지 않습니다.
        public bool TryCreateRewards(RoomType roomType, int dungeonSeed, int nodeId, bool allowCursed, out List<ItemRecord> rewards, out string message)
        {
            rewards = new List<ItemRecord>();
            message = null;

            if (catalog == null)
            {
                message = "아이템 데이터가 로드되지 않았습니다.";
                return false;
            }

            string tableId = GetRewardTableId(roomType);
            List<RarityWeight> rarityWeights;
            if (!catalog.rarityWeightTables.TryGetValue(tableId, out rarityWeights) || rarityWeights == null || rarityWeights.Count == 0)
            {
                message = "보상 희귀도 테이블이 없습니다: " + tableId;
                return false;
            }

            // 보상 후보 자체는 던전 시드와 노드 ID로 고정합니다.
            // 단, 인벤토리에 들어가는 위치는 별도 정책에 따라 비시드 랜덤을 사용합니다.
            var random = new SeedService(NormalizeSeed(dungeonSeed)).GetStream("Reward_" + nodeId);
            var usedItemIds = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < RewardChoiceCount; i++)
            {
                ItemRecord reward;
                if (!TryPickRewardItem(rarityWeights, random, allowCursed, usedItemIds, out reward))
                {
                    message = "보상 후보 아이템이 부족합니다.";
                    return rewards.Count > 0;
                }

                rewards.Add(reward);
                usedItemIds.Add(reward.definition.itemId);
            }

            return rewards.Count > 0;
        }

        // 보상 후보 하나를 희귀도 가중치와 아이템 후보 목록에서 선택합니다.
        private bool TryPickRewardItem(List<RarityWeight> rarityWeights, IRandomStream random, bool allowCursed, HashSet<string> usedItemIds, out ItemRecord reward)
        {
            reward = null;

            // 후보가 실제로 존재하는 희귀도만 남긴 뒤, CSV weight 값으로 희귀도를 먼저 뽑습니다.
            var validWeights = BuildUsableRarityWeights(rarityWeights, allowCursed, usedItemIds);
            while (validWeights.Count > 0)
            {
                var weights = new List<float>(validWeights.Count);
                for (int i = 0; i < validWeights.Count; i++)
                {
                    weights.Add(validWeights[i].weight);
                }

                var rarityWeight = validWeights[random.WeightedIndex(weights)];
                // 뽑힌 희귀도 안에서 중복/저주 조건을 통과한 아이템 중 하나를 선택합니다.
                var candidates = BuildItemCandidates(rarityWeight.rarityId, allowCursed, usedItemIds);
                if (candidates.Count > 0)
                {
                    reward = candidates[random.Range(0, candidates.Count)];
                    return true;
                }

                validWeights.Remove(rarityWeight);
            }

            return false;
        }

        // 실제로 선택 가능한 아이템이 있는 희귀도 가중치만 반환합니다.
        private List<RarityWeight> BuildUsableRarityWeights(List<RarityWeight> source, bool allowCursed, HashSet<string> usedItemIds)
        {
            var result = new List<RarityWeight>();
            for (int i = 0; i < source.Count; i++)
            {
                var rarityWeight = source[i];
                if (rarityWeight == null || rarityWeight.weight <= 0)
                {
                    continue;
                }

                if (BuildItemCandidates(rarityWeight.rarityId, allowCursed, usedItemIds).Count > 0)
                {
                    result.Add(rarityWeight);
                }
            }

            // Dictionary/CSV 로드 순서 차이로 시드 결과가 흔들리지 않도록 정렬합니다.
            result.Sort(CompareRarityWeight);
            return result;
        }

        // 특정 희귀도에서 중복/저주 조건을 통과한 아이템 후보를 만듭니다.
        private List<ItemRecord> BuildItemCandidates(string rarityId, bool allowCursed, HashSet<string> usedItemIds)
        {
            var candidates = new List<ItemRecord>();

            List<ItemRecord> source;
            if (!catalog.itemsByRarity.TryGetValue(rarityId, out source) || source == null)
            {
                return candidates;
            }

            for (int i = 0; i < source.Count; i++)
            {
                var item = source[i];
                if (item == null || item.definition == null)
                {
                    continue;
                }

                if (usedItemIds.Contains(item.definition.itemId))
                {
                    continue;
                }

                // 저주 아이템은 플레이어가 이미 저주 아이템을 가진 뒤부터 보상 후보에 포함합니다.
                if (item.definition.isCursed && !allowCursed)
                {
                    continue;
                }

                candidates.Add(item);
            }

            // 같은 시드에서 같은 후보가 나오도록 item_id 기준으로 순서를 고정합니다.
            candidates.Sort(CompareItemId);
            return candidates;
        }

        // 방 타입에 맞는 희귀도 가중치 테이블 ID를 반환합니다.
        private static string GetRewardTableId(RoomType roomType)
        {
            switch (roomType)
            {
                case RoomType.Elite:
                    return EliteRewardTableId;
                case RoomType.Boss:
                    return BossRewardTableId;
                default:
                    return NormalRewardTableId;
            }
        }

        // 유효하지 않은 시드라면 임시 시드를 생성해 랜덤 스트림을 만들 수 있게 합니다.
        private static int NormalizeSeed(int seed)
        {
            return SeedValue.IsValid(seed) ? seed : SeedValue.Generate();
        }

        // item_id 기준으로 보상 후보 순서를 고정합니다.
        private static int CompareItemId(ItemRecord left, ItemRecord right)
        {
            string leftId = left != null && left.definition != null ? left.definition.itemId : string.Empty;
            string rightId = right != null && right.definition != null ? right.definition.itemId : string.Empty;
            return string.CompareOrdinal(leftId, rightId);
        }

        // rarity_id 기준으로 희귀도 가중치 순서를 고정합니다.
        private static int CompareRarityWeight(RarityWeight left, RarityWeight right)
        {
            string leftId = left != null ? left.rarityId : string.Empty;
            string rightId = right != null ? right.rarityId : string.Empty;
            return string.CompareOrdinal(leftId, rightId);
        }
    }
}
