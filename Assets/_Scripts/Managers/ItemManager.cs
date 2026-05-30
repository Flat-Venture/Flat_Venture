using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 1. 모든 아이템의 정보를 가지고 있는 매니저
/// </summary>
public class ItemManager : MonoBehaviour
{
    public static ItemManager Instance;

    // 모든 아이템 정보를 가지고 있는 SO를 이용
    [SerializeField]
    AllItemsData allItemDatas;

    // 아이템 등급 확률을 가지고 있는 SO 이용
    [SerializeField]
    ItemRankProbability itemRankProbability;

    // 아이템 데이터를 ID로 편하게 찾기위한 Dictionary
    Dictionary<int, ItemData> itemDictionary = new Dictionary<int, ItemData>();

    // 아이템 등급별 리스트 Dictionary
    public Dictionary<Enums.ItemRank, List<ItemData>> itemDictionaryForRank 
        = new Dictionary<Enums.ItemRank, List<ItemData>>();

    // 아이템 시너지 별 리스트 Dictionary
    public Dictionary<Enums.ItemSynergy, List<ItemData>> itemDictionaryForSynergy
        = new Dictionary<Enums.ItemSynergy, List<ItemData>>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // 아이템 딕셔너리  채워주기
        itemDictionary = allItemDatas.allItems.ToDictionary(x => x.iId, x => x);

        itemDictionaryForRank = allItemDatas.allItems
            .GroupBy(x => x.iRank)
            .ToDictionary(
                group => group.Key,
                group => group.ToList()
            );

        // 아이템 시너지가 리스트 이므로 시너지 리스트를 평탄화 하여 새 컬렉션을 만듦
        // 리스트 평탄화: [불, 얼음], [물, 얼음], [불, 물] => [불, 얼음, 물, 얼음, 불, 물] 
        // 중첩 리스트에서 단일 리스트로 변경 후 원래 아이템과 시너지 타입을 묶은 익명 객체 생성
        itemDictionaryForSynergy = allItemDatas.allItems
            .SelectMany(item => item.iSynergy.Select(synergy => new {Item = item, Syenrgy = synergy}))
            .GroupBy(pair => pair.Syenrgy)
            .ToDictionary(
               group => group.Key,
               group => group.Select(pair => pair.Item).ToList()
            );        
    }

    // ItemId를 통한 ItemData 반환
    public ItemData GetItemData(int itemId)
    {
        if( !itemDictionary.TryGetValue(itemId, out var data))
        {
            Debug.LogError("ItemData를 찾을 수 없음");
            return null;
        }
        return data;
    }

    // 모든 아이템 정보 리턴
    public List<ItemData> GetAllItem()
    {
        return allItemDatas.allItems;
    }

    // 확률에 의한 등급 랜덤 반환
    public Enums.ItemRank GetRandomItemRank()
    {
        // 아이템 등급 확률이 없거나 아이템 Rank enum의 길이와 등록되 등급 확률 길이가 다를 때,
        if (itemRankProbability == null || itemRankProbability.GetSortedWeight().Count 
            != System.Enum.GetValues(typeof(Enums.ItemRank)).Length)
        {
            Debug.Log("테이블 수정 부탁");
            return Enums.ItemRank.Common;
        }

        var sortedWeights = itemRankProbability.GetSortedWeight();

        // 전체 가중치 합
        int totalWeight = 0;
        foreach (var rankWeight in sortedWeights)
        {
            totalWeight += rankWeight.weight + Player.Instance.finalStats.luck;
        }

        // 0 ~ 전체 가중치 합 사이 랜덤한 숫자
        int randNum = UnityEngine.Random.Range(0, totalWeight);
        Debug.Log(randNum);
        int currentWeight = 0;
        foreach (var rankWeight in sortedWeights)
        {
            currentWeight += rankWeight.weight;
            if (randNum < currentWeight)
            {
                return rankWeight.rank;
            }
            
        }
        return Enums.ItemRank.Common;
    }

    // 해당 랭크에서 랜덤 아이템 반환
    public ItemData GetRandomItemDataByRank(Enums.ItemRank rank)
    {
        // 깊은 복사를 통해서 rank 리스트를 생성
        List<ItemData> rankItemList = new List<ItemData>(itemDictionaryForRank[rank]);

        // 해당 리스트 중 랜덤한 아이템 반환
        int randomIndex = UnityEngine.Random.Range(0, rankItemList.Count);
        return rankItemList[randomIndex];
    }
}
