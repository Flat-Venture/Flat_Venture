using UnityEngine;

/// <summary>
/// 주기적으로 플레이어 위치에 독 장판을 생성하는 엘리트 특성
/// </summary>
public class PoisonCasterTrait : MonsterTrait
{
    [Header("Poison Cast Settings")]
    [Tooltip("생성할 독 장판 프리팹")]
    public GameObject poisonPoolPrefab;
    [Tooltip("장판 시전 쿨타임")]
    public float cooldown = 8f;

    private float nextCastTime;

    public override void Setup(MonsterController controller)
    {
        base.Setup(controller);
        //방에 들어오자마자 바로 깔지 않고 2초 뒤 첫 시전
        nextCastTime = Time.time + 2f; 
    }

    private void Update()
    {
        if (controller == null || !controller.IsAlive || controller.CurrentTarget == null) return;

        if (Time.time >= nextCastTime)
        {
            CastPoisonPool();
            nextCastTime = Time.time + cooldown;
        }
    }

    private void CastPoisonPool()
    {
        if (poisonPoolPrefab != null)
        {
            //타겟(플레이어)의 현재 위치를 가져와서 약간 바닥(y=0.1)에 장판을 깝니다.
            Vector3 spawnPos = controller.CurrentTarget.position;
            spawnPos.y = 0.1f; 

            Instantiate(poisonPoolPrefab, spawnPos, Quaternion.identity);
            Debug.Log($"<color=green>[Cast]</color> {gameObject.name}이(가) 플레이어 위치에 독 장판을 생성했습니다!");
        }
    }
}