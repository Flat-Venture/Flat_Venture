using UnityEngine;

public class EnemySample : MonoBehaviour
{
    public float moveSpeed = 5f;

    private Transform player;
    private int playerLayer;

    private void Start()
    {
        // Player 레이어 번호 캐싱
        playerLayer = LayerMask.NameToLayer("Player");

        if (playerLayer == -1)
        {
            Debug.LogError("EnemySample: 'Player' 레이어를 찾을 수 없습니다.");
            return;
        }

        // Player 싱글톤이 존재하면 바로 참조
        if (Player.Instance != null)
        {
            player = Player.Instance.transform;
        }
    }

    private void Update()
    {
        // 런타임 중 Player가 생성된 경우 대응
        if (player == null && Player.Instance != null)
        {
            player = Player.Instance.transform;
        }

        // 플레이어를 향해 이동
        if (player != null)
        {
            Vector3 dir = (player.position - transform.position).normalized;
            transform.Translate(dir * moveSpeed * Time.deltaTime, Space.World);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // 충돌체의 루트 게임오브젝트 가져오기
        Transform root = collision.transform.root;

        // null 체크 추가
        if (root == null || root.gameObject == null) return;

        // 레이어 체크
        if (root.gameObject.layer == playerLayer)
        {
            // 충돌 시 Destroy(gameObject) 호출 전 Null 체크 추가
            if (this != null && gameObject != null)
            {
                Destroy(gameObject);
            }
        }
    }
}
