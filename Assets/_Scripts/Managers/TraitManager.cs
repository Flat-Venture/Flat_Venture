using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TraitManager : MonoBehaviour
{
    public static TraitManager Instance;

    [Header("모든 특성 데이터")]
    public AllTraitsData allTraits;

    // SaveLoadManager의 userData를 참조
    public UserData userData => SaveLoadManager.Instance.userData;

    Dictionary<Enums.TraitType, TraitData> traitsDictionary
        = new Dictionary<Enums.TraitType, TraitData>();

    const float SAVE_DELAY = 0.5f;
    Coroutine saveCoroutine;

    UIManager uiManager;
    SoundManager soundManager;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        traitsDictionary = allTraits.allTraits.ToDictionary(x => x.traitType, x => x);
    }

    private void Start()
    {
        uiManager = UIManager.Instance;
        soundManager = SoundManager.Instance;
    }

    // 사용 가능한 남은 포인트 (유저 레벨 1당 특성 포인트 1)
    public int GetAvailablePoints()
    {
        return userData.GetRemainPoints();
    }

    // 특성 포인트 투자
    public void InvestTraitPoint(Enums.TraitType type, int point = 1)
    {
        if (GetAvailablePoints() <= 0)
        {
            return;
        }

        soundManager.PlaySFX("buttonClick");

        // traitPoints[type]의 값을 가져오거나 없으면 설정한 디폴트 값(0)을 가져옴.
        int currentPoints = userData.traitPoints.GetValueOrDefault(type, 0);
        if (currentPoints < 20)
        {
            if (userData.GetRemainPoints() < point)
            {
                userData.traitPoints[type] += userData.GetRemainPoints();
            }
            else
            {
                userData.traitPoints[type] = currentPoints + point;
            }

            if (userData.traitPoints[type] > 20)
            {
                userData.traitPoints[type] = 20;
            }

            // UI 업데이트
            uiManager.traitUIController.RefreshTraitUI();

            // 특성정보 저장
            SaveTraitInfo();
            if (Player.Instance != null)
            {
                Player.Instance.SetFinalStat();
                Player.Instance.Heal(Player.Instance.finalStats.maxHp);
            }
        }
    }

    // 특성 포인트 반환
    public void RefundTraitPoint(Enums.TraitType type, int point = 1)
    {
        soundManager.PlaySFX("buttonClick");

        if (userData.traitPoints.TryGetValue(type, out var value) && value > 0)
        {
            userData.traitPoints[type] = value - point;
            if (userData.traitPoints[type] <= 0)
            {
                userData.traitPoints[type] = 0;
                userData.traitPoints.Remove(type);
            }

            // UI 업데이트
            uiManager.traitUIController.RefreshTraitUI();

            // 특성정보 저장
            SaveTraitInfo();
            if (Player.Instance != null)
            {
                Player.Instance.SetFinalStat();
                Player.Instance.Heal(0);
            }
        }
    }

    public void SaveTraitInfo()
    {
        if (saveCoroutine != null)
        {
            StopCoroutine(saveCoroutine);
        }
        saveCoroutine = StartCoroutine(SaveTraitInfoDelay());
    }

    IEnumerator SaveTraitInfoDelay()
    {
        yield return new WaitForSeconds(SAVE_DELAY);
        SaveLoadManager.Instance.SaveUserData();
        saveCoroutine = null;
    }

    public Stats GetTraitsStats()
    {
        Stats totalTraitStats = new Stats();

        // 유저가 찍은 특성 포인트
        foreach (var keys in userData.traitPoints.Keys)
        {
            totalTraitStats += traitsDictionary[keys].statsPerPoint * userData.traitPoints[keys];

            foreach (BonusTraitEffect bonus in traitsDictionary[keys].bonusTrait)
            {
                if (userData.traitPoints[keys] >= bonus.requiredPoints)
                {
                    totalTraitStats += bonus.bonusStats;
                }
                else
                {
                    break;
                }
            }
        }
        return totalTraitStats;
    }
}