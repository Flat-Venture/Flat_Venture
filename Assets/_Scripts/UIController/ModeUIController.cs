using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ModeUIController : MonoBehaviour
{
    [Header("Mode 관련 UI 정보")]
    public TextMeshProUGUI modeName;                    // 아이템 승급, 스왑 어떤 모드인지 출력
    public Button okButton;                             // 아이템 승급 또는 스왑 버튼
    public TextMeshProUGUI okButtonText;                // 아이템 승급 또는 스왑 텍스트
    public Button cancelButton;                         // 취소 버튼

    [Header("Mode 함수 적용을 위한 정보")]
    public int swapModeClickCount = 0;                  // 스왑 모드 클릭 횟수 (최대 2개)
    public int rankUpModeClickCount = 0;                // 아이템 승급 모드 클릭 횟수 (최대 1개)

    public List<int> swapItemIndex = new List<int>();   // 스왑 모드 클릭한 인벤토리 인덱스
    public int rankUpItemIndex = -1;                    // 아이템 승급 모드 클릭한 인벤토리 인덱스

    ModeManager modeManager;
    InventoryManager inventoryManager;
    UIManager uiManager;
    SoundManager soundManager;

    void Awake()
    {
        modeManager = ModeManager.Instance;
        inventoryManager = InventoryManager.Instance;
        uiManager = UIManager.Instance;
        soundManager = SoundManager.Instance;

        okButton.onClick.AddListener(OkButtonClick);
        cancelButton.onClick.AddListener(CancelButtonClick);
    }

    private void Update()
    {
        // 조건이 충족되지 않으면 스왑 / 강화 버튼 비활성화
        switch (modeManager.GetCurrentMode())
        {
            // 일반 모드
            case Enums.InventoryMode.None:
                return;
            // 스왑 모드
            case Enums.InventoryMode.Swap:
                if(swapModeClickCount < 2)
                {
                    okButton.interactable = false;
                }
                else
                {
                    okButton.interactable = true;
                }
                break;

            // 아이템 등급 강화 모드
            case Enums.InventoryMode.RankUp:
            case Enums.InventoryMode.RanKUpWithSynergy:
                if (rankUpModeClickCount < 1)
                {
                    okButton.interactable = false;
                }
                else
                {
                    okButton.interactable = true;
                }
                break;
        }
    }

    public void UpdateModeUI()
    {
        switch (modeManager.GetCurrentMode())
        {
            // 일반 모드
            case Enums.InventoryMode.None:
                return;
            // 스왑 모드
            case Enums.InventoryMode.Swap:
                modeName.text = "아이템 위치 변경";
                okButtonText.text = "변경";
                break;
            // 아이템 등급 강화 모드
            case Enums.InventoryMode.RankUp:
                modeName.text = "아이템 등급 강화";
                okButtonText.text = "등급 강화";
                break;
            // 시너지 유지 아이템 등급 강화 모드
            case Enums.InventoryMode.RanKUpWithSynergy:
                modeName.text = "시너지 유지 아이템 등급 강화";
                okButtonText.text = "등급 강화";
                break;
        }
    }

    void OkButtonClick()
    {
        switch (modeManager.GetCurrentMode())
        {
            // 일반 모드
            case Enums.InventoryMode.None:
                return;

            // 스왑 모드
            case Enums.InventoryMode.Swap:
                if(swapModeClickCount == 2 && swapItemIndex != null && swapItemIndex.Count == 2)
                {
                    inventoryManager.SwapItem(swapItemIndex[0], swapItemIndex[1]);
                }
                // 만약 모두 선택하지 않았는데 스왑 버튼을 눌렀을 경우 그냥 리턴 (예외처리 추가 작업)
                else
                {
                    return;
                }

                soundManager.PlaySFX("swap");
                ClearSwapMode();
                uiManager.inventoryUIController.UnCheckAllSlot();
                modeManager.SetMode(Enums.InventoryMode.None);
                HideModeUI();
                break;

            // 아이템 등급 강화 모드
            case Enums.InventoryMode.RankUp:
                if(rankUpModeClickCount == 1 && rankUpItemIndex <= 8 && rankUpItemIndex >= 0)
                {
                    inventoryManager.RankUpItem(rankUpItemIndex);
                }
                // 만약 모두 선택하지 않았는데 강화 버튼을 눌렀을 경우 그냥 리턴 (예외처리 추가 작업)
                else
                {
                    return;
                }

                soundManager.PlaySFX("reinforce");
                ClearRankUpMode();
                uiManager.inventoryUIController.UnCheckAllSlot();
                modeManager.SetMode(Enums.InventoryMode.None);
                HideModeUI();
                break;

            // 시너지 유지 아이템 등급 강화 모드
            case Enums.InventoryMode.RanKUpWithSynergy:
                if (rankUpModeClickCount == 1 && rankUpItemIndex <= 8 && rankUpItemIndex >= 0)
                {
                    inventoryManager.RankUpItemWithSynergy(rankUpItemIndex);
                }
                else
                {
                    return;
                }

                soundManager.PlaySFX("reinforce");
                ClearRankUpMode();
                uiManager.inventoryUIController.UnCheckAllSlot();
                modeManager.SetMode(Enums.InventoryMode.None);
                HideModeUI();
                break;
        }
    }
    

    void CancelButtonClick()
    {
        switch (modeManager.GetCurrentMode())
        {
            // 일반 모드
            case Enums.InventoryMode.None:
                return;

            // 스왑 모드
            case Enums.InventoryMode.Swap:
            case Enums.InventoryMode.RankUp:
            case Enums.InventoryMode.RanKUpWithSynergy:
                ClearSwapMode();
                uiManager.inventoryUIController.UnCheckAllSlot();
                break;
        }
    }

    // 스왑 아이템이 2개만 찍히도록
    public bool CheckSwapModeCanClick()
    {
        if(swapModeClickCount >= 2)
        {
            return false;
        }
        return true;
    }

    // 아이템 등급 강화 아이템이 1개만 찍히도록
    public bool CheckRankUpModeCanClick()
    {
        if (rankUpModeClickCount >= 1)
        {
            return false;
        }
        return true;
    }

    // 아이템 개수 업데이트
    public void UpdateSwapModeClickCount(int count)
    {
        swapModeClickCount += count;
    }
    public void UpdateRankUpModeClickCount(int count)
    {
        rankUpModeClickCount += count;
    }

    // 스왑 인덱스 추가 및 제거
    public void AddSwapItemIndex(int index)
    {
        swapItemIndex.Add(index);
    }
    public void RemoveSwapItemIndex(int index)
    {
        if (swapItemIndex != null && swapItemIndex.Count > 0)
        {
            swapItemIndex.Remove(index);
        }
    }

    // 강화 인덱스 추가 및 제거
    public void AddRankUpItemIndex(int index)
    {
        rankUpItemIndex = index;
    }
    public void RemoveRankUpItemIndex(int index)
    {
        rankUpItemIndex = -1;
    }

    // 스왑 모드 변수 초기화
    public void ClearSwapMode()
    {
        swapModeClickCount = 0;
        swapItemIndex.Clear();
    }

    // 아이템 승급 모드 변수 초기화
    public void ClearRankUpMode()
    {
        rankUpModeClickCount = 0;
        rankUpItemIndex = -1;
    }

    public void ShowModeUI()
    {
        if (modeManager.GetCurrentMode() != Enums.InventoryMode.None)
        {
            gameObject.SetActive(true);
        }
    }

    public void HideModeUI()
    {
        gameObject.SetActive(false);
    }
}
