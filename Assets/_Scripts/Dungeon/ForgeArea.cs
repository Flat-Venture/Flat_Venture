using UnityEngine;

public class ForgeArea : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private KeyCode interactionKey = KeyCode.F;
    [SerializeField] private LayerMask playerLayer; // 인스펙터에서 'Player' 레이어 선택

    private bool isPlayerInside = false;

    private void Update()
    {
        // 플레이어가 영역 내에 있고 F 키를 누를 때 실행
        if (isPlayerInside && Input.GetKeyDown(interactionKey))
        {
            ForgeManager.Instance.OpenForge();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 레이어 비트 연산을 사용하여 플레이어 레이어인지 확인
        // (1 << other.gameObject.layer)는 해당 오브젝트의 레이어를 비트로 변환합니다.
        if ((playerLayer.value & (1 << other.gameObject.layer)) != 0)
        {
            isPlayerInside = true;
            Debug.Log("대장간 영역 진입 (Layer: Player)");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if ((playerLayer.value & (1 << other.gameObject.layer)) != 0)
        {
            isPlayerInside = false;
            ForgeManager.Instance.CloseForge();
            Debug.Log("대장간 영역 이탈");
        }
    }
}