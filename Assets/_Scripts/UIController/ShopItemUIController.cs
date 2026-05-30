using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class ShopItemUIController : MonoBehaviour
{
    ItemData shopItem;

    public TextMeshProUGUI itemName;                    // 아이템 이름
    public Image itemIcon;                              // 아이템 아이콘
    public TextMeshProUGUI itemStat;                    // 아이템 스탯
    public TextMeshProUGUI itemDescription;             // 아이템 설명
    public TextMeshProUGUI itemPrice;                   // 아이템 가격
    public Button buyButton;                            // 구매 버튼 

    public Transform synergyPanel;                      // 시너지 아이콘 부모 패널
    public DescriptionSynergyUI synergyUI;              // 시너지 아이콘 + 텍스트 프리팹

    public GameObject soldoutPanel;                     // 판매 이미지

    void Start()
    {
        soldoutPanel.SetActive(false);

        buyButton.onClick.AddListener(BuyItem);
        if(Player.Instance.goldSystem.currentGold < shopItem.iPrice)
        {
            buyButton.interactable = false;
        }
    }

    // 아이템 설명창 생성
    public void InitializeShopItem(ItemData itemData)
    {
        if (itemData == null)
        {
            return;
        }

        shopItem = itemData;

        // 아이템 이름 텍스트
        itemName.text = itemData.iName;

        // 아이템 등급에 따라 다른 색
        switch (itemData.iRank)
        {
            case Enums.ItemRank.Common:
                itemName.color = Color.white;
                break;
            case Enums.ItemRank.Rare:
                itemName.color = Color.blue;
                break;
            case Enums.ItemRank.Unique:
                itemName.color = new Color(128f / 255f, 0f, 128f / 255f, 1f);
                break;
            case Enums.ItemRank.Legendary:
                itemName.color = new Color(1f, 165f / 255f, 0f, 1f);
                break;
        }

        // 아이템 시너지 중복 제거
        itemData.SanitizeData();

        // 시너지 UI 생성
        for (int i = 0; i < itemData.iSynergy.Count; i++)
        {
            DescriptionSynergyUI icon = Instantiate(synergyUI, synergyPanel);

            SynergyData sd = SynergyManager.Instance.GetSynergyData(itemData.iSynergy[i]);
            icon.SetUp(sd.synergyIcon, sd.synergyName);
        }

        itemStat.text = ItemDataHelper.GetItemStatsDescription(itemData, 1);

        itemDescription.text = itemData.iDescription;
        itemIcon.sprite = itemData.iIcon;
        itemPrice.text = itemData.iPrice.ToString();
    }

    void BuyItem()
    {
        if(Player.Instance.goldSystem.currentGold >= shopItem.iPrice)
        {
            if (!InventoryManager.Instance.CheckInventoryIsFull() &&
                !InventoryManager.Instance.CheckDuplicateItems(shopItem.iId))
            {
                InventoryManager.Instance.AddItemToInventory(shopItem);
                Player.Instance.goldSystem.AddGold(-shopItem.iPrice);

                buyButton.interactable = false;
                soldoutPanel.SetActive(true);
            }
        }
    }
}
