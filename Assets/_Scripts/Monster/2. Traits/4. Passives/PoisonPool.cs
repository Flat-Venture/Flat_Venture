using UnityEngine;

/// <summary>
/// 바닥에 깔려서 범위 내 플레이어에게 지속 데미지를 주는 장판 오브젝트
/// </summary>
public class PoisonPool : MonoBehaviour
{
    [Tooltip("장판이 유지되는 시간")]
    public float duration = 5f;
    [Tooltip("1틱 당 데미지")]
    public float damage = 2f;
    [Tooltip("데미지가 들어가는 주기 (초)")]
    public float tickRate = 1f;

    private float nextTickTime;

    private void Start()
    {
        // 생성되면 duration 초 뒤에 스스로 파괴됩니다.
        Destroy(gameObject, duration);
    }

    private void OnTriggerStay(Collider other)
    {
        //트리거 안에 있는 오브젝트가 Player이고, 틱 시간이 지났다면 실행
        if (Time.time >= nextTickTime && other.CompareTag("Player"))
        {
            PlayerHealthController health = other.GetComponentInParent<PlayerHealthController>();
            if (health != null)
            {
                Debug.Log($"<color=green>[Poison Pool]</color> 독 장판 위에서 {damage}의 피해를 입습니다!");
                health.TakeDamage(damage);
                
                //다음 데미지가 들어갈 시간 갱신
                nextTickTime = Time.time + tickRate;
            }
        }
    }
}
