using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class TraitSlotUI : MonoBehaviour
{
    TraitManager traitManager;

    TraitData traitData;
    Enums.TraitType traitType;
    
    [Header("특성 관련 UI")]
    public Image backgroundImage;                       // 배경 음악
    public TextMeshProUGUI traitName;                   // 특성 이름
    public TextMeshProUGUI traitCurrentPoints;          // 현재 투자한 특성 포인트
    public TextMeshProUGUI traitStats;                  // 특성 스탯

    public TextMeshProUGUI point5Stats;                 // 5포인트 특성 스탯
    public TextMeshProUGUI point10Stats;                // 10포인트 특성 스탯
    public TextMeshProUGUI point20Stats;                // 20포인트 특성 스탯

    public TextMeshProUGUI point5StatsValue;            // 5포인트 특성 효과
    public TextMeshProUGUI point10StatsValue;           // 10포인트 특성 효과
    public TextMeshProUGUI point20StatsValue;           // 20포인트 특성 효과

    public Button plusPointButton;                      // +1 포인트 버튼
    public Button minusPointButton;                     // -1 포인트 버튼
    public Button plus5PointButton;                     // +5 포인트 버튼
    public Button minus5pointButton;                    // -5 포인트 버튼

    private void Awake()
    {
        traitManager = TraitManager.Instance;
    }

    private void Start()
    {
        plusPointButton.onClick.AddListener(() => traitManager.InvestTraitPoint(traitType));
        minusPointButton.onClick.AddListener(() => traitManager.RefundTraitPoint(traitType));

        plus5PointButton.onClick.AddListener(() => traitManager.InvestTraitPoint(traitType, 5));
        minus5pointButton.onClick.AddListener(() => traitManager.RefundTraitPoint(traitType, 5));
    }

    public void InitializeTraitSlotUI(TraitData data)
    {
        traitData = data;
        traitType = data.traitType;

        backgroundImage.color = data.traitColor;
        traitName.text = data.traitName;

        int currentPoints = traitManager.userData.traitPoints.GetValueOrDefault(data.traitType, 0);
        traitCurrentPoints.text = currentPoints.ToString();
        traitCurrentPoints.color = data.traitColor;

        traitStats.text = ItemDataHelper.GetTraitStatInfo(data.statsPerPoint, currentPoints);

        point5Stats.color = data.traitColor;
        point10Stats.color = data.traitColor;
        point20Stats.color = data.traitColor;

        point5StatsValue.text = data.bonusTrait[0].bonusDescription;
        point10StatsValue.text = data.bonusTrait[1].bonusDescription;
        point20StatsValue.text = data.bonusTrait[2].bonusDescription;

        ActiveBonusTraitEffect(currentPoints);

        plusPointButton.image.color = data.traitColor;
        minusPointButton.image.color = data.traitColor;
        plus5PointButton.image.color = data.traitColor;
        minus5pointButton.image.color = data.traitColor;
    }

    public void RefreshTraitSlot()
    {
        int currentPoints = traitManager.userData.traitPoints.GetValueOrDefault(traitData.traitType, 0);
        traitCurrentPoints.text = currentPoints.ToString();

        traitStats.text = ItemDataHelper.GetTraitStatInfo(traitData.statsPerPoint, currentPoints);

        ActiveBonusTraitEffect(currentPoints);
    }

    public void ActiveBonusTraitEffect(int currentPoints)
    {
        if (currentPoints < 5)
        {
            point5StatsValue.color = Color.gray;
            point10StatsValue.color = Color.gray;
            point20StatsValue.color = Color.gray;
        }
        else
        {
            if (currentPoints < 10)
            {
                point5StatsValue.color = Color.green;
                point10StatsValue.color = Color.gray;
                point20StatsValue.color = Color.gray;
            }
            else
            {
                if (currentPoints < 20)
                {
                    point5StatsValue.color = Color.green;
                    point10StatsValue.color = Color.green;
                    point20StatsValue.color = Color.gray;
                }
                else
                {
                    point5StatsValue.color = Color.green;
                    point10StatsValue.color = Color.green;
                    point20StatsValue.color = Color.green;
                }
            }
        }
    }
}
