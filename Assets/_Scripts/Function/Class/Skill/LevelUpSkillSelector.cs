using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 레벨업 시 스킬 카드 3장 어떻게 나올지 고정해주기
/// </summary>
public class LevelUpSkillSelector
{
    private ClassData classData;
    private PlayerSkillController skillController;

    public LevelUpSkillSelector(ClassData classDataSO, PlayerSkillController controller)
    {
        classData = classDataSO;
        skillController = controller;
    }

    public List<SkillCardData> Create3Cards()
    {
        Debug.Log($"skillSlots length: {skillController.skillSlots.Length}");
        Debug.Log($"ownedSkills ref: {skillController.ownedSkills}");
        Debug.Log("Create3Cards 호출됨");
        Debug.Log($"classData null? {classData == null}");
        Debug.Log($"ownedSkills Count: {skillController.ownedSkills.Count}");
        Debug.Log($"classSkills Count: {classData.classSkills.Count}");

        List<SkillCardData> cards = new List<SkillCardData>();

        bool hasEmptySlot = skillController.HasEmptySlot();


        if (!hasEmptySlot)                                          //슬롯 꽉 찼을 때 
        {
            foreach (var runtime in skillController.ownedSkills)    //가지고 있는 스킬 레벨업 카드로 바꾸기
            {
                cards.Add(CreateLevelUpCard(runtime));
                if (cards.Count >= 3) break;                        //카드 갯수 3장이상이면 Add 멈추기
            }

            //ownedSkills가 3개면 카드 완성
            return cards;
        }

        //슬롯 여유 있으면 신규+레벨업 섞어서 3개
        List<SkillBase> newCandidates = new List<SkillBase>();

        foreach(var s in classData.classSkills)
        {
            if(skillController.FindOwnedSkill(s) == null)
            {
                newCandidates.Add(s);
            }
        }

        List<SkillRuntime> levelUpCandidates = new List<SkillRuntime>(skillController.ownedSkills);

        //신규 카드가 있으면 최소 1장은 신규 스킬 보장 (나중에 스킬 갯수 늘어났을때 대비)
        if( newCandidates.Count > 0 )
        {
            SkillBase pickedNew = newCandidates[Random.Range(0,newCandidates.Count)];
            cards.Add(CreateNewCard(pickedNew));
            newCandidates.Remove(pickedNew);
        }

        //나머지 채우기 (신규 / 레벨업 섞기)
        while(cards.Count < 3)
        {
            bool canPickNew = newCandidates.Count > 0;
            bool canPickUp = levelUpCandidates.Count > 0;

            if (!canPickNew && !canPickUp) break;

            bool pickNew = canPickNew && (!canPickUp || Random.value < 0.5f);

            if(pickNew)
            {
                SkillBase s = newCandidates[Random.Range(0, newCandidates.Count)];
                cards.Add(CreateNewCard(s));
                newCandidates.Remove(s);
            }
            else
            {
                SkillRuntime r = levelUpCandidates[Random.Range(0, levelUpCandidates.Count)];
                cards.Add(CreateLevelUpCard(r));
                levelUpCandidates.Remove(r);
            }
        }
        return cards;
    }

    private SkillCardData CreateNewCard(SkillBase skill)
    {
        return new SkillCardData
        {
            cardType = Enums.SkillCardType.NewSkill,
            skillBaseData = skill,
            nameText = skill.skillName,
            descriptionText = $"NEW SKILL\n{skill.desc}",
            icon = skill.icon
        };
    }

    private SkillCardData CreateLevelUpCard(SkillRuntime runtime)
    {
        return new SkillCardData
        {
            cardType = Enums.SkillCardType.LevelUpSkill,
            skillBaseData = runtime.skillBaseData,
            nameText = runtime.skillBaseData.skillName,
            descriptionText = $"LEVEL UP > Lv.{runtime.currentLevel + 1}",
            icon = runtime.skillBaseData.icon
        };
    }
}
