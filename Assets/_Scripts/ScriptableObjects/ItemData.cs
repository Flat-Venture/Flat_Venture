using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 1. 아이템 데이터를 저장하는 SO
/// 2. 아이템 이름, 설명, 효과, 능력치 등을 포함
/// </summary>
[CreateAssetMenu(fileName = "ItemData", menuName = "Scriptable Objects/ItemData")]
public class ItemData : ScriptableObject
{
    public int iId;                             // 아이템 고유 ID
    public Sprite iIcon;                        // 아이템 아이콘
    public string iName;                        // 아이템 이름
    public string iStatDescription;             // 아이템 스탯 설명
    [TextArea]
    public string iDescription;                 // 아이템 설명
    public Enums.ItemRank iRank;                // 아이템 등급
    public List<Enums.ItemSynergy> iSynergy;    // 아이템 시너지
    public Enums.ItemEffect iEffect;            // 아이템 추가 효과
    public float iPrice;                        // 아이템 가격(골드)

    // 아이템 능력치
    public Stats iBaseStat;                     // 아이템 능력치

    // 아이템 추가 능력치
    public Stats iBonusStat;                    // 아이템 강화 수치

    // 아이템 효과 스탯
    public float iExtraAttackDamage;            // 강화 보너스 공격력
    public float iExtraRange;                   // 공격 추적 범위
    public float iExtraAttackRange;             // 강화 보너스 공격 범위
    public float iExtraAttackSpeed;             // 강화 보너스 공격 속도
    public float iExtraProjectileSpeed;         // 강화 보너스 투사체 속도

    private void OnValidate()
    {
        UpdatePriceByRank();
    }

    // 랭크에 따라 가격이 바뀌는 함수
    private void UpdatePriceByRank()
    {
        // 등급별 가격 설정 (기획에 맞게 숫자를 수정하세요)
        switch (iRank)
        {
            case Enums.ItemRank.Common:
                iPrice = 200f;
                break;
            case Enums.ItemRank.Rare:
                iPrice = 400f;
                break;
            case Enums.ItemRank.Unique:
                iPrice = 800f;
                break;
            case Enums.ItemRank.Legendary:
                iPrice = 1600f;
                break;
            default:
                iPrice = 0f;
                break;
        }
    }

    // 이 함수 호출 시 리스트 자체에서 중복을 없앰
    public void SanitizeData()
    {
        if (iSynergy != null)
        {
            // Distinct를 사용하여 중복 제거 후 리스트 재할당
            iSynergy = iSynergy.Distinct().ToList();
            
            // 시너지 설정이 없으면 None으로 지정
            if(iSynergy.Count == 0)
            {
                iSynergy.Add(Enums.ItemSynergy.None);
            }

            // 중복된 시너지가 없는 상황에서 시너지가 2개 이상이고, None이 있다면 None 제거
            if(iSynergy.Count >= 2)
            {
                if (iSynergy.Contains(Enums.ItemSynergy.None))
                {
                    iSynergy.Remove(Enums.ItemSynergy.None);
                }
            }

            iSynergy.Sort((a, b) =>
            {
                int value1 = (int)a;
                int value2 = (int)b;

                return a.CompareTo(b);
            });
        }
    }
}

