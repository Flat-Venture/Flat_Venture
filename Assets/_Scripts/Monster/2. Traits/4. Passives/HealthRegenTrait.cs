using UnityEngine;

/// <summary>
/// 일정 시간마다 몬스터 스스로 체력을 자동 회복하는 특성
/// </summary>
public class HealthRegenTrait : MonsterTrait
{
    [Header("Regeneration Settings")]
    [Tooltip("체력 회복 틱 주기 (초)")]
    public float tickRate = 2f;
    [Tooltip("1틱 당 회복할 체력량")]
    public float regenAmount = 5f;

    private float nextRegenTime;

    public override void Setup(MonsterController controller)
    {
        base.Setup(controller);
        //생성 직후 바로 회복하지 않고 첫 주기 대기
        nextRegenTime = Time.time + tickRate;
    }

    private void Update()
    {
        //몬스터가 살아있을 때만 회복 로직 작동
        if (controller == null || !controller.IsAlive) return;

        if (Time.time >= nextRegenTime)
        {
            //체력이 꽉 차지 않았을 때만 회복 시도
            if (controller.CurrentHP < controller.maxHP)
            {
                Debug.Log($"<color=green>[Regen]</color> {gameObject.name}이(가) 체력을 {regenAmount}만큼 회복 시도합니다.");
                
                //TODO: MonsterController.cs에 Heal(float amount) 함수를 생성한 뒤 이 주석을 풀고 사용하세요.
                //controller.Heal(regenAmount);
            }
            
            //다음 회복 타이머 갱신
            nextRegenTime = Time.time + tickRate;
        }
    }
}