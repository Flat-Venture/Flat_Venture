using UnityEngine;

[CreateAssetMenu(menuName = "Class/Mage")]
public class MageClass : ClassBase
{
    public float explosionRadius = 2.5f;      // 폭발 범위

    public override void BasicAttack(Player p, LayerMask monsterLayer)
    {
        float atk = p.finalStats.attackDamage;       // 플레이어 공격력
        float rng = p.finalStats.attackRange;      // 플레이어 사정거리

        // 1) 가장 가까운 적 찾기
        Collider[] monsters = Physics.OverlapSphere(p.transform.position, rng, monsterLayer);

        float minDist = Mathf.Infinity;
        Collider nearest = null;

        foreach (Collider m in monsters)
        {
            float dist = (m.transform.position - p.transform.position).sqrMagnitude;
            if (dist < minDist)
            {
                minDist = dist;
                nearest = m;
            }
        }

        if (nearest == null) return;                      // 적이 없으면 종료

        // 2) 폭발 처리 시작
        Vector3 center = nearest.transform.position;      // 폭발 중심 = 가장 가까운 몬스터 위치

        Collider[] inExplosion = Physics.OverlapSphere(center, explosionRadius, monsterLayer);

        foreach (Collider c in inExplosion)
        {
            MonsterBase mob = c.GetComponent<MonsterBase>();
            if (mob != null)
                mob.TakeDamage(atk);                      // 폭발 데미지 적용
            Debug.Log($"Hit Monster : {mob.name}");
        }

        // 폭발 이펙트 Instantiate 가능
    }
}
