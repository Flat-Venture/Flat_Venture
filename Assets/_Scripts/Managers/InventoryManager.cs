using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditorInternal.Profiling.Memory.Experimental;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    public ItemData[] inventory;                            // 인벤토리 아이템 배열
    int itemCount = 0;                                      // 인벤토리 아이템 수

    int currentIndex = 0;                                   // 다음 아이템이 들어갈 위치 인덱스
    int lineSize = 3;                                       // 한 줄에 있는 슬롯 개수
    public SortedDictionary<Enums.ItemSynergy, int> synergyCount;        // 시너지 효과 카운트 (키: 시너지 종류, 값: 시너지 개수)
    public SortedDictionary<Enums.ItemSynergy, int> synergyActiveCount;  // 시너지 활성화 개수 (키: 시너지 종류, 값: 활성화된 시너지 개수)
    public Dictionary<int, int> reinforcedSlots;            // 강화된 슬롯 카운트 (키: 슬롯 인덱스, 값: 강화 레벨)
    Dictionary<int, int> indexByItemId;                     // 아이템 ID로 슬롯 인덱스 찾기 위한 딕셔너리
    List<int[]> lineCheck;                                  // 줄 별 시너지 효과가 완성되었는지 확인하기 위한 2차원 배열

    UIManager uiManager;
    ItemManager itemManager;
    SynergyManager synergyManager;

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

        inventory = new ItemData[9];
        synergyCount = new SortedDictionary<Enums.ItemSynergy, int>();
        synergyActiveCount = new SortedDictionary<Enums.ItemSynergy, int>();

        indexByItemId = new Dictionary<int, int>();
        GenerateLineCheck();
        InitReinforceSlots();
        InitSynergyActiveCount();
    }

    void Start()
    {
        uiManager = UIManager.Instance;
        itemManager = ItemManager.Instance;
        synergyManager = SynergyManager.Instance;
    }

    // 강화슬롯 초기화
    void InitReinforceSlots()
    {
        reinforcedSlots = new Dictionary<int, int>();
        for (int i = 0; i < inventory.Length; i++)
        {
            reinforcedSlots[i] = 0;
        }
    }

    // 시너지 활성화 딕셔너리 초기화
    void InitSynergyActiveCount()
    {
        // 미리 모든 아이템 시너지를 Key로 만들어둠
        // 아이템 효과 중 무작위 시너지 효과 레벨 +1 등이 생길 때 사용하기 위해서
        foreach (Enums.ItemSynergy type in Enum.GetValues(typeof(Enums.ItemSynergy)))
        {
            synergyActiveCount[type] = 0;
        }

    }

    // 라인 체크 배열 생성
    void GenerateLineCheck()
    {
        lineCheck = new List<int[]>();

        // 가로 줄 체크 배열 생성
        for (int i = 0; i < lineSize; i++)
        {
            int[] row = new int[lineSize];
            for (int j = 0; j < lineSize; j++)
            {
                row[j] = i * lineSize + j;
            }
            lineCheck.Add(row);
        }

        // 세로 줄 체크 배열 생성
        for (int i = 0; i < lineSize; i++)
        {
            int[] column = new int[lineSize];
            for (int j = 0; j < lineSize; j++)
            {
                column[j] = j * lineSize + i;
            }
            lineCheck.Add(column);
        }

        // 좌상 -> 우하 대각선 배열 생성
        int[] diagonal1 = new int[lineSize];
        for (int i = 0; i < lineSize; i++)
        {
            diagonal1[i] = i * (lineSize + 1);
        }
        lineCheck.Add(diagonal1);

        // 우상 -> 좌하 대각선 배열 생성
        int[] diagonal2 = new int[lineSize];
        for (int i = 0; i < lineSize; i++)
        {
            diagonal2[i] = (i + 1) * (lineSize - 1);
        }
        lineCheck.Add(diagonal2);
    }

    // 인벤토리에 아이템 추가
    public void AddItemToInventory(ItemData newItem)
    {
        if (newItem == null)
        {
            return;
        }

        if (CheckInventoryIsFull())
        {
            Debug.Log("인벤토리 가득 참");
            return;
        }

        if (CheckDuplicateItems(newItem.iId))
        {
            Debug.Log("이미 인벤토리에 있는 아이템");
            return;
        }

        inventory[currentIndex] = newItem;
        indexByItemId[newItem.iId] = currentIndex;

        // 아이템 추가할 때, 해당 아이템의 시너지를 저장
        for (int i = 0; i < newItem.iSynergy.Count; i++)
        {
            if (newItem.iSynergy[i] != Enums.ItemSynergy.None)
            {
                if (!synergyCount.ContainsKey(newItem.iSynergy[i]))
                {
                    synergyCount[newItem.iSynergy[i]] = 0;
                }
                synergyCount[newItem.iSynergy[i]]++;
            }
        }

        // 아이템 시너지 카운트 업데이트
        CheckActiveSynergy();

        if (Player.Instance != null)
        {
            float tempHp = Player.Instance.finalStats.maxHp;
            Player.Instance.SetFinalStat();
            Player.Instance.Heal(Player.Instance.finalStats.maxHp - tempHp);
        }

        // 아이템 획득 시 시너지 효과 업데이트
        uiManager.playerStatUIController.synergyEffectUIController.ReturnSynergySlot();
        uiManager.playerStatUIController.synergyEffectUIController.ShowSynergyEffect();

        // currentIndex 다시 설정
        for (int i = 0; i < inventory.Length; i++)
        {
            if (inventory[i] == null)
            {
                currentIndex = i;
                break;
            }
        }
        // 아이템 개수 증가
        itemCount++;

        if (currentIndex >= inventory.Length)
        {
            currentIndex--;
        }

        uiManager.inventoryUIController.UpdateItemIcon();
        uiManager.playerStatUIController.UpdatePlayerStatUI();
    }

    // 인벤토리에 아이템 추가 인덱스 기반 (스왑할 때 사용)
    public void AddItemToInventoryByIndex(int index, ItemData newItem)
    {
        if(newItem == null)
        {
            return;
        }

        if (CheckInventoryIsFull())
        {
            Debug.Log("인벤토리 가득 참");
            return;
        }

        if (CheckDuplicateItems(newItem.iId))
        {
            Debug.Log("이미 인벤토리에 있는 아이템");
            return;
        }

        inventory[index] = newItem;
        if (newItem != null)
        {
            indexByItemId[newItem.iId] = index;

            // 아이템 추가할 때, 해당 아이템의 시너지를 저장
            for (int i = 0; i < newItem.iSynergy.Count; i++)
            {
                if (newItem.iSynergy[i] != Enums.ItemSynergy.None)
                {
                    if (!synergyCount.ContainsKey(newItem.iSynergy[i]))
                    {
                        synergyCount[newItem.iSynergy[i]] = 0;
                    }
                    synergyCount[newItem.iSynergy[i]]++;
                }
            }
        }

        // 아이템 시너지 카운트 업데이트
        CheckActiveSynergy();

        if (Player.Instance != null)
        {
            float tempHp = Player.Instance.finalStats.maxHp;
            Player.Instance.SetFinalStat();
            Player.Instance.Heal(Player.Instance.finalStats.maxHp - tempHp);
        }

        // 아이템 획득 시 시너지 효과 업데이트
        uiManager.playerStatUIController.synergyEffectUIController.ReturnSynergySlot();
        uiManager.playerStatUIController.synergyEffectUIController.ShowSynergyEffect();

        // 아이템 개수 증가
        itemCount++;

        for (int i = 0; i < inventory.Length; i++)
        {
            if (inventory[i] == null)
            {
                currentIndex = i;
                break;
            }
        }
        uiManager.inventoryUIController.UpdateItemIcon();
        uiManager.playerStatUIController.UpdatePlayerStatUI();
    }

    // 인벤토리 아이템 최종 스탯 반환
    public Stats GetInventoryTotalStats()
    {
        Stats totalStats = new Stats();
        for (int i = 0; i < inventory.Length; i++)
        {
            var itemData = inventory[i];
            if (itemData != null)
            {
                if (reinforcedSlots.ContainsKey(i) && inventory[i] != null)
                {
                    totalStats += itemData.iBaseStat + itemData.iBonusStat * reinforcedSlots[i];
                }
            }
        }

        List<Enums.ItemSynergy> activeSyenrgies = synergyActiveCount
                        .Where(k => k.Value > 0).Select(k => k.Key).ToList();
        for (int i = 0; i < activeSyenrgies.Count; i++)
        {
            totalStats += synergyManager.GetSynergyData(activeSyenrgies[i])
                .GetBonusStats(synergyActiveCount[activeSyenrgies[i]]);
        }
        return totalStats;
    }

    // 인벤토리 슬롯 강화
    public void ReinforceInventorySlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= inventory.Length)
        {
            Debug.LogError("인벤토리 슬롯 범위를 벗어남");
            return;
        }

        int level = UnityEngine.Random.Range(1, 3);

        // 강화 수치도 랜덤하게 올라가고 싶으면 여기 수정
        reinforcedSlots[slotIndex] += level;
        uiManager.inventoryUIController.UpdateItemIcon();
        uiManager.playerStatUIController.UpdatePlayerStatUI();
    }

    // 인벤토리 아이템 제거 (버리기 버튼을 통해 호출)
    public void RemoveItemFromInventory(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= inventory.Length)
        {
            Debug.LogError("인벤토리 슬롯 범위를 벗어남");
            return;
        }
        if (inventory[slotIndex] == null)
        {
            Debug.LogWarning("해당 슬롯에 아이템이 없음");
            return;
        }

        // 시너지 개수 카운트 줄여주고 0이면 제거
        for (int i = 0; i < inventory[slotIndex].iSynergy.Count; i++)
        {
            if (inventory[slotIndex].iSynergy[i] != Enums.ItemSynergy.None)
            {
                if (synergyCount.ContainsKey(inventory[slotIndex].iSynergy[i]))
                {
                    synergyCount[inventory[slotIndex].iSynergy[i]]--;
                }
                if (synergyCount[inventory[slotIndex].iSynergy[i]] == 0)
                {
                    synergyCount.Remove(inventory[slotIndex].iSynergy[i]);
                }
            }
        }

        indexByItemId.Remove(inventory[slotIndex].iId);
        inventory[slotIndex] = null;

        // 아이템 시너지 카운트 업데이트
        CheckActiveSynergy();

        if (Player.Instance != null)
        {
            Player.Instance.SetFinalStat();
            Player.Instance.Heal(0f);
        }

        // 아이템 제거 시, 시너지 효과 패널 업데이트
        uiManager.playerStatUIController.synergyEffectUIController.ReturnSynergySlot();
        uiManager.playerStatUIController.synergyEffectUIController.ShowSynergyEffect();

        itemCount--;

        if (currentIndex >= slotIndex)
        {
            currentIndex = slotIndex;
        }
        uiManager.inventoryUIController.UpdateItemIcon();
        uiManager.playerStatUIController.UpdatePlayerStatUI();
    }

    // 아이템 중복 체크
    public bool CheckDuplicateItems(int itemId)
    {
        if (indexByItemId.ContainsKey(itemId)) {
            return true;
        }
        else
        {
            return false;
        }
    }

    // 시너지 빙고 확인
    public void CheckActiveSynergy()
    {
        // 재계산 전에 모든 시너지 카운트를 0으로 초기화
        var synergyTypes = synergyActiveCount.Keys.ToList();
        foreach (var type in synergyTypes)
        {
            synergyActiveCount[type] = 0;
        }

        foreach (int[] line in lineCheck)
        {
            // 첫 번째 슬롯에 아이템이 없거나, 첫 번째 아이템에 시너지가 없으면 다음 라인 확인
            if (inventory[line[0]] == null
                || inventory[line[0]].iSynergy == null
                || inventory[line[0]].iSynergy.Count == 0)
            {
                continue;
            }

            // 첫 번째 슬롯에 있는 아이템의 모든 시너지를 HashSet으로 설정
            HashSet<Enums.ItemSynergy> checkSynergy = inventory[line[0]].iSynergy
                .Where(x => x != Enums.ItemSynergy.None)
                .ToHashSet();

            // 시너지가 없다면 다음 라인 확인
            if (checkSynergy.Count == 0)
            {
                continue;
            }

            // 나머지 슬롯을 순회하며 교집합 업데이트
            bool isLineEmpty = false;
            for (int i = 1; i < line.Length; i++)
            {
                int index = line[i];

                // 아이템이 비어있거나 시너지가 비어있으면 실패
                if (inventory[index] == null
                || inventory[index].iSynergy == null
                || inventory[index].iSynergy.Count == 0)
                {
                    isLineEmpty = true;
                    break;
                }

                // 현재 슬롯에 있는 아이템의 모든 시너지를 HashSet을 생성
                HashSet<Enums.ItemSynergy> currentItemSynergy = inventory[index].iSynergy
                    .Where(x => x != Enums.ItemSynergy.None)
                    .ToHashSet();

                // 첫 번째 슬롯의 아이템 시너지와 교집합 연산
                checkSynergy.IntersectWith(currentItemSynergy);

                // 교집합 결과가 비어있다면, 공통 시너지 존재하지 않음
                if (checkSynergy.Count == 0)
                {
                    break;
                }
            }

            // 아이템이 비어있으면 다음 라인으로
            if (isLineEmpty) continue;

            // 남은 공통 시너지 활성화
            foreach (var synergy in checkSynergy)
            {
                synergyActiveCount[synergy]++;
            }

        }
    }

    // 인벤토리 아이템 위치 변환
    public void SwapItem(int index1, int index2)
    {
        ItemData temp = inventory[index1];
        ItemData temp2 = inventory[index2];

        if (temp == null && temp2 == null)
        {
            return;
        }

        RemoveItemFromInventory(index1);
        RemoveItemFromInventory(index2);

        AddItemToInventoryByIndex(index2, temp);
        AddItemToInventoryByIndex(index1, temp2);

        // 스왑 후 시너지 빙고 확인
        CheckActiveSynergy();

        // 인벤토리 슬롯 이미지 업데이트
        uiManager.inventoryUIController.UpdateItemIcon();
        uiManager.playerStatUIController.UpdatePlayerStatUI();
    }

    // 아이템 등급 업 => 랜덤 아이템으로
    public void RankUpItem(int index)
    {
        if (inventory[index] == null)
        {
            Debug.Log("아무것도 없다");
            return;
        }
        if (inventory[index].iRank == Enums.ItemRank.Legendary)
        {
            Debug.Log("이미 최고 등급");
            return;
        }

        int currentRankValue = (int)inventory[index].iRank;
        int nextRankValue = currentRankValue + 1;
        Type rankEnumType = typeof(Enums.ItemRank);

        if (Enum.IsDefined(rankEnumType, nextRankValue))
        {
            Enums.ItemRank nextRank = (Enums.ItemRank)nextRankValue;

            if (itemManager.itemDictionaryForRank.ContainsKey(nextRank)) {

                // 현재 등급 다음 단계의 아이템 리스트를 가져옴 (깊은 복사)
                List<ItemData> nextRankItemList = new List<ItemData>(itemManager.itemDictionaryForRank[nextRank]);

                // 해당 등급 중 랜덤한 아이템 가져옴
                int randomIndex = UnityEngine.Random.Range(0, nextRankItemList.Count);

                // 중복 아이템이 나오지 않을때까지 반복
                while (CheckDuplicateItems(nextRankItemList[randomIndex].iId))
                {
                    // 만약 중복 아이템이 나오면 그 아이템을 리스트에서 제거
                    nextRankItemList.RemoveAt(randomIndex);

                    // 다음 랭크 아이템을 모두 가지고 있을 때, 강제종료
                    if (nextRankItemList.Count == 0)
                    {
                        Debug.Log("남은 아이템이 없음");
                        return;
                    }

                    // 랜덤인덱스 재설정
                    randomIndex = UnityEngine.Random.Range(0, nextRankItemList.Count);
                }

                RemoveItemFromInventory(index);
                AddItemToInventoryByIndex(index, nextRankItemList[randomIndex]);

                // 아이템 등급 업 후 시너지 체크
                CheckActiveSynergy();

                // 인벤토리 슬롯 이미지 업데이트
                uiManager.inventoryUIController.UpdateItemIcon();
                uiManager.playerStatUIController.UpdatePlayerStatUI();
            }
        }
    }

    // 아이템 등급 업 => 시너지 효과 유지한 채로 (시너지가 2개 이상이면 둘 중 하나라도 유지)
    public void RankUpItemWithSynergy(int index)
    {
        if (inventory[index] == null)
        {
            Debug.Log("아무것도 없다");
            return;
        }
        if (inventory[index].iRank == Enums.ItemRank.Legendary)
        {
            Debug.Log("이미 최고 등급");
            return;
        }

        int currentRankValue = (int)inventory[index].iRank;
        int nextRankValue = currentRankValue + 1;
        Type rankEnumType = typeof(Enums.ItemRank);

        if (Enum.IsDefined(rankEnumType, nextRankValue))
        {
            Enums.ItemRank nextRank = (Enums.ItemRank)nextRankValue;

            if (itemManager.itemDictionaryForRank.ContainsKey(nextRank)) {

                // 현재 등급 다음 단계의 아이템 리스트를 가져옴 (깊은 복사)
                List<ItemData> rankUpItemList = new List<ItemData>(itemManager.itemDictionaryForRank[nextRank]);

                List<HashSet<ItemData>> synergyItemList = new List<HashSet<ItemData>>();

                // 현재 인덱스의 있는 시너지를 가진 아이템 리스트를 HashSet으로 가져옴
                foreach (Enums.ItemSynergy synergy in inventory[index].iSynergy)
                {
                    synergyItemList.Add(itemManager.itemDictionaryForSynergy[synergy].ToHashSet());
                }

                // 다음 등급의 아이템 리스트와 현재 시너지의 아이템 리스트의 교집합을 구함
                foreach (var itemList in synergyItemList)
                {
                    itemList.IntersectWith(rankUpItemList);
                }

                HashSet<ItemData> finalItemHashSet = new HashSet<ItemData>();

                // 해당 교집합을 합하면 다음 등급의 아이템과 현재 아이템의 시너지를 이어받는 아이템 리스트가 나옴
                foreach (var itemList in synergyItemList)
                {
                    finalItemHashSet.UnionWith(itemList);
                }

                List<ItemData> finalItemList = finalItemHashSet.ToList();

                // 최종 리스트에서 랜덤한 아이템 가져옴
                int randomIndex = UnityEngine.Random.Range(0, finalItemList.Count);

                // 중복 아이템이 나오지 않을때까지 반복
                while (CheckDuplicateItems(finalItemList[randomIndex].iId))
                {
                    // 중복 아이템을 리스트에서 제거
                    finalItemList.RemoveAt(randomIndex);

                    // 다음 랭크 아이템을 모두 가지고 있을 때, 강제종료
                    if (finalItemList.Count == 0)
                    {
                        Debug.Log("남은 아이템이 없음");
                        return;
                    }

                    // 랜덤 인덱스 재설정
                    randomIndex = UnityEngine.Random.Range(0, finalItemList.Count);
                }

                RemoveItemFromInventory(index);
                AddItemToInventoryByIndex(index, finalItemList[randomIndex]);

                // 아이템 등급 업 후 시너지 체크
                CheckActiveSynergy();

                // 인벤토리 슬롯 이미지 업데이트
                uiManager.inventoryUIController.UpdateItemIcon();
                uiManager.playerStatUIController.UpdatePlayerStatUI();
            }
        }
    }

    public bool CheckInventoryIsFull()
    {
        if (itemCount < inventory.Length)
        {
            return false;
        }
        return true;
    }

    public void ClearInventory()
    {
        inventory = new ItemData[9];
        itemCount = 0;
        currentIndex = 0;
        synergyCount = new SortedDictionary<Enums.ItemSynergy, int>();
        synergyActiveCount = new SortedDictionary<Enums.ItemSynergy, int>();
        reinforcedSlots = new Dictionary<int, int>();
        indexByItemId = new Dictionary<int, int>();

        ModeManager.Instance.SetMode(Enums.InventoryMode.None);
        InitReinforceSlots();
        InitSynergyActiveCount();

        uiManager.playerStatUIController.synergyEffectUIController.ReturnSynergySlot();
        uiManager.synergyDescriptionUIController.HideSynergyDescription();
        uiManager.inventoryUIController.UpdateItemIcon();
    }
}