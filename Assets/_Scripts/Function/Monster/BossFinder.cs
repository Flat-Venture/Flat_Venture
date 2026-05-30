using UnityEngine;
using static Enums;

public class BossFinder : MonoBehaviour
{
    public float detectRadius = 30f;

    int bossLayer;
    bool bound;

    void Update()
    {
        if (bound) return;
        if (BossUIBinder.Instance == null) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, detectRadius);

        foreach (var h in hits)
        {
            MonsterBase monster = h.GetComponentInParent<MonsterBase>();
            if (monster == null) continue;

            if (monster.md == null) continue;

            if (monster.md.monsterRank != MonsterRank.Boss) continue;

            BossBase boss = monster as BossBase;
            if (boss == null) continue;

            BossUIBinder.Instance.Bind(boss);
            bound = true;
            return;
        }
    }
}
