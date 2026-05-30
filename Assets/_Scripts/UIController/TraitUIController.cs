using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class TraitUIController : MonoBehaviour
{
    public AllTraitsData allTraits;             // 모든 특성 SO 리스트
    public Transform slotParent;                // 특성 슬롯의 부모
    public TextMeshProUGUI remainPoint;         // 남은 포인트 텍스트
    public GameObject traitSlotPrefab;          // 특성 슬롯 프리팹

    List<TraitSlotUI> traitsSlots = new List<TraitSlotUI>();

    TraitManager traitManager;

    private void Awake()
    {
        traitManager = TraitManager.Instance;
    }

    private void Start()
    {
        
        SortAllTraitsByTraitType();

        foreach (TraitData data in allTraits.allTraits)
        {
            GameObject slot = Instantiate(traitSlotPrefab, slotParent);
            TraitSlotUI slotUI = slot.GetComponent<TraitSlotUI>();
            slotUI.InitializeTraitSlotUI(data);
            traitsSlots.Add(slotUI);
        }
        RefreshTraitUI();
    }

    public void RefreshTraitUI()
    {
        foreach(TraitSlotUI slot in traitsSlots)
        {
            slot.RefreshTraitSlot();
        }
        remainPoint.text = traitManager.GetAvailablePoints().ToString();
    }

    public void SortAllTraitsByTraitType()
    {
        allTraits.allTraits.Sort((a, b) => a.traitType.CompareTo(b.traitType));
    }
}
