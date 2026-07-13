using UnityEngine;
using UnityEngine.Events;
using FlatVenture.NUH.Player.State; // 플레이어 상태 접근용
using FlatVenture.NUH.Player;       // PlayerController 접근용

/// <summary>
/// 휴식 방 중앙에 배치되어 플레이어와 상호작용하는 모닥불 오브젝트
/// </summary>
public class CampfireInteract : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("상호작용 가능한 거리")]
    public float interactRadius = 3f;

    [Header("Events")]
    [Tooltip("상호작용(F키) 시 휴식 방 UI를 띄우기 위한 이벤트")]
    public UnityEvent onInteract;

    private bool isPlayerInRange = false;
    
    //한 번만 사용 가능하도록 제한
    private bool isAlreadyUsed = false;
    private PlayerController cachedPlayer;

    private void Update()
    {
        //범위 안에 있고, 아직 사용하지 않았으며, 키보드 'F'를 눌렀을 때 작동
        if (isPlayerInRange && !isAlreadyUsed && Input.GetKeyDown(KeyCode.F))
        {
            Interact();
        }
    }

    private void Interact()
    {
        isAlreadyUsed = true;
        Debug.Log("<color=orange>[Campfire]</color> 모닥불과 상호작용했습니다! 선택지 UI를 호출합니다.");
        
        //TODO: 플레이어 이동 정지 (선택 중 움직임 방지)
        
        //Inspector에 연결된 UI 띄우기 함수 실행
        onInteract?.Invoke();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isAlreadyUsed) return;

        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            cachedPlayer = other.GetComponentInParent<PlayerController>();
            
            //TODO: 플레이어 머리 위에 "F키 눌러서 상호작용" 말풍선 띄우기
            Debug.Log("<color=orange>[Campfire]</color> 모닥불에 다가왔습니다. (F 상호작용 가능)");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            cachedPlayer = null;
            
            //TODO: 말풍선 숨기기
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0.5f, 0, 0.3f);
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}