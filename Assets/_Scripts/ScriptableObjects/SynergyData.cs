using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "SynergyData", menuName = "Scriptable Objects/SynergyData")]
public class SynergyData : ScriptableObject
{
    public Sprite synergyIcon;                      // 시너지 아이콘
    public Enums.ItemSynergy synergyType;           // 시너지 타입
    public string synergyName;                      // 시너지 명칭
    public string synergyDescription;               // 시너지 설명
    public int maxLevel;                            // 시너지 최대 활성화 개수
    public List<SynergyLevel> levels;               // 시너지 활성화 개수에 따른 추가 능력치

    private Dictionary<int, Stats> statsByLevel;    // 시너지 레벨에 따른 스탯 딕셔너리

    // levels 리스트 requiredLines를 기준으로 오름차순 정렬
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (levels != null)
        {
            levels.Sort((a, b) => a.requiredLines.CompareTo(b.requiredLines));
        }

        statsByLevel = null;
    }
#endif

    // 딕셔너리 초기화
    private void InitStatsByLevel()
    {
        if(statsByLevel != null)
        {
            return;
        }

        try
        {
            // level 리스트 데이터를 dictionary로 변환
            statsByLevel = levels.ToDictionary(x => x.requiredLines, x => x.bonusStats);
        }
        catch (System.ArgumentException ex)
        {
            Debug.LogError("중복된 키 발견");
            // 딕셔너리 비워서 시스템 작동하지 않게 강제
            statsByLevel = new Dictionary<int, Stats>();
        }

    }

    public Stats GetBonusStats(int level)
    {
        // 딕셔너리 초기화
        InitStatsByLevel();

        // 입력받은 레벨이 시너지 최고 레벨보다 높으면 최고 레벨로 바꿔줌
        if(level > maxLevel)
        {
            level = maxLevel;
        }

        // 시너지 레벨에 따른 스탯 조회
        if(statsByLevel.TryGetValue(level, out Stats bonusStats))
        {
            return bonusStats;
        }

        // 현재 시너지 레벨이 존재하지 않을 때, 현재 시너지 레벨보다 낮고,
        // 그 중에서 가장 큰값을 찾아서 스탯 조회
        int floorKey = statsByLevel.Keys.Where(key => key < level).LastOrDefault();

        if(floorKey > 0 && statsByLevel.TryGetValue(floorKey, out Stats floorStats))
        {
            return floorStats;
        }

        // 없으면 null 반환
        Debug.LogError("해당 레벨이 없음");
        return null;
    }
}

[System.Serializable]
public class SynergyLevel
{
    public int requiredLines;

    public Stats bonusStats;
}