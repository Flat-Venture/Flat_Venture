using UnityEngine;
using System.Collections;

public class StealthOnHitTrait : MonsterTrait
{
    [Header("Stealth Settings")]
    [Tooltip("은신 유지 시간")]
    public float stealthDuration = 2f;
    [Tooltip("은신 발동 쿨타임")]
    public float cooldown = 10f;
    
    private float nextStealthTime;
    private Renderer[] renderers;

    public override void Setup(MonsterController controller)
    {
        base.Setup(controller);
        
        //자식 오브젝트의 모든 렌더러 캐싱
        renderers = GetComponentsInChildren<Renderer>();
    }

    public override void OnTakeDamage(float damage)
    {
        base.OnTakeDamage(damage);

        //쿨타임 도달 확인 후 은신 실행
        if (Time.time >= nextStealthTime)
        {
            StartCoroutine(StealthRoutine());
            nextStealthTime = Time.time + cooldown;
        }
    }

    private IEnumerator StealthRoutine()
    {
        //모든 렌더러 비활성화로 은신 처리
        ToggleRenderers(false);
        Debug.Log($"// {gameObject.name} 피격 시 은신 발동");
        
        //TODO: 필요 시 몬스터 타겟팅 해제 로직 추가

        //지정된 은신 시간 대기
        yield return new WaitForSeconds(stealthDuration);

        //렌더러 원상 복구로 은신 해제
        ToggleRenderers(true);
        Debug.Log($"// {gameObject.name} 은신 해제");
    }

    private void ToggleRenderers(bool state)
    {
        //배열 반복문으로 렌더러 상태 일괄 변경
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null) renderers[i].enabled = state;
        }
    }
}