using UnityEngine;

/// <summary>
/// 몬스터가 등장하지 않고 모닥불 상호작용과 포탈 생성을 관리하는 휴식 방 전용 컨트롤러
/// </summary>
public class RestRoomController : MonoBehaviour
{
    [Header("Room Settings")]
    [Tooltip("플레이어가 스폰될 위치")]
    public Transform playerSpawnPoint;
    
    [Header("Portal Settings")]
    [Tooltip("다음 방으로 넘어갈 포탈 프리팹")]
    public GameObject portalPrefab;
    [Tooltip("선택을 마치면 포탈이 생성될 위치")]
    public Transform portalSpawnPoint;

    [Header("UI Settings")]
    [Tooltip("화면에 띄울 휴식 방 전용 UI 패널")]
    public GameObject restUIPanel;

    private void Start()
    {
        //방이 시작될 때 UI는 무조건 꺼둠
        if (restUIPanel != null)
        {
            restUIPanel.SetActive(false);
        }
    }

    /// <summary>
    /// StageManager가 방을 세팅할 때 호출하는 시작 함수
    /// </summary>
    public void StartRoomEvent()
    {
        Debug.Log("<color=green>[Rest Room]</color> 휴식 방에 입장했습니다. 평화롭습니다.");
        // 휴식방은 몬스터를 스폰할 필요가 없으므로 바로 대기 상태로 들어감
    }

    /// <summary>
    /// 모닥불 스크립트(CampfireInteract)의 UnityEvent에서 호출할 함수
    /// </summary>
    public void OpenRestUI()
    {
        Debug.Log("<color=orange>[Rest Room]</color> 휴식 방 UI를 엽니다.");
        if (restUIPanel != null)
        {
            restUIPanel.SetActive(true);
            
            //TODO: 필요하다면 UI가 켜졌을 때 플레이어의 이동을 막거나 Time.timeScale = 0f 로 정지
        }
    }

    /// <summary>
    /// UI에서 [휴식] 또는 [아이템 교환] 버튼을 눌러 선택을 완전히 마쳤을 때 호출할 함수
    /// </summary>
    public void OnChoiceCompleted()
    {
        //UI 닫기
        if (restUIPanel != null)
        {
            restUIPanel.SetActive(false);
        }

        //밖으로 나갈 포탈 열어주기
        SpawnPortal();
    }

    private void SpawnPortal()
    {
        if (portalPrefab != null && portalSpawnPoint != null)
        {
            Instantiate(portalPrefab, portalSpawnPoint.position, Quaternion.identity);
            Debug.Log("<color=cyan>[Rest Room]</color> 포탈이 열렸습니다");
        }
        else
        {
            Debug.LogError("RestRoomController에 포탈 프리팹이나 스폰 위치가 할당되지 않았습니다!");
        }
    }
}