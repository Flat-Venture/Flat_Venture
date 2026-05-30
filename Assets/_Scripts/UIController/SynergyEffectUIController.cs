using System.Collections.Generic;
using UnityEngine;

public class SynergyEffectUIController : MonoBehaviour
{
    [Header("시너지 보유 UI 정보")]
    public GameObject synergyEffectUIPanel;             // 시너지 효과 설명 패널

    public Transform synergyEffectScroll;               // 시너지 효과 슬롯 프리팹의 부모
    public SynergySlotUI synergySlot;                   // 시너지 슬롯 프리팹
    public List<SynergySlotUI> synergySlotUIList;       // 활성화 되어있는 시너지 슬롯 UI를 저장하는 리스트

    PoolManager poolManager;
    InventoryManager inventoryManager;
    SynergyManager synergyManager;

    private void Awake()
    {
        poolManager = PoolManager.Instance;
        inventoryManager = InventoryManager.Instance;
        synergyManager = SynergyManager.Instance;

        poolManager.CreatePool<SynergySlotUI>(Enums.PoolType.SynergyEffects,
            synergySlot, 1, synergyEffectScroll);
        synergySlotUIList = new List<SynergySlotUI>();
    }

    // 시너지 효과 UI 풀에서 가져오기 및 패널 활성화
    public void ShowSynergyEffect()
    {
        // 만약 활성화 되기 전에 미리 시너지 슬롯을 만들어놨다면 다시 Pool로 되돌림
        if (synergySlotUIList.Count > 0)
        {
            ReturnSynergySlot();
        }

        int i = 0;

        foreach (var keys in inventoryManager.synergyCount.Keys)
        {
            SynergySlotUI slot = poolManager.Get<SynergySlotUI>(Enums.PoolType.SynergyEffects);

            // 오브젝트의 자식 인덱스를 정해줌 (위부터 차례대로 나오게 하기 위함)
            slot.transform.SetSiblingIndex(i);
            i++;

            slot.SetUp(synergyManager.GetSynergyData(keys), inventoryManager.synergyActiveCount[keys],
                synergyManager.GetSynergyData(keys).maxLevel);

            synergySlotUIList.Add(slot);
        }
    }

    // 시너지 효과 Slot을 다시 오브젝트 풀에 되돌림
    public void ReturnSynergySlot()
    {
        if (synergySlotUIList == null || synergySlotUIList.Count == 0) return;

        for (int i = 0; i < synergySlotUIList.Count; i++)
        {
            poolManager.Return(Enums.PoolType.SynergyEffects, synergySlotUIList[i]);
        }

        synergySlotUIList.Clear();
    }
}
