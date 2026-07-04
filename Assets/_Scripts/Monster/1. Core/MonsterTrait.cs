using UnityEngine;

/// <summary>
/// 몬스터에 부착할 수 있는 모든 행동/패시브/특성의 기본 규격
/// </summary>
public abstract class MonsterTrait : MonoBehaviour
{
    protected MonsterController controller;

    public virtual void Setup(MonsterController controller)
    {
        this.controller = controller;
    }

    public virtual void OnBattleStarted(){}
    public virtual void OnTakeDamage(float damage){}
    public virtual void OnDeath(){}
}
