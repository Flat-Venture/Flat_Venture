using UnityEngine;

public class SpawnPointManager : MonoBehaviour
{
    private void Start()
    {
        MovePlayerToSpawn();
    }

    private void MovePlayerToSpawn()
    {
        // 플레이어 인스턴스 확인
        if (Player.Instance == null)
        {
            Debug.LogWarning("SpawnPointManager: Player.Instance가 존재하지 않습니다.");
            return;
        }

        // 스폰 포인트 찾기
        GameObject spawn = GameObject.Find("SpawnPoint");

        if (spawn == null)
        {
            Debug.LogError("SpawnPointManager: SpawnPoint를 찾을 수 없습니다.");
            return;
        }

        // --- 위치 이동 로직 시작 ---

        // CharacterController 처리 (가장 중요한 부분)
        // CharacterController가 켜져 있으면 transform.position 변경이 무시될 수 있습니다.
        CharacterController controller = Player.Instance.GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;

        // 실제 위치 및 회전값 복사
        Player.Instance.transform.position = spawn.transform.position;
        Player.Instance.transform.rotation = spawn.transform.rotation;

        // 컴포넌트 재활성화
        if (controller != null) controller.enabled = true;

        Debug.Log("SpawnPointManager: 플레이어를 스폰 포인트로 이동시켰습니다.");
    }
}