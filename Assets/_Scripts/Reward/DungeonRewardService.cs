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

        public DungeonRewardService(GameDataCatalog catalog)
        {
            this.catalog = catalog;
        }

        // 방 타입에 맞는 보상 3개를 생성합니다.
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

        private bool TryPickRewardItem(List<RarityWeight> rarityWeights, IRandomStream random, bool allowCursed, HashSet<string> usedItemIds, out ItemRecord reward)
        {
            reward = null;

            var validWeights = BuildUsableRarityWeights(rarityWeights, allowCursed, usedItemIds);
            while (validWeights.Count > 0)
            {
                var weights = new List<float>(validWeights.Count);
                for (int i = 0; i < validWeights.Count; i++)
                {
                    weights.Add(validWeights[i].weight);
                }

                var rarityWeight = validWeights[random.WeightedIndex(weights)];
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

            result.Sort(CompareRarityWeight);
            return result;
        }

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

                if (item.definition.isCursed && !allowCursed)
                {
                    continue;
                }

                candidates.Add(item);
            }

            candidates.Sort(CompareItemId);
            return candidates;
        }

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

        private static int NormalizeSeed(int seed)
        {
            return SeedValue.IsValid(seed) ? seed : SeedValue.Generate();
        }

        private static int CompareItemId(ItemRecord left, ItemRecord right)
        {
            string leftId = left != null && left.definition != null ? left.definition.itemId : string.Empty;
            string rightId = right != null && right.definition != null ? right.definition.itemId : string.Empty;
            return string.CompareOrdinal(leftId, rightId);
        }

        private static int CompareRarityWeight(RarityWeight left, RarityWeight right)
        {
            string leftId = left != null ? left.rarityId : string.Empty;
            string rightId = right != null ? right.rarityId : string.Empty;
            return string.CompareOrdinal(leftId, rightId);
        }
    }
}
