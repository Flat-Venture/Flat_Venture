using UnityEngine;

/// <summary>
/// 인게임 화면에서 지도의 가시성과 노드 상호작용 상태 관리
/// </summary>
public class MapUIManager : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("지도를 구성하는 전체 UI의 CanvasGroup")]
    [SerializeField] private CanvasGroup mapCanvasGroup;

    //View의 순수 UI 상태 변수
    public bool isMapOpendByTab = false;

    private void Awake()
    {
        //초기 상태에서 지도 UI 숨김
        HideMap();
    }

    /// <summary>
    /// 최적화: GameObject.SetActive 대신 CanvasGroup을 제어합니다.
    /// </summary>
    /// <param name="isInteractable">노드 클릭 가능 여부 설정</param>
    public void ShowMap(bool isInteractable)
    {
        mapCanvasGroup.alpha = 1.0f;
        mapCanvasGroup.blocksRaycasts = isInteractable;
        mapCanvasGroup.enabled = isInteractable;
    }

    public void HideMap()
    {
        mapCanvasGroup.alpha = 0.0f;
        mapCanvasGroup.blocksRaycasts = false;
        mapCanvasGroup.enabled = false;
    }
}
