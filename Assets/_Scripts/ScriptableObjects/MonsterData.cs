using UnityEngine;

/// <summary>
/// 1. 몬스터 데이터를 저장하는 SO
/// 2. 몬스터 이름, 능력치, 드롭 골드, 드롭 경험치 등을 포함
/// 3. 몬스터의 행동 패턴이나 AI 정보도 포함
/// </summary>
[CreateAssetMenu(fileName = "MonsterData", menuName = "Scriptable Objects/MonsterData")]
public class MonsterData : ScriptableObject
{
    public Enums.MonsterRank monsterRank;       // 몬스터 등급(종류)

    public int mId;                             // 몬스터 ID
    public string monsterName;                  // 몬스터 이름
    public float maxHp;                         // 몬스터 MaxHp
    public float attackDamage;                  // 몬스터 공격력
    public float attackRange;                   // 몬스터 공격 범위
    public float attackSpeed;                   // 몬스터 공격 속도
    public float projectileCount;               // 몬스터 투사체 개수
    public float projectileSpeed;               // 몬스터 투사체 속도
    public float moveSpeed;                     // 몬스터 이동 속도




    public float dropGold;                      // 몬스터 드롭 골드
    public float dropExp;                       // 몬스터 드롭 경험치

    public float shield;                        // 몬스터 보호막

    public GameObject mosterPrefab;             // 몬스터 프리팹

    // 이건 보스한테만 필요
    public float stunGauge;                      // 몬스터 기절 게이지


}

