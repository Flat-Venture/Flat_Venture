using UnityEngine;
using TMPro;
using System.Collections.Generic; // Text Mesh Pro 사용을 위해 추가

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance;

    [Header("UI References")]
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private TextMeshProUGUI goldText; // Gold : [골드값] 표시용 텍스트

    public Transform itemListParent;                    // 아이템 리스트 부모 오브젝트
    public GameObject sellItemInfoPrefab;               // 팔 아이템 정보 프리팹                  

    Dictionary<int, bool> shopItemId;                   // 중복 아이템 체크용 딕셔너리

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // 최초 로딩 시 상점은 무조건 비활성화
        if (shopPanel != null)
            shopPanel.SetActive(false);

        // PlayerGoldSystem의 이벤트에 UI 갱신 함수 연결
        if (Player.Instance != null && Player.Instance.goldSystem != null)
        {
            Player.Instance.goldSystem.OnGoldChanged += UpdateGoldUI;
        }

        shopItemId = new Dictionary<int, bool>();

        while (shopItemId.Count < 3)
        {
            InstantiateShopItem();
        }
        
    }

    public void InstantiateShopItem()
    {
        ItemData shopItem = ItemManager.Instance.GetRandomItemDataByRank(
            ItemManager.Instance.GetRandomItemRank());

        if (shopItemId.ContainsKey(shopItem.iId))
        {
            return;
        }

        GameObject go = Instantiate(sellItemInfoPrefab, itemListParent);
        ShopItemUIController itemInfo = go.GetComponent<ShopItemUIController>();
        itemInfo.InitializeShopItem(shopItem);
        shopItemId.Add(shopItem.iId, true);
    }

    // 상점 열기
    public void OpenShop()
    {
        if (!shopPanel.activeSelf)
        {
            SoundManager.Instance.PlaySFX("shopEnter");
        }

        shopPanel.SetActive(true);

        // 열릴 때 현재 골드값 즉시 반영
        if (Player.Instance != null && Player.Instance.goldSystem != null)
        {
            UpdateGoldUI(Player.Instance.goldSystem.currentGold);
        }

        Debug.Log("상점을 열었습니다.");
    }

    // 상점 닫기 (Exit 버튼에 연결)
    public void CloseShop()
    {
        shopPanel.SetActive(false);
        Debug.Log("상점을 닫았습니다.");
    }

    // 골드 UI 텍스트 업데이트 함수
    private void UpdateGoldUI(float currentGold)
    {
        if (goldText != null)
        {
            // 소수점 없이 정수로 표현하려면 (int) 캐스팅이나 "F0" 포맷 사용
            goldText.text = $"Gold : {Mathf.FloorToInt(currentGold)}";
        }
    }

    private void OnDestroy()
    {
        // 메모리 누수 방지를 위한 이벤트 구독 해제
        if (Player.Instance != null && Player.Instance.goldSystem != null)
        {
            Player.Instance.goldSystem.OnGoldChanged -= UpdateGoldUI;
        }
    }
}