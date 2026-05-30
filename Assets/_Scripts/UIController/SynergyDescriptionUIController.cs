using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SynergyDescriptionUIController : MonoBehaviour
{
    [Header("시너지 설명 패널 UI 정보")]
    public RectTransform synergyDescriptionPanel;       // 시너지 설명 패널 오브젝트
    public TextMeshProUGUI synergyNameText;             // 시너지 이름 텍스트
    public TextMeshProUGUI synergyDescriptionText;      // 시너지 설명 텍스트
    public Image synergyIcon;                           // 시너지 아이콘 이미지

    [Header("시너지 스탯 설명을 위한 UI 정보")]
    public SynergyData selectedSynergy;                 // 현재 선택된 시너지 데이터

    public Transform synergyStatTextPanel;              // 시너지 스탯 설명 텍스트 프리팹의 부모 패널
    public SynergyStatText synergyStatText;             // 시너지 스탯 설명 텍스트 프리팹
    public List<SynergyStatText> synergyStatTextList;   // 시너지 스탯 설명 텍스트 프리팹 리스트 

    Vector2 firstSynergyDescriptionSize;                // 시너지 설명 패널 처음 크기

    float textHeight;

    PoolManager poolManager;
    InventoryManager inventoryManager;

    private void Awake()
    {
        poolManager = PoolManager.Instance;
        inventoryManager = InventoryManager.Instance;

        poolManager.CreatePool<SynergyStatText>(Enums.PoolType.SynergyStatText,
            synergyStatText, 4, synergyStatTextPanel);

       firstSynergyDescriptionSize = synergyDescriptionPanel.sizeDelta;
    }

    public void IncreaseSynergyDescriptionSize(int count)
    {
        textHeight = synergyStatText.statText.rectTransform.sizeDelta.y * count;

        synergyDescriptionPanel.sizeDelta = new Vector2(
            synergyDescriptionPanel.sizeDelta.x, synergyDescriptionPanel.sizeDelta.y + textHeight);
    }

    public void InitializeSynergyDescriptionSize()
    {
        synergyDescriptionPanel.sizeDelta = firstSynergyDescriptionSize;
    }

    public void ShowSynergyDescription(SynergyData synergyData)
    {
        if(synergyData == null)
        {
            HideSynergyDescription();
            return;
        }
        synergyDescriptionPanel.gameObject.SetActive(true);

        synergyNameText.text = synergyData.name;
        synergyDescriptionText.text = synergyData.synergyDescription;
        synergyIcon.sprite = synergyData.synergyIcon;

        for (int i = 0; i < synergyData.levels.Count; i++)
        {
            SynergyStatText statText = poolManager.Get<SynergyStatText>(Enums.PoolType.SynergyStatText);

            // 오브젝트의 자식 인덱스를 정해줌 (위부터 차례대로 나오게 하기 위함)
            statText.transform.SetSiblingIndex(i);

            statText.Setup(inventoryManager.synergyActiveCount[synergyData.synergyType],
                synergyData.levels[i].requiredLines, synergyData);
            synergyStatTextList.Add(statText);
        }
        
        IncreaseSynergyDescriptionSize(synergyStatTextList.Count);
    }

    // 아이템 설명 패널 비활성화
    public void HideSynergyDescription()
    {
        InitializeSynergyDescriptionSize();
        ReturnSynergyStatText();
        synergyDescriptionPanel.gameObject.SetActive(false);
    }

    // 시너지 UI 오브젝트 풀로 돌려주는 함수
    public void ReturnSynergyStatText()
    {
        if (synergyStatTextList == null || synergyStatTextList.Count == 0) return;

        for (int i = 0; i < synergyStatTextList.Count; i++)
        {
            poolManager.Return(Enums.PoolType.SynergyStatText, synergyStatTextList[i]);
        }
        synergyStatTextList.Clear();
    }
}
