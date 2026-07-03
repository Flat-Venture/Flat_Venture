using UnityEngine;
using System.Collections.Generic;

public abstract class MonsterTrait : MonoBehaviour
{
    public abstract void OnInitialize(MonsterController controller);
}

public class MonsterController : MonoBehaviour
{
    [Header("States")]
    public float maxHP;
    public float moveSpeed;
    
    private List<MonsterTrait> activeTraite = new List<MonsterTrait>();

    private void Awake()
    {
        //몬스터가 생성될 때 붙어있는 모든 Trait들을 초기화
        var traits = GetComponents<MonsterTrait>();
        foreach (var trait in traits)
        {
            trait.OnInitialize(this);
            activeTraite.Add(trait);
        }
    }
}
