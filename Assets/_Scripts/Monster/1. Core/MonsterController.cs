using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 모든 몬스터의 뼈대가 되는 메인 컨트롤러
/// </summary>
public class MonsterController : MonoBehaviour, IPlayerAttackTarget
{
    [Header("Base States")]
    public float maxHP = 100f;
    public float moveSpeed = 5f;
    
    public float CurrentHP { get; private set;}

    //모든 몬스터가 공유할 현재 타겟
    public Transform CurrentTarget { get; set; }

    //넉백 당하는 중인지 체크
    public bool IsStunned { get; set; }

    //현재 공격을 시전 중인지 체크
    public bool IsAttacking { get; set; }

    //피격 상태를 알려주는 콜백
    public Action<float, Vector3> onHitCallback;

    //사망 시 외부에 알리기 위한 콜백
    public Action<MonsterController> onDeathCallback;

    //몬스터에 붙어 있는 모든 특성 리스트
    private List<MonsterTrait> traits = new List<MonsterTrait>();

    //생존 여부를 플레이어 스크립트에 알려줌
    public bool IsAlive => CurrentHP > 0;

    //몬스터의 트랜스폼을 플레이어 스크립트에 제공
    public Transform TargetTransform => transform;

    private void Awake()
    {
        //HP 초기화
        CurrentHP = maxHP;

        //몬스터가 생성될 때 붙어있는 모든 Trait들을 초기화
        traits.AddRange(GetComponentsInChildren<MonsterTrait>());
        foreach (var trait in traits)
        {
            trait.Setup(this);
        }
    }

    /// <summary>
    /// 플레이어 스크립트에서 호출하는 기본 데미지
    /// </summary>
    public void TakeDamage(float damage)
    {
        Vector3 knockbackDir = Vector3.zero;

        //현재 쫓고 있는 타겟이 명확하다면 그 반대 방향으로 밀려남
        if (CurrentTarget != null)
        {
            knockbackDir = (transform.position - CurrentTarget.position).normalized;
            knockbackDir.y = 0;
        }

        float defaultKnockbackPower = 15f;
        ApplyDamageAndKnockback(damage, knockbackDir * defaultKnockbackPower);
    }

    /// <summary>
    /// 플레이어나 다른 요소로 데미지 입었을 때
    /// </summary>
    public void ApplyDamageAndKnockback(float damage, Vector3 knockbackForce)
    {
        //이미 죽은 상태면 무시
        if (CurrentHP <= 0) return;

        //TODO: 이곳에서 방어력 감소, 회피율 계산 등의 식을 적용

        CurrentHP -= damage;
        Debug.Log($"[Monster] {gameObject.name}이(가) {damage} 피해를 입음. 남은 체력: {CurrentHP}");

        //넉백 담당 스크립트에게 맞았다는 사실과 힘을 전달
        onHitCallback?.Invoke(damage, knockbackForce);

        //몬스터 몸에 붙은 특성들에게 맞았다고 알려줌 (ex. 맞으면 반격하는 특성이 반응하도록)
        foreach (var trait in traits) trait.OnTakeDamage(damage);

        if (CurrentHP <= 0) Die();
    }

    /// <summary>
    /// 방 생성 시 랜덤 엘리트 특성 등을 동적으로 추가
    /// </summary>
    /// <param name="newTrait"></param>
    public void AttachDynamicTrait(MonsterTrait newTrait)
    {
        if (newTrait != null && !traits.Contains(newTrait))
        {
            traits.Add(newTrait);

            //부착 즉시 초기화하여 컨트롤러와 연결
            newTrait.Setup(this);
        }
    } 

    private void Die()
    {
        Debug.Log($"[Monster] {gameObject.name} 사망");

        //몬스터 특성들에게 본인의 사망을 알림 (ex. 죽을 때 자폭하는 특성이 작동하도록)
        foreach (var trait in traits) trait.OnDeath();

        //외부에 본인의 사망을 알림
        onDeathCallback?.Invoke(this);

        //오브젝트 파괴
        Destroy(gameObject);
    }
}
