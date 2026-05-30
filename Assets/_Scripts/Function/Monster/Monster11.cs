using System.Collections;
using UnityEngine;

public class Monster11 : MonsterBase
{
    float timer = 0f;

    [Header("Pull")]
    public float pullRange = 8f;
    public float pullCooldown = 4f;
    public float pullDuration = 0.35f;
    public float pullStrength = 12f; // 끌어당기는 힘의 세기

    [Header("Pull Effect")]
    public GameObject pullEffectPrefab;
    public float effectHeight = 0.05f;   // 바닥에서 살짝 띄우기


    float lastPull = -999f;
    bool pulling = false;

    protected override void Idle()
    {
        agent.isStopped = true;
        if (player == null) return;

        if (Vector3.Distance(transform.position, player.transform.position) <= detectRange)
        {
            state = Enums.MonsterState.Move;
        }
    }

    protected override void Move()
    {
        if (player == null || md == null) return;

        float dis = Vector3.Distance(transform.position, player.transform.position);

        agent.isStopped = false;
        agent.updateRotation = true;
        agent.SetDestination(player.transform.position);

        if (dis <= pullRange && Time.time >= lastPull + pullCooldown)
        {
            state = Enums.MonsterState.Attack;
            timer = 0f;
        }
    }

    protected override void Attack()
    {
        if (player == null || pulling)
        {
            state = Enums.MonsterState.Move;
            return;
        }

        // 풀 스킬 1회 사용
        lastPull = Time.time;
        StartCoroutine(CoPullPlayer());

        state = Enums.MonsterState.Move;
    }

    IEnumerator CoPullPlayer()
    {
        pulling = true;

        Transform pt = player.transform;

        // 몬스터 중심 이펙트 생성
        GameObject effect = null;
        if (pullEffectPrefab != null)
        {
            Vector3 pos = transform.position;
            pos.y += effectHeight;

            effect = Instantiate(pullEffectPrefab, pos, Quaternion.Euler(90f, 0f, 0f));
        }

        float t = 0f;
        while (t < pullDuration)
        {
            t += Time.deltaTime;

            // 몬스터 중심으로 끌어당김
            Vector3 dir = (transform.position - pt.position);
            dir.y = 0f;
            dir = dir.normalized;

            pt.position += dir * pullStrength * Time.deltaTime;
            
            yield return null;
        }

        // 이펙트 제거
        if (effect != null) Destroy(effect);

        pulling = false;
    }


}
