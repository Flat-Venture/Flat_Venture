using UnityEngine;
using FlatVenture.NUH.Player;
using FlatVenture.NUH.Player.State;

/// <summary>
/// 몬스터 주변 일정 범위 내에 들어온 플레이어의 이동 속도를 늦추는 디버프 오라
/// </summary>
public class SlowDebuffAuraTrait : MonsterTrait
{
    [Header("Debuff Settings")]
    [Tooltip("디버프가 적용되는 오라 반경")]
    public float auraRadius = 5f;
    [Tooltip("이동 속도 감소 배율 (예: 0.5 = 50% 느려짐)")]
    public float slowMultiplier = 0.5f;
    [Tooltip("디버프 적용 갱신 주기")]
    public float tickRate = 0.5f;

    private float nextTickTime;
    private bool isPlayerInAura = false;
    private PlayerController cachedPlayer;

    private void Update()
    {
        if (controller == null || !controller.IsAlive || controller.CurrentTarget == null) return;

        if (Time.time >= nextTickTime)
        {
            CheckAura();
            nextTickTime = Time.time + tickRate;
        }
    }

    private void CheckAura()
    {
        float distance = Vector3.Distance(transform.position, controller.CurrentTarget.position);

        if (distance <= auraRadius)
        {
            if (!isPlayerInAura)
            {
                isPlayerInAura = true;
                ApplySlowDebuff();
            }
        }
        else
        {
            if (isPlayerInAura)
            {
                isPlayerInAura = false;
                RemoveSlowDebuff();
            }
        }
    }

    private void ApplySlowDebuff()
    {
        //플레이어 컨트롤러를 가져옴
        if (cachedPlayer == null) 
            cachedPlayer = controller.CurrentTarget.GetComponentInParent<PlayerController>();

        //플레이어의 RuntimeState에 접근하여 속도 디버프 적용
        if (cachedPlayer != null && cachedPlayer.RuntimeState != null)
        {
            float originalSpeed = cachedPlayer.RuntimeState.Source.MoveSpeed;
            cachedPlayer.RuntimeState.SetMoveSpeed(originalSpeed * slowMultiplier);
            
            Debug.Log($"<color=magenta>[Debuff]</color> 플레이어가 둔화되어 이동 속도가 {originalSpeed * slowMultiplier}로 감소했습니다!");
        }
    }

    private void RemoveSlowDebuff()
    {
        //범위를 벗어나면 Source(원본)의 MoveSpeed로 원상 복구
        if (cachedPlayer != null && cachedPlayer.RuntimeState != null)
        {
            float originalSpeed = cachedPlayer.RuntimeState.Source.MoveSpeed;
            cachedPlayer.RuntimeState.SetMoveSpeed(originalSpeed);
            
            Debug.Log($"<color=magenta>[Debuff]</color> 범위를 벗어나 이동 속도가 정상({originalSpeed})으로 돌아왔습니다.");
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0, 1, 0.3f);
        Gizmos.DrawWireSphere(transform.position, auraRadius);
    }
}