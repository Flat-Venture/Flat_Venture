using UnityEngine;

[CreateAssetMenu(menuName = "Class/Warrior")]
public class WarriorClass : ClassBase
{
    public override void BasicAttack(Player p, LayerMask monster)
    {
        float atk = p.finalStats.attackDamage;          //Player 스탯 (공격력)
        float rng = p.finalStats.attackRange;           //Player 스탯 (사정거리)

        Collider[] monsters = Physics.OverlapSphere(p.transform.position, rng, monster);        //공격 범위안에 들어온 몬스터 배열

        float minDist = Mathf.Infinity;
        Collider nearest = null;                //공격 범위 안에 들어온 몬스터 중 가장 가까운 몬스터 넣을 변수

        foreach (Collider m in monsters)        //공격 범위안에 들어온 몬스터 배열 안에서
        {
            float dist = (m.transform.position - p.transform.position).sqrMagnitude;
            if (dist < minDist)
            {
                minDist = dist;
                nearest = m;                    //가장 가까운놈 찾아주고 변수에 넣어주고
            }
        }

        if (nearest == null) return;

        //가장 가까운 친구 각도 저장
        Vector3 nearestDir = (nearest.transform.position - p.transform.position).normalized;

        foreach(Collider m in monsters)         //OverlapSphere 안에 들어온 몬스터 중
        {
            Vector3 dir = (m.transform.position - p.transform.position).normalized;     //몬스터와 플레이어와의 각도 저장해주고
            float angle = Vector3.Angle(nearestDir, dir);                               //플레이어와 가장 가까운 몬스터, 범위내 몬스터 각도 비교해본 후

            if (angle <= 45f) // Warrior 기본 : 좁은 전방 공격                              +-20도 내에 있으면
            {
                MonsterBase mob = m.GetComponent<MonsterBase>();                        //
                if (mob != null) mob.TakeDamage(atk);
                Debug.Log($"Hit Monster : {mob.name}");
            }
        }

    }
}
