using UnityEngine;
using System.Collections;

public class DashAttackTrait : MonsterTrait
{
    [Header("Dash Settings")]
    public float dashRange = 6f;
    public float dashSpeed = 15f;
    public float windUpTime = 0.5f;
    public float postDashDelay = 0.5f;
    public float dashCooldown = 3f;

    private bool isDashing = false;
    private Rigidbody rb;
    private float nextDashTime = 0f;

    public override void Setup(MonsterController controller)
    {
        base.Setup(controller);
        rb = GetComponent<Rigidbody>();
        nextDashTime = Time.time + dashCooldown;
    }

    private void Update()
    {
        //타겟이 없거나 스턴 상태면 빠져나감
        if (controller == null || !controller.IsAlive || controller.IsStunned || controller.CurrentTarget == null) return;
        
        //이미 다른 공격을 하고 있다면 중복 실행 방지
        if (controller.IsAttacking || isDashing) return;

        float distance = Vector3.Distance(transform.position, controller.CurrentTarget.position);

        //사거리 안에 들어오고 쿨타임이 돌았을 때 돌진 시작
        if (distance <= dashRange && Time.time >= nextDashTime)
        {
            StartCoroutine(DashRoutine());
        }
    }

    private IEnumerator DashRoutine()
    {
        //이동 스크립트가 멈추도록 공격 상태로 전환
        controller.IsAttacking = true;
        isDashing = true;

        //돌진 전기를 모으는 대기시간
        yield return new WaitForSeconds(windUpTime);

        //대기 중 죽었다면 안전하게 종료
        if (controller == null || !controller.IsAlive) yield break;

        Vector3 dashDirection = (controller.CurrentTarget.position - transform.position).normalized;
        dashDirection.y = 0;

        float dashDuration = dashRange / dashSpeed;
        float timer = 0f;

        //물리 엔진을 이용해 목표방향으로 빠르게 이동시킴
        while (timer < dashDuration)
        {
            if (rb != null)
            {
                rb.linearVelocity = dashDirection * dashSpeed;
            }
            timer += Time.deltaTime;
            yield return null;
        }

        //돌진이 끝나면 제자리에서 멈춤
        if (rb != null) rb.linearVelocity = Vector3.zero;

        //돌진 후 숨을 고르는 대기시간
        yield return new WaitForSeconds(postDashDelay);

        //모든 공격이 끝났으므로 상태 해제
        controller.IsAttacking = false;
        isDashing = false;
        nextDashTime = Time.time + dashCooldown;
    }
}