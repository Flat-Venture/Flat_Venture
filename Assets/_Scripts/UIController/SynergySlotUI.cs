using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SynergySlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("시너지 UI 정보")]
    public SynergyData synergyData;                 // 시너지 데이터
    public Image synergyIcon;                       // 시너지 아이콘
    public TextMeshProUGUI activeSynergyCount;      // 활성화된 시너지 수 / 최대 활성화 수

    UIManager uiManager;

    private void Start()
    {
        uiManager = UIManager.Instance;
    }

    public void SetUp(SynergyData synergyData, int activeCount, int maxCount)
    {
        this.synergyData = synergyData;
        this.synergyIcon.sprite = synergyData.synergyIcon;
        activeSynergyCount.text = activeCount.ToString() + " / " + maxCount.ToString();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        uiManager.synergyDescriptionUIController.ShowSynergyDescription(synergyData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        uiManager.synergyDescriptionUIController.HideSynergyDescription();
    }

}
