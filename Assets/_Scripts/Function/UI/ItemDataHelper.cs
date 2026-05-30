using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

public static class ItemDataHelper
{
    // 스탯의 한글 명칭 맵핑
    public static Dictionary<string, string> statName = new Dictionary<string, string>()
    {
        {"maxHp", "최대 체력" },
        {"maxMp", "최대 마나" },
        {"mpRegen", "마나 재생 속도" },
        {"maxDashCount", "최대 대시 횟수" },
        {"dashRegen", "대시 재생 속도" },
        {"dashDistance", "대시 거리" },
        {"attackDamage", "공격력" },
        {"attackRange", "사거리" },
        {"attackSpeed", "공격 속도" },
        {"projectileCount", "투사체 개수" },
        {"projectileSpeed", "투사체 속도" },
        {"moveSpeed", "이동 속도" },
        {"criticalRate", "치명타 확률" },
        {"criticalDamage", "치명타 피해" },
        {"shield", "보호막" },
        {"bonusExpRate", "추가 경험치 획득량" },
        {"bonusGoldRate", "추가 골드 획득량" },
        {"luck", "행운" },
        {"cooldownReduction", "스킬 가속" },
        {"reviveCount", "부활 횟수" }
    };

    public static List<(string, float)> GetPlayerStatInfo(Stats playerStats)
    {
        // 튜플을 이용해 스탯 이름, 기본 스탯을 한 번에 묶어서 관리
        List<(string name, float baseStat)> playerStatInfo = new List<(string, float)>();

        FieldInfo[] fields = typeof(Stats).GetFields(BindingFlags.Instance | BindingFlags.Public);

        foreach (FieldInfo field in fields)
        {
            float value = GetFieldValue(playerStats, field);

            if(statName.TryGetValue(field.Name, out string koreanName))
            {
                playerStatInfo.Add((koreanName, value));
            }
        }
        return playerStatInfo;
    }

    public static string GetTraitStatInfo(Stats traitStat, int point)
    {
        if (traitStat == null) return "특성 정보 없음";

        StringBuilder statStringBuilder = new StringBuilder();

        List<(string name, float baseStat)> traitStatInfo = new List<(string name, float baseStat)>();

        FieldInfo[] fields = typeof(Stats).GetFields(BindingFlags.Instance | BindingFlags.Public);

        foreach (FieldInfo field in fields)
        {
            float baseValue = GetFieldValue(traitStat, field);
            baseValue *= point;
            baseValue = (float)System.Math.Round(baseValue, 2);
            if (Mathf.Abs(baseValue) > 0.001f)
            {
                // 스탯 한글 명칭으로 추가
                if (statName.TryGetValue(field.Name, out string koreanName))
                {
                    traitStatInfo.Add((koreanName, baseValue));
                }
            }
        }

        foreach (var stat in traitStatInfo)
        {
            string basePart = $"+{stat.baseStat} {stat.name}";

            if (stat.name == "마나 재생 속도" || stat.name == "대시 재생 속도"|| 
                stat.name == "공격 속도" || stat.name == "치명타 확률" || 
                stat.name == "치명타 피해" || stat.name == "추가 경험치 획득량" ||
                stat.name == "추가 골드 획득량" || stat.name == "스킬 가속")
            {
                basePart = $"+{stat.baseStat * 100}% {stat.name}";
            }

            statStringBuilder.AppendLine($"{basePart}");
        }
        return statStringBuilder.ToString();
    }

    // 아이템의 상승 스탯 목록을 문자열로 반환
    public static string GetItemStatsDescription(ItemData itemData, int reinforce)
    {
        if (itemData == null) return "아이템 정보 없음";

        // string은 불변 객체로 문자열을 변경할 때마다 새로운 String을 생성 -> 성능 저하
        // ==> 메모리에서 이전 참조값을 버리고 새로운 참조값으로 변경
        // StringBuilder는 가변 객체로 버퍼라는 동적 배열을 가지고 있어, 이 버퍼에 문자열을 추가
        // ==> 참조값이 변경되지 않고 힙 메모리에서 값이 삽입, 추가, 제거
        StringBuilder statStringBuilder = new StringBuilder();

        // 튜플을 이용해 스탯 이름, 기본 스탯, 추가 스탯을 한 번에 묶어서 관리
        List<(string name, float baseStat, float bonusStat)> itemStatInfo
            = new List<(string, float, float)>();

        // Stats 필드 정보 가져오기
        // 인스턴스 멤버 포함 | public 멤버 포함
        FieldInfo[] fields = typeof(Stats).GetFields(BindingFlags.Instance | BindingFlags.Public);

        // 모든 스탯 필드를 순회하여 값 계산
        foreach (FieldInfo field in fields)
        {
            // 아이템 데이터에서 iBaseStat, iBonusStat객체에서 필드 값을 가져옴
            float baseValue = GetFieldValue(itemData.iBaseStat, field);
            float bonusValue = GetFieldValue(itemData.iBonusStat, field);

            bonusValue *= reinforce;

            // 유의미한 아이템 스탯만 필터링
            if (Mathf.Abs(baseValue) > 0.001f || Mathf.Abs(bonusValue) > 0.001f)
            {
                // 스탯 한글 명칭으로 추가
                if (statName.TryGetValue(field.Name, out string koreanName))
                {
                    itemStatInfo.Add((koreanName, baseValue, bonusValue));
                }
            }
        }



        // 문자열 생성
        foreach (var stat in itemStatInfo)
        {
            string basePart = $"{stat.name}: {stat.baseStat}";
            string bonusPart = "";

            if (stat.name == "마나 재생 속도" || stat.name == "대시 재생 속도" ||
                stat.name == "공격 속도" || stat.name == "치명타 확률" ||
                stat.name == "치명타 피해" || stat.name == "추가 경험치 획득량" ||
                stat.name == "추가 골드 획득량" || stat.name == "스킬 가속")
            {
                basePart = $"{stat.name}: {stat.baseStat * 100}%";
            }

            // 강화 수치가 0보다 크다면, 강화 보너스 정보 추가
            if (stat.bonusStat > 0)
            {
                bonusPart = $" + {stat.bonusStat}";
                if (stat.name == "마나 재생 속도" || stat.name == "대시 재생 속도" ||
                stat.name == "공격 속도" || stat.name == "치명타 확률" ||
                stat.name == "치명타 피해" || stat.name == "추가 경험치 획득량" ||
                stat.name == "추가 골드 획득량" || stat.name == "스킬 가속")
                {
                    bonusPart = $" + {stat.bonusStat * 100}%";
                }
            }
            statStringBuilder.AppendLine($"{basePart}{bonusPart}");
        }

        return statStringBuilder.ToString();

    }

    // 아이템 시너지 스탯 목록을 문자열로 변환
    public static string GetSynergyStatsDescription(SynergyData synergyData, int synergyLevel)
    {
        if (synergyData == null) return "시너지 정보 없음";

        StringBuilder statStringBuilder = new StringBuilder();

        // 튜플을 이용해 스탯 이름, 기본 스탯을 한 번에 묶어서 관리
        List<(string name, float baseStat)> synergyStatInfo = new List<(string, float)>();

        // Stats 필드 정보 가져오기
        // 인스턴스 멤버 포함 | public 멤버 포함
        FieldInfo[] fields = typeof(Stats).GetFields(BindingFlags.Instance | BindingFlags.Public);

        // 모든 스탯 필드를 순회하여 값 계산
        foreach (FieldInfo field in fields)
        {
            // 해당 레벨의 시너지 효과 스탯 정보 가져오기
            float bonusValue = GetFieldValue(synergyData.GetBonusStats(synergyLevel), field);

            // 유의미한 아이템 스탯만 필터링
            if (Mathf.Abs(bonusValue) > 0.001f)
            {
                // 스탯 한글 명칭으로 추가
                if (statName.TryGetValue(field.Name, out string koreanName))
                {
                    synergyStatInfo.Add((koreanName, bonusValue));
                }
            }
        }

        // 문자열 생성
        foreach (var stat in synergyStatInfo)
        {
            string basePart = $"{stat.name}: {stat.baseStat}  ";

            if (stat.name == "마나 재생 속도" || stat.name == "대시 재생 속도" ||
                stat.name == "공격 속도" || stat.name == "치명타 확률" ||
                stat.name == "치명타 피해" || stat.name == "추가 경험치 획득량" ||
                stat.name == "추가 골드 획득량" || stat.name == "스킬 가속")
            {
                basePart = $"{stat.name}: {stat.baseStat * 100}%  ";
            }

            statStringBuilder.Append($"{basePart}");
        }

        return statStringBuilder.ToString();
    }

    // Stats 구조체에서 필드 값을 안전하게 가져오는 보조 함수
    private static float GetFieldValue(Stats stats, FieldInfo field)
    {
        if (stats == null) return 0f;

        object value = field.GetValue(stats);

        if (value is float floatValue) return floatValue;
        if (value is int intValue) return (float)intValue;

        return 0f;
    }
}
