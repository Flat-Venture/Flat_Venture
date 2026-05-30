using System;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("인벤토리 슬롯 UI 정보")]
    public int slotIndex;                       // 슬롯 인덱스
    public Image itemIcon;                      // 아이템 아이콘 이미지
    public Image rarityFrame;                   // 아이템 희귀도 프레임 이미지 -> 추후 추가
    public GameObject selectedFrame;            // 선택된 슬롯 프레임 이미지
    public TextMeshProUGUI reinforceLevel;      // 슬롯 강화 레벨 텍스트
    public Image clickedCheck;                  // 아이템 클릭 확인용 이미지

    //Action<int> clickCallback;

    InventoryManager inventoryManager;
    UIManager uiManager;
    ModeManager modeManager;
    SoundManager soundManager;

    private void Start()
    {
        inventoryManager = InventoryManager.Instance;
        uiManager = UIManager.Instance;
        modeManager = ModeManager.Instance;
        soundManager = SoundManager.Instance;
    }

    private void OnEnable()
    {
        if(selectedFrame != null)
        {
            selectedFrame.SetActive(false);
        }

        if(clickedCheck != null)
        {
            clickedCheck.enabled = false;
        }
    }

    public void SetUp(int index)
    {
        slotIndex = index;
        //clickCallback = clickAction;
    }

    // 아이템 슬롯에 마우스 커서 들어가면 아이템 설명창 활성화
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (selectedFrame != null)
        {
            selectedFrame.SetActive(true);
            uiManager.itemDescriptionUIController.isMouseOver = true;
            uiManager.itemDescriptionUIController.slotIndex = slotIndex;
            uiManager.itemDescriptionUIController.
                ShowItemDescription(inventoryManager.inventory[slotIndex]);
        }
    }

    // 아이템 슬롯에서 마우스 커서 나가면 아이템 설명창 비활성화
    public void OnPointerExit(PointerEventData eventData)
    {
        if (selectedFrame != null)
        {
            selectedFrame.SetActive(false);
            uiManager.itemDescriptionUIController.isMouseOver = false;
            // 프리팹을 아이템 풀에 다시 돌려주는 작업
            uiManager.itemDescriptionUIController.HideItemDescription();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        soundManager.PlaySFX("slotClick");

        switch (modeManager.GetCurrentMode())
        {
            // 일반 모드
            case Enums.InventoryMode.None:
                return;

            // 스왑 모드
            case Enums.InventoryMode.Swap:
                if (!uiManager.modeUIController.CheckSwapModeCanClick() && clickedCheck.enabled == false)
                {
                    return;
                }

                if (clickedCheck.enabled)
                {
                    uiManager.modeUIController.UpdateSwapModeClickCount(-1);
                    uiManager.modeUIController.RemoveSwapItemIndex(slotIndex);
                }
                else
                {
                    uiManager.modeUIController.UpdateSwapModeClickCount(1);
                    uiManager.modeUIController.AddSwapItemIndex(slotIndex);
                }

                clickedCheck.enabled = !clickedCheck.enabled;

                break;

            // 아이템 등급 강화 모드
            case Enums.InventoryMode.RankUp:
            case Enums.InventoryMode.RanKUpWithSynergy:
                if (!uiManager.modeUIController.CheckRankUpModeCanClick() && clickedCheck.enabled == false)
                {
                    return;
                }

                if (clickedCheck.enabled)
                {
                    uiManager.modeUIController.UpdateRankUpModeClickCount(-1);
                    uiManager.modeUIController.RemoveRankUpItemIndex(slotIndex);
                }
                else
                {
                    uiManager.modeUIController.UpdateRankUpModeClickCount(1);
                    uiManager.modeUIController.AddRankUpItemIndex(slotIndex);
                }

                clickedCheck.enabled = !clickedCheck.enabled;

                break;
        }
    }
}
