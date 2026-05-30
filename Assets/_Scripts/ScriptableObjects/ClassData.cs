using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 1. 직업 데이터를 저장하는 SO
/// 2. 직업 이름, 설명, 능력치 보너스 등을 포함
/// </summary>
[CreateAssetMenu(fileName = "ClassData", menuName = "Scriptable Objects/ClassData")]
public class ClassData : ScriptableObject
{
    public Enums.ClassType classType;           // 직업 타입
    public Sprite classImage;                   // 각 직업별 초상화
    
    public int cLevel;                          // 직업 레벨
    public float currentExp;                    // 직업 현재 경험치
    public float maxExp;                        // 직업 최대 경험치
    public int classPoints;                     // 직업 포인트 (추후에 이용)

    public Stats cBaseStat;                     // 직업 능력치

    public Stats cTraitStat;                    // 직업 특성 포인트로 올라가는 능력치

    public List<SkillBase> classSkills = new List<SkillBase>();     //직업 스킬 리스트

    // 레벨에 따라 올라가는 수치들
    // 이건 변수명 안정해서 얘기해 봐야함.
    public float hpPerLevel;                    // 레벨당 체력 증가량
    public float mpPerLevel;                    // 레벨당 마나 증가량
    public float attackDamagePerLevel;          // 레벨당 공격력 증가량
    public float criticalRatePerLevel;          // 레벨당 치명타 확률 증가량
    public float criticalDamagePerLevel;        // 레벨당 치명타 피해 증가량 -> 이건 없어도 될거같음

    public ClassBase classLogic;                // 직업별 행동 로직(기본 공격)
    // maxExp 계산용 변수가 필요한가?
}
