using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 1. UI 관리를 담당하는 매니저
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    private RectTransform canvas;                                   // 캔버스 참조

    [SerializeField]
    [Header("인벤토리 UI")]
    private GameObject inventoryPanelPrefab;                        // 인벤토리 패널 프리팹
    public InventoryUIController inventoryUIController;             // 인벤토리 UI 컨트롤러

    [SerializeField]
    [Header("아이템 설명 UI")]
    private GameObject itemDescriptionPanelPrefab;                  // 아이템 설명 패널 프리팹
    public ItemDescriptionUIController itemDescriptionUIController; // 아이템 설명 UI 컨트롤러

    [SerializeField]
    [Header("플레이어 스탯 패널 UI")]
    private GameObject playerStatPanelPrefab;                       // 플레이어 스탯창 프리팹
    public PlayerStatUIController playerStatUIController;           // 플레이어 스탯창 UI 컨트롤러

    [SerializeField]
    [Header("시너지 설명 UI")]
    private GameObject synergyDescriptionPanelPrefab;               // 시너지 설명 패널 프리팹
    public SynergyDescriptionUIController synergyDescriptionUIController;   // 시너지 설명 패널 UI 컨트롤러

    [SerializeField]
    [Header("스왑, 강화 모드 UI")]
    private GameObject modeUIPrefab;                                // 강화, 스왑 모드 UI 프리팹
    public ModeUIController modeUIController;                       // 강화, 스왑 모드 UI 컨트롤러

    [SerializeField]
    [Header("특성 패널 UI")]
    private GameObject traitPanelPrefab;                            // 특성 패널 프리팹
    public TraitUIController traitUIController;                     // 특성 UI 컨트롤러

    [SerializeField]
    [Header("세팅 UI")]
    private GameObject settingUIPrefab;                             // 세팅 UI 프리팹
    public SettingUIController settingUIController;                 // 세팅 UI 컨트롤러

    [SerializeField]
    [Header("플레이어 상태UI")]
    private GameObject playerStatePanelPrefab;                      // 플레이어 상태 UI 프리팹
    private GameObject playerStateUI;

    [SerializeField]
    [Header("스킬 슬롯UI")]
    private GameObject skillSlotPanelPrefab;                        //스킬 슬롯 UI 프리팹
    private GameObject skillSlotUI;

    [SerializeField]
    [Header("스킬 선택 UI")]
    private GameObject skillSelectPrefab;                           
    public SkillSelectionUI skillSelectionUI;

    SceneLoaderManager sceneLoaderManager;

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
    private void Start()
    {
        sceneLoaderManager = SceneLoaderManager.Instance;
    }

    private void Update()
    {
        if(sceneLoaderManager.GetCurrentSceneName() == SceneNames.Title)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.I))
        {
            ToggleInventory();
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            if (sceneLoaderManager.GetCurrentSceneName() == SceneNames.Town)
            {
                ToggleTrait();
            }
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            InventoryManager.Instance.AddItemToInventory(
                ItemManager.Instance.GetRandomItemDataByRank(ItemManager.Instance.GetRandomItemRank()));
        }
    }

    // Canvas 등록
    public void RegisterCanvas(RectTransform canvasTransform)
    {
        // 이미 캔버스가 등록되어 있는 경우 경고 메시지 출력 후 종료
        if (canvas != null && canvas != canvasTransform)
        {
            Debug.LogWarning("UIManager: 이미 캔버스가 등록되어 있습니다.");
            return;
        }
        
        canvas = canvasTransform;

        InstantiatePlayerStateUI(canvas);
        InstantiateSkillSlotUI(canvas);
        InstantiateInventoryPanel(canvas);
        InstantiateItemDescriptionPanel(canvas);
        InstantiatePlayerStatPanel(canvas);
        InstantiateSynergyDescriptionPanel(canvas);
        InstantiateModeUI(canvas);
        InstantiateSettingUI(canvas);
        InstantiateSkillSelectPanel(canvas);
        InstantiateTraitPanel(canvas);

    }

    // Canvas 등록 해제
    public void UnRegisterCanvas(RectTransform canvasTransform)
    {
        if (canvas == canvasTransform)
        {
            canvas = null;
        }
    }

    // 인벤토리 UI 토글
    public void ToggleInventory()
    {
        // 인벤토리 UI 토글
        inventoryUIController.gameObject.SetActive(!inventoryUIController.gameObject.activeSelf);
        playerStatUIController.gameObject.SetActive(!playerStatUIController.gameObject.activeSelf);

        // 인벤토리가 비활성화 되어있는데 아이템 설명창이 남아있는 경우 아이템 설명창도 hide
        if (!inventoryUIController.gameObject.activeSelf && itemDescriptionUIController.gameObject.activeSelf)
        {
            itemDescriptionUIController.HideItemDescription();
        }

        if (!inventoryUIController.gameObject.activeSelf && synergyDescriptionUIController.gameObject.activeSelf)
        {
            synergyDescriptionUIController.HideSynergyDescription();
        }

        // ModeUI도 이 때 같이 켜져야 함
        modeUIController.UpdateModeUI();
        if (!inventoryUIController.gameObject.activeSelf)
        {
            modeUIController.HideModeUI();
        }
        else
        {
            modeUIController.ShowModeUI();
        }

        playerStatUIController.UpdatePlayerStatUI();
    }

    // 특성 UI 토글
    public void ToggleTrait()
    {
        traitUIController.gameObject.SetActive(!traitUIController.gameObject.activeSelf);
        if (traitUIController.gameObject.activeSelf)
        {
            traitUIController.RefreshTraitUI();
        }
    }

    // InventoryPanel 생성
    void InstantiateInventoryPanel(RectTransform canvas)
    {
        // 인벤토리 패널 생성 및 초기화
        GameObject panel = Instantiate(inventoryPanelPrefab, canvas);
        inventoryUIController = panel.GetComponent<InventoryUIController>();

        inventoryUIController.InitInventoryUIController();

        panel.SetActive(false);
    }

    // ItemDescriptionPanel 생성
    void InstantiateItemDescriptionPanel(RectTransform canvas)
    {
        // 아이템 설명 패널 생성 및 초기화
        GameObject panel = Instantiate(itemDescriptionPanelPrefab, canvas);
        itemDescriptionUIController = panel.GetComponent<ItemDescriptionUIController>();
        panel.SetActive(false);
    }

    // playerStatPanel 생성
    void InstantiatePlayerStatPanel(RectTransform canvas)
    {
        // 플레이어 스탯 패널 생성 및 초기화
        GameObject panel = Instantiate(playerStatPanelPrefab, canvas);
        playerStatUIController = panel.GetComponent<PlayerStatUIController>();

        playerStatUIController.InitPlayerStatUIController();

        panel.SetActive(false);
    }

    // SynergyDescriptionPanel 생성
    void InstantiateSynergyDescriptionPanel(RectTransform canvas)
    {
        // 시너지 설명 패널 생성 및 초기화
        GameObject panel = Instantiate(synergyDescriptionPanelPrefab, canvas);
        synergyDescriptionUIController = panel.GetComponent<SynergyDescriptionUIController>();

        panel.SetActive(false);
    }

    // traitPanel 생성
    void InstantiateTraitPanel(RectTransform canvas)
    {
        // 특성 패널 생성 및 초기화
        GameObject panel = Instantiate(traitPanelPrefab, canvas);
        traitUIController = panel.GetComponent<TraitUIController>();

        panel.SetActive(false);
    }

    // ModeUI 생성
    void InstantiateModeUI(RectTransform canvas)
    {
        GameObject ui = Instantiate(modeUIPrefab, canvas);
        modeUIController = ui.GetComponent<ModeUIController>();

        ui.SetActive(false);
    }

    // SettingUI 생성
    void InstantiateSettingUI(RectTransform canvas)
    {
        GameObject ui = Instantiate(settingUIPrefab, canvas);
        settingUIController = ui.GetComponent<SettingUIController>();

        ui.SetActive(true);
    }

    // Player State(Hp, Mp, Gold) UI 생성
    void InstantiatePlayerStateUI(RectTransform canvas)
    {
        playerStateUI = Instantiate(playerStatePanelPrefab, canvas);
        playerStateUI.transform.SetSiblingIndex(0);

        playerStateUI.SetActive(true);
    }

    // Skill Slot UI 생성
    void InstantiateSkillSlotUI(RectTransform canvas)
    {
        skillSlotUI = Instantiate(skillSlotPanelPrefab, canvas);
        skillSlotUI.transform.SetSiblingIndex(0);

        playerStateUI.SetActive(true);
    }

    // Skill Select UI 생성
    void InstantiateSkillSelectPanel(RectTransform canvas)
    {
        GameObject ui = Instantiate(skillSelectPrefab, canvas);
        skillSelectionUI = ui.GetComponent<SkillSelectionUI>();

        ui.SetActive(false);
    }

    /// <summary>
    /// 플레이어HUD 업데이트해주기 (나올때만 나오기)
    /// </summary>
    /// <param name="sceneName"></param>
    public void UpdateHUD(string sceneName)
    {
        bool showHUD =
            sceneName == SceneNames.Town ||
            sceneName == SceneNames.Dungeon||
            sceneName == SceneNames.Restroom;

        if (playerStateUI != null)
        {
            playerStateUI.SetActive(showHUD);
        }

        if(skillSelectionUI != null)
        {
            skillSlotUI.SetActive(showHUD);
        }
    }

    public Transform GetCanvas()
    {
        return canvas;
    }
}
