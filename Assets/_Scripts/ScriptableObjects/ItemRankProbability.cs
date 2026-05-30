using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemRankProbability", menuName = "Scriptable Objects/ItemRankProbability")]
public class ItemRankProbability : ScriptableObject
{
    [System.Serializable]
    public class ItemRankWeight
    {
        public Enums.ItemRank rank;             // 아이템 등급
        public int weight;                      // 아이템 가중치
    }

    public List<ItemRankWeight> rankWeights = new List<ItemRankWeight>();

    // 인스펙터 순서가 섞여도 Rank에 따라 정렬된 리스트 반환
    public List<ItemRankWeight> GetSortedWeight()
    {
        // 해시셋 변환으로 중복 제거
        HashSet<ItemRankWeight> weightSet = new HashSet<ItemRankWeight>(rankWeights.ToHashSet());
        List<ItemRankWeight> distinctWeights = weightSet.ToList();
        return distinctWeights.OrderBy(x => (int)x.rank).ToList();
    }
}
