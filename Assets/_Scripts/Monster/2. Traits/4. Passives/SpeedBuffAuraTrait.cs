using UnityEngine;
using System.Collections;

/// <summary>
/// 주기적으로 주변 아군 몬스터들의 이동 속도를 증가시키는 버프
/// </summary>
public class SpeedBuffAuraTrait : MonsterTrait
{
    [Header("Buff Settings")]
    [Tooltip("버프가 닿는 반경")]
    public float buffRadius = 6f;
    [Tooltip("이동 속도 증가 배율 (예: 1.5 = 150%)")]
    public float speedMultiplier = 1.5f;
    [Tooltip("버프 지속 시간")]
    public float buffDuration = 3f;
    [Tooltip("버프 시전 쿨타임")]
    public float buffCooldown = 8f;

    [Header("Targeting")]
    [Tooltip("아군 몬스터로 인식할 레이어 (최적화 필수)")]
    public LayerMask allyLayer;

    private float nextBuffTime;

    public override void Setup(MonsterController controller)
    {
        base.Setup(controller);
        nextBuffTime = Time.time + buffCooldown;
    }

    private void Update()
    {
        if (controller == null || !controller.IsAlive || controller.IsStunned) return;

        if (Time.time >= nextBuffTime)
        {
            CastSpeedBuff();
            nextBuffTime = Time.time + buffCooldown;
        }
    }

    private void CastSpeedBuff()
    {
        Debug.Log($"<color=yellow>[Speed Buff]</color> {gameObject.name}이(가) 광역 이속 버프를 시전합니다!");

        Collider[] colliders = Physics.OverlapSphere(transform.position, buffRadius, allyLayer);
        
        for (int i = 0; i < colliders.Length; i++)
        {
            MonsterController ally = colliders[i].GetComponent<MonsterController>();
            
            if (ally != null && ally.IsAlive)
            {
                //버프 지속시간 관리를 위해 코루틴 실행
                StartCoroutine(ApplyBuffRoutine(ally));
            }
        }
    }

    private IEnumerator ApplyBuffRoutine(MonsterController ally)
    {
        //기존 속도 저장 후 배율 적용
        float originalSpeed = ally.moveSpeed;
        ally.moveSpeed = originalSpeed * speedMultiplier;
        
        Debug.Log($"<color=yellow>[Buff Applied]</color> {ally.gameObject.name}의 속도가 {ally.moveSpeed}로 증가!");

        //지정된 시간만큼 대기
        yield return new WaitForSeconds(buffDuration);

        //시간이 지나면 원래 속도로 복구 (그 사이 몬스터가 죽지 않았는지 확인)
        if (ally != null) 
        {
            ally.moveSpeed = originalSpeed;
            Debug.Log($"<color=yellow>[Buff Ended]</color> {ally.gameObject.name}의 속도가 정상화되었습니다.");
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0.9f, 0, 0.3f);
        Gizmos.DrawWireSphere(transform.position, buffRadius);
    }
}
