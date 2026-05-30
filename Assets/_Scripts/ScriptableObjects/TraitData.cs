using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BonusTraitEffect
{
    public int requiredPoints;          // 5, 10, 20
    public Stats bonusStats;            // 추가 스탯
    [TextArea]
    public string bonusDescription;     // 효과 설명
}


[CreateAssetMenu(fileName = "TraitData", menuName = "Scriptable Objects/TraitData")]
public class TraitData : ScriptableObject
{
    public Enums.TraitType traitType;           // 특성 enum
    public string traitName;                    // 특성 이름
    public Color traitColor;                    // 특성 대표 색
    public Sprite icon;                         // 아이콘은 아직 없어도 됨

    public Stats statsPerPoint;                 // 1포인트당 상승 스탯
    public List<BonusTraitEffect> bonusTrait;   // 5, 10, 20 포인트 보너스


    // requiredPoints 기준으로 bonusTrait 정렬
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (bonusTrait != null)
        {
            bonusTrait.Sort((a, b) => a.requiredPoints.CompareTo(b.requiredPoints));
        }
    }
#endif
}
