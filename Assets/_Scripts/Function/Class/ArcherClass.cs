using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(menuName = "Class/Archer")]
public class ArcherClass : ClassBase
{    
    public override void BasicAttack(Player p, LayerMask monsterLayer)
    {
        float atk = p.finalStats.attackDamage;     // 플레이어 공격력
        float rng = p.finalStats.attackRange;    // 플레이어 사정거리

        Debug.Log($"[Archer.BasicAttack] atk={atk}, rng={rng}");

        // 가장 가까운 몬스터 찾기
        Collider[] monsters = Physics.OverlapSphere(p.transform.position, rng, monsterLayer);

        Debug.Log($"[Archer.BasicAttack] monsters in range = {monsters.Length}");

        float minDist = Mathf.Infinity;                 // 최소 거리 초기화
        Collider nearest = null;                        // 가장 가까운 몬스터 저장

        foreach (Collider m in monsters)
        {
            float dist = (m.transform.position - p.transform.position).sqrMagnitude;

            Debug.Log($"[Archer.BasicAttack] candidate {m.name}, sqrDist={dist}");

            if (dist < minDist)
            {
                minDist = dist;
                nearest = m;
            }
        }

        if (nearest == null)
        {
            Debug.Log("[Archer.BasicAttack] nearest == null, return");
            return;     // 적이 없으면 /죽는중이라 collider만 남아있는 경우 공격x
        }

        if (nearest.GetComponent<MonsterBase>() == null)
        {
            Debug.Log($"[Archer.BasicAttack] {nearest.name} 에 MonsterBase 없음");
            return;     // 적이 없으면 /죽는중이라 collider만 남아있는 경우 공격x
        }

        //화살 하나 꺼내오기
        ArcherProjectile arrow = p.GetArrow();

        Debug.Log($"[Archer.BasicAttack] arrow = {arrow}");

        //위치/ 회전 초기화
        arrow.transform.position = p.transform.position;
        arrow.transform.rotation = Quaternion.identity;

        //이 화살이 누구 소속인지 알려주기 ( 나중에 ReturnArrow 쓸 수 있게 해주는 코드 )
        arrow.Init(p);

        //타겟과 데미지 전달
        arrow.SetTarget(nearest.transform, atk, rng);

    }
}
