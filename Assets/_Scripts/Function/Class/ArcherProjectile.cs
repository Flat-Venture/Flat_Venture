using UnityEngine;

public class ArcherProjectile : MonoBehaviour
{
    public float speed = 20f;           // 투사체 속도
    public float damage;                // 투사체 데미지
    Transform target;                   // 명중시킬 대상
    public float hitDis = 0.2f;         // 맞은 판정이 나는 몬스터와 투사체와의 거리

    Player owner;                       // 화살 관리하는 플레이어
    Vector3 startPos;                   // 화살 시작 위치
    public float maxDistance;           // 화살 최대 비행 거리

    public void Init(Player owner)
    {
        this.owner = owner;             // 화살 되돌릴 대상 저장
        startPos = transform.position;  // 시작 위치 기억하기
        target = null;                  // 이전 타겟 초기화
        damage = 0;                     // 데미지도 초기화 (보험용)

    }

    // 타겟 설정
    public void SetTarget(Transform target, float damage, float maxDis)
    {
        this.target = target;            // 타겟 저장
        this.damage = damage;            // 데미지 저장
        this.maxDistance = maxDis;
    }

    void Update()
    {
        if (target == null)
        {
            ReturnPool();               //타겟 없으면 회수
            return;
        }

        // 타겟 방향 계산
        Vector3 dir = (target.position - transform.position).normalized;      //유도 할거면 startPos -> trasnform.position으로 바꿔보기
        transform.position += dir * speed * Time.deltaTime; // 이동

        // 몬스터 맞았는지 체크 (근처 도달 체크 / 여기서만 Distance를 쓴 이유는, 근접했는지 한번만 체크하는 방식이라 최적화가 크게 필요없어서 사용 )
        if (Vector3.Distance(transform.position, target.position) < hitDis)
        {
            MonsterBase mob = target.GetComponent<MonsterBase>();
            if (mob != null) mob.TakeDamage(damage);       // 데미지 적용

            ReturnPool();                                  // 투사체 제거 (여기서 리턴?)
            return;
        }

        // 최대 사정거리 나가면 회수하기
        float sqrDist = (transform.position - startPos).sqrMagnitude;
        maxDistance = Player.Instance.finalStats.attackRange;               //화살 최대 비행거리 = 플레이어 사정거리
        if (sqrDist > maxDistance * maxDistance)
        {
            ReturnPool();
            return;
        }
    }

    void ReturnPool()
    {
        if(owner != null)
        {
            owner.ReturnArrow(this);        //플레이어에게 화살 돌려보냄 -> ObjectPool.Return 호출된거임
        }
        else
        {
            //owner 설정 실패 보험용
            Debug.LogWarning("ArcherProjectile owner 없음. Destroy로 대체");
            Destroy(gameObject);
        }
    }
}
