using System.Collections;
using UnityEngine;

/// <summary>
/// 현재 레벨 관리
/// 레벨업 필요 경험치 계산
/// 레벨업 이벤트 실행
/// </summary>
public class PlayerLevelSystem : MonoBehaviour
{
    public int currentLevel = 1;
    public float currentExp;


    public System.Action<int> OnLevelUp;            //int = 몇 레벨 올랐는지
    int remainingLevelUps;                          //남은 레벨업 갯수

    //이번 프레임에서 몇 번 레벨업 했는지 (한번에 많은 경험치로 2이상 레벨업 했을 경우를 대비)
    int pendingLevelUps = 0;

    public LevelUpSkillSelector selector;

    public System.Action<float, float> OnExpChanged;



    private void Start()
    {
        selector = new LevelUpSkillSelector(Player.Instance.classStat, PlayerSkillController.Instance);

        OnLevelUp -= HandleLevelUp;         //씬 전환 시 여러번 불릴 수 있으니 보험
        OnLevelUp += HandleLevelUp;
    }

    /// <summary>
    /// 레벨업 할 때 스킬 선택창 보이기(시간 멈춤)
    /// </summary>
    private void HandleLevelUp(int count)
    {
        remainingLevelUps = count;

        StartCoroutine(CoLevelUp());
    }
    IEnumerator CoLevelUp()
    {
        //사운드
        SoundManager.Instance.PlaySFX("levelUp");

        //이펙트
        EffectManager.Instance.PlayEffect(
            Enums.EffectType.LevelUp,
            Player.Instance.transform.position + Vector3.down * 1f,
            Quaternion.identity,
            Player.Instance.transform
        );

        //연출 나오게 기다려주는 시간
        yield return new WaitForSecondsRealtime(0.1f);

        //화면 정지
        Time.timeScale = 0f;
        GameStateManager.Instance.SetState(Enums.GamePlayState.LevelUpUI);

        yield return new WaitForSecondsRealtime(1f);

        //스킬 선택 UI 표시
        ShowNextLevelUpUI();
    }

    private void ShowNextLevelUpUI()
    {
        if (remainingLevelUps <= 0)
        {
            Time.timeScale = 1f;
            GameStateManager.Instance.SetState(Enums.GamePlayState.Playing);        //게임 상태 돌려놓기
            return;
        }

        remainingLevelUps--;

        var ui = skillUI;
        var cards = selector.Create3Cards();
        ui.ShowCards(cards, OnCardSelected);
    }

    private void OnCardSelected()
    {
        ShowNextLevelUpUI();
    }

    /// <summary>
    /// 최대 경험치량 관리
    /// </summary>
    /// <param name="level"></param>
    /// <returns></returns>
    public float GetRequiredExp(int level)
    {
        return 10f + level * 5f;
    }

    /// <summary>
    /// 경험치 획득 함수 / PlayerExperience에서 호출할 예정
    /// </summary>
    /// <param name="amount"></param>
    public void AddExp(float amount)
    {
        if (Player.Instance.isDead) return;
        currentExp += amount;
        OnExpChanged?.Invoke(currentExp, GetRequiredExp(currentLevel));

        CheckLevelUp();
    }

    /// <summary>
    /// 레벨업 체크 함수
    /// </summary>
    private void CheckLevelUp()
    {
        pendingLevelUps = 0;

        while (currentExp >= GetRequiredExp(currentLevel))
        {
            currentExp -= GetRequiredExp(currentLevel);
            currentLevel++;
            pendingLevelUps++;
        }

        if (pendingLevelUps > 0)
        {
            OnLevelUp?.Invoke(pendingLevelUps);
        }
    }

    public void ResetLevelAndExp()
    {
        currentLevel = 1;
        currentExp = 0f;
        pendingLevelUps = 0;
        Player.Instance.SetFinalStat();

        OnExpChanged?.Invoke(currentExp, GetRequiredExp(currentLevel));
    }

    private SkillSelectionUI skillUI
    {
        get
        {
            return UIManager.Instance?.skillSelectionUI;
        }
    }
}
