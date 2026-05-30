using NUnit.Framework;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemDescriptionUIController : MonoBehaviour
{
    [Header("아이템 설명 패널 UI 정보")]
    public GameObject itemDescriptionPanel;             // 아이템 설명 패널 오브젝트
    public RectTransform itemDescriptionSize;           // 아이템 설명 패널 크기
    public TextMeshProUGUI itemNameText;                // 아이템 이름 텍스트
    public Image itemIcon;                              // 아이템 아이콘 이미지
    public TextMeshProUGUI itemStatDescriptionText;     // 아이템 스탯 설명 텍스트
    public TextMeshProUGUI itemDescriptionText;         // 아이템 설명 텍스트

    [Header("패널의 높이 조절을 위한 텍스트 높이")]
    public float statTextHeight;                        // 스탯 설명 텍스트 높이
    public float descriptionTextHeight;                 // 아이템 설명 텍스트 높이

    [Header("현재 아이템 정보를 가져오기 위한 인덱스")]
    public int slotIndex;                               // 현재 선택된 슬롯 인덱스

    [Header("아이템 시너지 정보를 띄우기 위한 정보")]
    public Transform synergyPanel;                      // 시너지 아이콘 텍스트 프리팹의 부모 패널
    public DescriptionSynergyUI synergyUI;              // 시너지 아이콘과 텍스트 프리팹
    public List<DescriptionSynergyUI> synergyUIList;    // 활성화 되어있는 시너지UI를 저장하는 리스트

    bool isDescripting = false;                         // 아이템 설명창이 켜져있는지 확인
    public bool isMouseOver = false;                    // 아이템 슬롯위에 마우스가 올라가 있는지
    private Vector2 firstItemDescriptionSize;           // 아이템 설명창 처음 크기

    InventoryManager inventoryManager;
    SynergyManager synergyManager;
    PoolManager poolManager;

    private void Awake()
    {
        inventoryManager = InventoryManager.Instance;
        synergyManager = SynergyManager.Instance;
        poolManager = PoolManager.Instance;

        // 설명창의 시너지 설명 오브젝트 풀 생성
        poolManager.CreatePool(Enums.PoolType.DescriptionSynergy, synergyUI, 2, synergyPanel);
        synergyUIList = new List<DescriptionSynergyUI>();

        firstItemDescriptionSize = itemDescriptionSize.sizeDelta;
    }

    private void Update()
    {
        // 아이템 설명창이 활성화 중일 때,
        if (isDescripting && isMouseOver)
        {
            // F키를 누르면
            if (Input.GetKeyDown(KeyCode.F))
            {
                // 아이템 버리기, 아이템 설명창 닫기
                inventoryManager.RemoveItemFromInventory(slotIndex);
                HideItemDescription();
            }
        }
    }

    // 아이템 설명 패널 활성화
    public void ShowItemDescription(ItemData itemData)
    {
        if(itemData == null)
        {
            HideItemDescription();
            return;
        }

        itemDescriptionPanel.SetActive(true);
        isDescripting = true;
        itemNameText.text = itemData.iName;

        // 아이템 등급에 다른 아이템 이름 색 변경
        switch (itemData.iRank)
        {
            case Enums.ItemRank.Common:
                itemNameText.color = Color.white;
                break;
            case Enums.ItemRank.Rare:
                itemNameText.color = Color.blue;
                break;
            case Enums.ItemRank.Unique:
                itemNameText.color = new Color(128f / 255f, 0f, 128f / 255f, 1f);
                break;
            case Enums.ItemRank.Legendary:
                itemNameText.color = new Color(1f, 165f / 255f, 0f, 1f);
                break;
        }

        itemData.SanitizeData();

        // 시너지 UI 오브젝트 풀에서 불러오기
        for (int i = 0; i < itemData.iSynergy.Count; i++)
        {
            DescriptionSynergyUI icon = poolManager.Get<DescriptionSynergyUI>(Enums.PoolType.DescriptionSynergy);

            // 오브젝트 풀에서 꺼내온 오브젝트의 부모를 강제로 설정
            icon.transform.SetParent(synergyPanel, false);

            // 오브젝트의 자식 인덱스를 정해줌 (위부터 차례대로 나오게 하기 위함)
            icon.transform.SetSiblingIndex(i);

            SynergyData sd = synergyManager.GetSynergyData(itemData.iSynergy[i]);
            icon.SetUp(sd.synergyIcon, sd.synergyName);
            synergyUIList.Add(icon);
        }

        itemStatDescriptionText.text = ItemDataHelper.GetItemStatsDescription
            (itemData, inventoryManager.reinforcedSlots[slotIndex]);
        if(gameObject.layer == LayerMask.NameToLayer("RewardUI"))
        {
            itemStatDescriptionText.text = ItemDataHelper.GetItemStatsDescription(itemData, 1);
        }

        itemDescriptionText.text = itemData.iDescription;
        itemIcon.sprite = itemData.iIcon;
        IncreaseItemDescriptionSize();
    }

    // 설명 텍스트 크기에 따른 패널 크기 조절
    public void IncreaseItemDescriptionSize()
    {
        statTextHeight = itemStatDescriptionText.preferredHeight;
        descriptionTextHeight = itemDescriptionText.preferredHeight;
        itemDescriptionSize.sizeDelta = new Vector2(
            itemDescriptionSize.sizeDelta.x, itemDescriptionSize.sizeDelta.y + statTextHeight + descriptionTextHeight);
    }

    public void InitializeItemDescriptionSize()
    {
        itemDescriptionSize.sizeDelta = firstItemDescriptionSize;
    }

    // 아이템 설명 패널 비활성화
    public void HideItemDescription()
    {
        InitializeItemDescriptionSize();
        ReturnSynergyUI();
        itemDescriptionPanel.SetActive(false);
        isDescripting = false;
    }

    // 시너지 UI 오브젝트 풀로 돌려주는 함수
    public void ReturnSynergyUI()
    {
        if (synergyUIList == null || synergyUIList.Count == 0) return;

        for (int i = 0; i < synergyUIList.Count; i++)
        {
            poolManager.Return(Enums.PoolType.DescriptionSynergy, synergyUIList[i]);
        }
        synergyUIList.Clear();
    }
}
