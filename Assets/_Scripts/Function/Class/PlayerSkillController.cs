using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 스킬 슬롯 관리
/// AddSkill(중복 스킬이면 레벨업)
/// UseSkill(슬롯에 있는 스킬 사용)
/// 슬롯 꽉찼으면 신규 스킬 불가, 레벨업 카드로 대체 규칙 만들기
/// </summary>
public class PlayerSkillController : MonoBehaviour
{
    public static PlayerSkillController Instance;

    public System.Action OnSkillChanged;

    [Header("스킬 슬롯")]
    public SkillRuntime[] skillSlots = new SkillRuntime[3];                     //스킬 슬롯 리스트

    public List<SkillRuntime> ownedSkills = new List<SkillRuntime>();           //가지고 있는 스킬 리스트

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            skillSlots = new SkillRuntime[3];
            ownedSkills = new List<SkillRuntime>();
        }
        else
        {
            Destroy(gameObject);
            return;
        }

    }

    /// <summary>
    /// 스킬 슬롯 비었는지 확인용 bool
    /// </summary>
    /// <returns></returns>
    public bool HasEmptySlot()
    {
        for (int i = 0; i < skillSlots.Length; i++)
        {
            if (skillSlots[i] == null || skillSlots[i].skillBaseData == null) return true;
        }

        return false;
    }

    /// <summary>
    /// 빈 스킬 슬롯 찾기용 int
    /// </summary>
    /// <returns></returns>
    public int FindFirstEmptySlotIndex()
    {
        for (int i = 0; i < skillSlots.Length; i++)
        {
            if (skillSlots[i] == null || skillSlots[i].skillBaseData == null) return i;
            Debug.Log($"[PSC] this={this.GetHashCode()} Instance={Instance.GetHashCode()}");
        }
        return -1;

    }

    /// <summary>
    /// 현재 가지고 있는 스킬 데이터 찾기
    /// </summary>
    /// <param name="baseData"></param>
    /// <returns></returns>
    public SkillRuntime FindOwnedSkill(SkillBase baseData)
    {
        foreach (var r in ownedSkills)
        {
            if (r.skillBaseData == baseData) return r;
        }
        return null;
    }

    /// <summary>
    /// 스킬 슬롯에 스킬 넣기
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public SkillRuntime GetSkillAtSlot(int index)
    {
        if (index < 0 || index >= skillSlots.Length)
            return null;

        return skillSlots[index];
    }

    /// <summary>
    /// 스킬 선택했을 때
    /// 스킬 추가 or 스킬 레벨업
    /// </summary>
    /// <param name="selectedSkill"></param>
    public void AddSkillOrLevelUp(SkillBase selectedSkill)
    {
        if (selectedSkill == null) { Debug.Log("RETURN: selectedSkill null"); return; }


        //이미 스킬이 있으면 레벨업
        SkillRuntime owned = FindOwnedSkill(selectedSkill);
        if (owned != null)
        {
            owned.LevelUp();

            OnSkillChanged?.Invoke();
            Debug.Log("LEVEL UP");
            return;
        }

        //신규 스킬이면 슬롯이 비어있어야만 추가 가능
        int emptyIndex = FindFirstEmptySlotIndex();
        if (emptyIndex == -1) { Debug.Log("RETURN: no empty slot"); return; }


        SkillRuntime newRuntime = new SkillRuntime(selectedSkill);
        skillSlots[emptyIndex] = newRuntime;
        ownedSkills.Add(newRuntime);

        OnSkillChanged?.Invoke();
    }

    /// <summary>
    /// 해당 슬롯에 있는 스킬 사용하기
    /// </summary>
    /// <param name="slotIndex"></param>
    /// <param name="player"></param>
    public void UseSkillSlot(int slotIndex, Player player)
    {
        //슬롯 인덱스 벗어나면 return
        if (slotIndex < 0 || slotIndex >= skillSlots.Length) return;

        SkillRuntime runtime = skillSlots[slotIndex];
        if (runtime == null) return;

        runtime.Use(player);
    }

    /// <summary>
    /// 던전 끝나고 마을로 돌아올 때 스킬 리셋 시키기
    /// </summary>
    public void ResetAllSkills()
    {
        //Runtime의 스킬레벨 초기화
        foreach (var runtime in ownedSkills)
        {
            runtime.ResetSkillLevel();
        }
        //리스트 비우기
        ownedSkills.Clear();

        //런타임 스킬 슬롯 초기화
        for (int i = 0; i < skillSlots.Length; i++)
        {
            skillSlots[i] = null;
        }

        //여기에 SkillSlotUI.cs 의 Refresh가 들어가 있음
        OnSkillChanged?.Invoke();
    }
}
