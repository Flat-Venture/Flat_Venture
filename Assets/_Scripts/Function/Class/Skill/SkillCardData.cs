using UnityEngine;

[System.Serializable]
public class SkillCardData
{
    public Enums.SkillCardType cardType;    //스킬 타입(신규 스킬 or 보유 스킬(레벨업))
    public SkillBase skillBaseData;         //스킬 데이터

    public string nameText;                 //스킬 이름
    public string descriptionText;          //스킬 설명
    public Sprite icon;                     //스킬 아이콘
}
