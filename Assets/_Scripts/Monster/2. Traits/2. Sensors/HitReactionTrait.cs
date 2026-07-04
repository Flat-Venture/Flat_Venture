using System.Collections;
using UnityEngine;

/// <summary>
/// 피격 시 넉백 물리 효과와 기절 상태를 관리
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class HitReactionTrait : MonsterTrait
{
    [Header("Hit Reaction Settings")]
    [Tooltip("피격 시 뒤로 밀려나는 동안 움직이지 못하는 시간(초)")]
    public float stunDuration = 0.2f;
    [Tooltip("피격 시 몬스터 빨갛게 깜빡이는 시간(초)")]
    public float flashDuration = 0.1f;
    private Rigidbody rb;
    private Renderer meshRenderer;
    private Color originalColor;

    public override void Setup(MonsterController controller)
    {
        base.Setup(controller);
        rb = GetComponent<Rigidbody>();

        //자식 오브젝트에 있는 SpriteRenderer를 찾아서 연결하고, 원래 색상 저장
        meshRenderer = GetComponentInChildren<Renderer>();
        if (meshRenderer != null) originalColor = meshRenderer.material.color;
    
        //맞을 때 마다 HandleHit 함수를 실행하도록 구독
        controller.onHitCallback += HandleHit;
    }

    private void OnDestroy()
    {
        //오브젝트 파괴 시 메모리 누스 방지용 구독 해제
        if (controller != null) controller.onHitCallback -= HandleHit;
    }

    private void HandleHit(float damage, Vector3 knockbackForce)
    {
        //연속으로 맞으면 넉백 타이머를 초기화하고 처음부터 다시 밀려남
        StopAllCoroutines();
        
        //넉백 로직과 깜빡임 로직을 동시에 실행
        StartCoroutine(ApplyKnockbackRoutine(knockbackForce));
        if (meshRenderer != null) StartCoroutine(FlashRedRoutine());
    }

    private IEnumerator ApplyKnockbackRoutine(Vector3 knockbackForce)
    {
        //멈춤
        controller.IsStunned = true;

        //Y축(공중)으로 튀어오르는 것 방지
        knockbackForce.y = 0;

        //기존에 걸어가던 관성을 없애고 넉백 힘 적용
        rb.linearVelocity = Vector3.zero;
        rb.AddForce(knockbackForce, ForceMode.Impulse);

        //TODO: 스프라이트를 빨갛게 깜박이거나 피격 애니메이션 실행

        //지정된 스턴 시간 동안 대기
        yield return new WaitForSeconds(stunDuration);

        //스턴 종료. 관성을 잡고 다시 이동권 반환
        rb.linearVelocity = Vector3.zero;
        controller.IsStunned = false;
    }

    /// <summary>
    /// 피격 시 빨갛게 깜빡이는 시각 효과
    /// </summary>
    private IEnumerator FlashRedRoutine()
    {
        //스프라이트 색상을 빨간색으로 변경
        meshRenderer.material.color = Color.red;

        //지정된 시간 동안 대기
        yield return new WaitForSeconds(flashDuration);

        //원래 색상으로 복구
        meshRenderer.material.color = originalColor;
    }
}
