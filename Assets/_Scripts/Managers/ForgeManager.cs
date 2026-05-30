using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ForgeManager : MonoBehaviour
{
    public static ForgeManager Instance;

    [Header("UI References")]
    [SerializeField] private GameObject forgePanel;
    [SerializeField] private TextMeshPro tipText;

    [Header("제련소 버튼")]
    public Button reinforceSlots;                           // 랜덤 슬롯 강화
    public Button rankUp;                                   // 아이템 등급 업
    public Button rankUpWithSynergy;                        // 아이템 시너지 유지 등급 업

    [Header("금액 텍스트")]
    public TextMeshProUGUI reinforcePriceText;              // 랜덤 슬롯 강화 가격
    public TextMeshProUGUI rankUpPriceText;                 // 아이템 등급 업 가격
    public TextMeshProUGUI rankUpWithSynergyPriceText;      // 아이템 시너지 유지 등급 업 가격

    float reinforcePrice;
    float rankUpPrice;
    float rankUpWithSynergyPrice;

    // 선택 여부
    public bool isSelected = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // 최초 로드 시 이전 forge 의 선택 여부 초기화
        isSelected = false;

        if (forgePanel != null)
            forgePanel.SetActive(false);
        
        // 골드 값 층수에 따라 비싸지게
        InitializePrice(DungeonManager.Instance.userEXPClearedRoomCount);
        
        // 플레이어 골드
        float playerGold = Player.Instance.goldSystem.currentGold;

        // 플레이어 골드가 적으면 버튼 비활성화
        if (playerGold < reinforcePrice)
        {
            reinforceSlots.interactable = false;
        }
        if (playerGold < rankUpPrice)
        {
            rankUp.interactable = false;
        }
        if (playerGold < rankUpWithSynergyPrice)
        {
            rankUpWithSynergy.interactable = false;
        }

        // 버튼 이벤트 연결
        reinforceSlots.onClick.AddListener(Select1);
        rankUp.onClick.AddListener(Select2);
        rankUpWithSynergy.onClick.AddListener(Select3);
    }

    // 나중에 count int로 바꿔야함
    void InitializePrice(float count = 0)
    {
        reinforcePrice = 100 + count * 10;
        rankUpPrice = 150 + count * 20;
        rankUpWithSynergyPrice = 200 + count * 30;

        reinforcePriceText.text = reinforcePrice.ToString() + " G";
        rankUpPriceText.text = rankUpPrice.ToString() + " G";
        rankUpWithSynergyPriceText.text = rankUpWithSynergyPrice.ToString() + " G";
    }

    public void OpenForge()
    {
        // 이미 선택을 끝냈다면 다시 창을 열 수 없음
        if (isSelected)
        {
            Debug.Log("이미 선택 완료됨. Forge UI 열리지 않음.");
            return;
        }

        if (forgePanel != null)
        {
            forgePanel.SetActive(true);
            Debug.Log("대장간 UI 활성화");
        }
    }

    public void CloseForge()
    {
        if (forgePanel != null)
        {
            forgePanel.SetActive(false);
            Debug.Log("대장간 UI 비활성화");
        }
    }

    // ---------------------
    // 선택 버튼들 (추후 구현 예정)
    // ---------------------

    // 슬롯 강화
    public void Select1()
    {
        CompleteSelection();

        if (Player.Instance.goldSystem.currentGold >= reinforcePrice)
        {
            Debug.LogWarning(reinforcePrice);
            Player.Instance.goldSystem.AddGold(-reinforcePrice);
        }

        int randomIndex = Random.Range(0, InventoryManager.Instance.inventory.Length);
        InventoryManager.Instance.ReinforceInventorySlot(randomIndex);

        CloseForge();
    }

    // 아이템 등급 업
    public void Select2()
    {
        CompleteSelection();

        if (Player.Instance.goldSystem.currentGold >= rankUpPrice)
        {
            Player.Instance.goldSystem.AddGold(-rankUpPrice);
        }

        ModeManager.Instance.SetMode(Enums.InventoryMode.RankUp);
        if (!UIManager.Instance.inventoryUIController.gameObject.activeSelf)
        {
            UIManager.Instance.ToggleInventory();
        }
        else
        {
            UIManager.Instance.ToggleInventory();
            UIManager.Instance.ToggleInventory();
        }

        CloseForge();
    }

    // 아이템 시너지 유지 등급 업
    public void Select3()
    {
        CompleteSelection();

        if (Player.Instance.goldSystem.currentGold >= rankUpWithSynergyPrice)
        {
            Player.Instance.goldSystem.AddGold(-rankUpWithSynergyPrice);
        }

        ModeManager.Instance.SetMode(Enums.InventoryMode.RanKUpWithSynergy);
        if (!UIManager.Instance.inventoryUIController.gameObject.activeSelf)
        {
            UIManager.Instance.ToggleInventory();
        }
        else
        {
            UIManager.Instance.ToggleInventory();
            UIManager.Instance.ToggleInventory();
        }

        CloseForge();
    }

    private void CompleteSelection()
    {
        isSelected = true; //선택 완료

        if (tipText != null) //월드 팁 텍스트 변경
            tipText.text = "선택 완료됨";

        CloseForge();
    }

}
