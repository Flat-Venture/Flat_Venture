using UnityEngine;
using static Enums;

public class PlayerAttack : MonoBehaviour
{
    //기본공격 쿨타임용 변수
    float nextAtkTime = 0f;

    //적이 들어있는 레이어 (monster로 설정)
    public LayerMask monsterLayer;

    //Player불러오기
    Player player;

    // 외부참조용
    public bool canAtt = true; 

    private void Start()
    {
        player = Player.Instance;
    }

    void Update()
    {
        if (!canAtt) return;
        if (!GameStateManager.Instance.CanPlayerControl()) return;
        TryAttack();
    }

    /// <summary>
    /// 주변 몬스터 있는지 체크
    /// 쿨타임 체크
    /// 공격 시도
    /// </summary>
    /// <returns></returns>
    public bool TryAttack()
    {

        float atkSpd = player.finalStats.attackSpeed;
        float atkRng = player.finalStats.attackRange;

        //몬스터가 없으면 공격 안함
        Collider target = FindNearestMonster(atkRng);
        if (target == null) return false;

        // 쿨타임 체크 (쿨타임 안돌면 공격 안함
        if (Time.time < nextAtkTime)
        {
            return false;
        }

        //다음 공격 시간 설정
        nextAtkTime = Time.time + (1f / atkSpd);

        //현재 직업 가져오기
        ClassBase c = player.classStat.classLogic;
        if (c == null)
        {
            return false;
        }

        Debug.Log($"[TryAttack] using class logic = {c.name}");

        // 여기서 방향 확정 해주고
        Vector3 totarget = (target.transform.position - player.transform.position);
        totarget.y = 0f;
        totarget.Normalize();

        player.SetFacing(totarget.x);

        //애니메이션 넣어주기
        player.animCtrl.ChangeState(PlayerAnimState.Attack);

        //직업의 기본 공격 실행
        c.BasicAttack(player, monsterLayer);

        //사운드
        SoundManager.Instance.PlaySFX("baseAttack");

        //각도 설정
        Quaternion rot = Quaternion.LookRotation(totarget);

        rot *= Quaternion.Euler(0f, 90f, 0f);

        //이펙트 실행
        EffectManager.Instance.PlayEffect(
            EffectType.BaseAttack,
            transform.position,
            rot,
            player.transform
            );

        Debug.Log("TryAttack 실행");
        return true;
    }

    Collider FindNearestMonster(float range)
    {
        Collider[] monsters = Physics.OverlapSphere(
            transform.position,
            range,
            monsterLayer
        );

        float minDist = Mathf.Infinity;
        Collider nearest = null;

        foreach (var m in monsters)
        {
            float d = (m.transform.position - transform.position).sqrMagnitude;
            if (d < minDist)
            {
                minDist = d;
                nearest = m;
            }
        }

        return nearest;
    }

}
