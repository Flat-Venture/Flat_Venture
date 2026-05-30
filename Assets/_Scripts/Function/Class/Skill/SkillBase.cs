using UnityEngine;

/// <summary>
/// 모든 스킬이 공통으로 가져야 하는 틀
/// 이름
/// 아이콘
/// 기본 쿨타임
/// 실행 메서드
/// </summary>
public abstract class SkillBase : ScriptableObject
{
    //스킬 이름
    public string skillName;

    //스킬 아이콘
    public Sprite icon;

    //스킬 쿨타임
    public float cooldown = 1f;

    //스킬 설명
    public string desc;

    /// <summary>
    /// 스킬 실행 함수
    /// </summary>
    /// <param name="player"></param>
    /// <param name="skillLevel"></param>
    public abstract void Execute(Player player, int skillLevel);
    
}
