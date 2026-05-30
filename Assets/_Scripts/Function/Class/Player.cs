using UnityEngine;
using Unity.Cinemachine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 1. 플레이어 스탯 (체력, 마나, 공격력, 방어력 등)
/// 2. 플레이어 행동 (이동, 공격, 대쉬 등)
/// 3. 플레이어 애니메이션 제어
/// </summary>
public class Player : MonoBehaviour
{    // 아이템 + 클래스의 최종 스탯들을 합쳐야함.
    // 스탯들 다 정리해주고, 여기있는 이동속도를 PlayerMove에 넣어주기 / 공격력, 공격속도 등을 PlayerAttack에 넣어주기

    // Player는 Prefab으로 DungeonManager에서 만들어서 진행
    public static Player Instance;

    public PlayerLevelSystem levelSystem;
    public PlayerGoldSystem goldSystem;
    public PlayerSkillController skillController;

    //스킬에 player로 들고올수 있게 캐싱
    PlayerMove move;
    PlayerAttack pa;

    //직업 데이터 넣는곳, 처음은 Warrior고정, 직업 변경시 바꿔줘야함.
    public ClassData classStat;

    public float currentHp;
    public float currentMp;

    //버프 전용 스탯
    private Stats addBuffStats = new Stats();                           //합연산 버프
    private Stats multiBuffStats = Stats.CreateMultiplierDefault();     //곱연산 버프

    public Stats finalStats;                                            //최종 스탯

    bool isStunned = false;
    public bool IsStunned => isStunned;
    bool isInputReversed = false;
    public bool IsInputReversed => isInputReversed;


    //아처 오브젝트 풀 큐 만들기
    //ScriptableObject(Job_Archer)는 “씬에 있는 풀”을 직접 관리하면 안 됨
    PoolManager poolManager;                    //캐싱용
    public ArcherProjectile arrowPrefab;        //화살 프리팹
    public int arrowPoolSize = 5;               //풀 화살 갯수 저장

    // 플레이어 Sprite 항상 카메라 바라보기
    public CinemachineCamera pCam;
    Transform pSprite;

    // 플레이어 애니메이션
    public PlayerAnimController animCtrl;


    //Hp,Mp,Exp 바 연동용 이벤트 추가
    public System.Action<float, float> OnHpChanged;     //current, max
    public System.Action<float, float> OnMpChanged;

    //죽을때
    public float deathRotateAngle = -90f;
    public float deathShakeAngle = 10f;
    public float deathAnimDuration = 2f;

    public bool isDead = false;

    /*피격, 대쉬시 무적 판정용 / 
     * AddInvincible, RemoveInvincible 사용
     * Enums - InvincibleReason에 무적하고 싶은 이유를 넣기
     * 적용하고 싶은 곳에 AddInvincible(무적 이유) 넣어주면 
     * Invincible.Count가 0 초과가 되므로
     * TakeDamage에서 걸러줍니다.
     * 무적 해제를 해야될 타이밍에
     * RemoveInvincible(아까무적적용한이유); 넣어주면
     * 자동으로 무적 풀림
    */
    HashSet<Enums.InvincibleReason> invincibleReasons = new HashSet<Enums.InvincibleReason>();
    public bool isInvincible => invincibleReasons.Count > 0;
    public float hitInvincibleDuration = 0.5f;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);

        poolManager = PoolManager.Instance;

        pSprite = transform.Find("Sprite");

        multiBuffStats = Stats.CreateMultiplierDefault();

        move = GetComponent<PlayerMove>();
        animCtrl = GetComponentInChildren<PlayerAnimController>();
        pa = GetComponent<PlayerAttack>();
        levelSystem = GetComponent<PlayerLevelSystem>();
        goldSystem = GetComponent<PlayerGoldSystem>();
        skillController = GetComponent<PlayerSkillController>();
    }
    private void Start()
    {
        finalStats = GetBaseStat();
        currentHp = finalStats.maxHp;
        currentMp = finalStats.maxMp;
        OnHpChanged?.Invoke(currentHp, finalStats.maxHp);
        OnMpChanged?.Invoke(currentMp, finalStats.maxMp);

        if (arrowPrefab == null)
        {
            Debug.LogError("ArrowPrefab이 비어있음!");
            return; // 또는 예외 처리하고 진행 중단
        }
        //아처일때만 화살 오브젝트 풀링 실행
        if(classStat.classType == Enums.ClassType.Archer)
        poolManager.CreatePool<ArcherProjectile>(Enums.PoolType.ArrowPool, arrowPrefab, arrowPoolSize, transform);
    }

    private void Update()
    {
        if (isDead) return;
        LookAtPCam();
    }

    public Stats GetBaseStat()
    {
        return TraitManager.Instance.GetTraitsStats() + classStat.cBaseStat;
    }

    // <summary>
    /// 최종 데미지 스탯 갱신해주기
    /// </summary>
    /// <returns></returns>
    public void SetFinalStat()
    {
        Stats baseStats = GetBaseStat() + InventoryManager.Instance.GetInventoryTotalStats();
        Stats added = baseStats + addBuffStats;
        finalStats = added * multiBuffStats;
    }

    /// <summary>
    /// 곱연산 버프 스킬 실행 함수
    /// </summary>
    /// <param name="mulStats"></param>
    /// <param name="duration"></param>
    /// <returns></returns>
    public Coroutine ApplyMultiplicativeBuff(Stats mulStats, float duration)
    {
        return StartCoroutine(CoApplyMultiplicativeBuff(mulStats, duration));
    }

    private IEnumerator CoApplyMultiplicativeBuff(Stats mulStats, float duration)
    {
        // 1. 곱연산 버프 적용
        multiBuffStats *= mulStats;
        SetFinalStat();

        // 2. 시간 대기 (레벨업 UI / 일시정지 대응)
        yield return new WaitForSecondsRealtime(duration);

        // 3. 곱연산 버프 해제
        multiBuffStats /= mulStats;
        SetFinalStat();
    }

    /// <summary>
    /// 플레이어가 데미지 입었을 때 호출될 함수
    /// </summary>
    /// <param name="value"></param>
    public void TakeDamage(float value)
    {
        //무적 상태면 return
        if (isInvincible) return;


        // 방어력(defense)이 있다면 데미지 감소 로직 추가 가능
        // float finalDamage = Mathf.Max(0, value - GetFinalStat().defense); 
        float finalDamage = value;

        // 몬스터 쪽에서 들고가야함 -> 플레이어가 데미지를 받는 로직은 플레이어 쪽에 있어야 합니다.
        currentHp -= finalDamage;

        // 사운드
        //SoundManager.Instance.PlaySFX("hurt");

        Debug.Log("Player took " + finalDamage + " damage. Current HP: " + currentHp);

        OnHpChanged?.Invoke(currentHp, finalStats.maxHp);

        //피격시 무적 적용
        StartCoroutine(CoHitInvincible());
    }

    IEnumerator CoHitInvincible()
    {
        //무적 이유 추가해주고 무적 시간동안 피격x, 끝나면 이유 없애기
        AddInvincible(Enums.InvincibleReason.Hit);
        yield return new WaitForSeconds(hitInvincibleDuration);
        RemoveInvincible(Enums.InvincibleReason.Hit);
    }

    /// <summary>
    /// 체력 회복 함수 (스킬이나 상호작용 등에 쓰일 예정)
    /// </summary>
    /// <param name="amount"></param>
    public void Heal(float amount)
    {
        //Heal 수치만큼 현재 체력 회복, 최대체력 안넘음
        currentHp = Mathf.Min(currentHp + amount, finalStats.maxHp);
        OnHpChanged?.Invoke(currentHp, finalStats.maxHp);

        Debug.Log("힐");
    }

    /// <summary>
    /// 플레이어 죽을 시 실행될 함수
    /// </summary>
    public void Die()
    {
        // 플레이어 사망 처리 (게임 오버, 재시작 등)
        Debug.Log("Player has died.");


        if(isDead) return;
        isDead = true;

        //이동, 공격 막기
        move.canMove = false;
        pa.canAtt = false;

        //애니메이션 상태 고정
        animCtrl.ChangeState(Enums.PlayerAnimState.Idle);

        // Time.timeScale = 0;
        StartCoroutine(CoDie());
    }

    IEnumerator CoDie()
    {
        Transform sprite = pSprite;
        Vector3 originalScale = sprite.localScale;

        float elapsed = 0f;

        // 앞 뒤 흔들고
        while (elapsed < deathAnimDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = elapsed / deathAnimDuration;
            float shake = Mathf.Sin(t * Mathf.PI * 4f) * deathShakeAngle;

            sprite.localRotation = Quaternion.Euler(shake, 0f, 0f);
            yield return null;
        }

        float fallTime = 0.4f;
        elapsed = 0f;

        // 쓰러지기
        while (elapsed < fallTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / fallTime;

            float z = Mathf.Lerp(0f, -deathRotateAngle, t);
            sprite.localRotation = Quaternion.Euler(z, 0f, 0f);

            yield return null;
        }
    }

    /// <summary>
    /// 화살 하나 꺼내오기
    /// </summary>
    /// <returns></returns>
    public ArcherProjectile GetArrow()
    {
        return poolManager.Get<ArcherProjectile>(Enums.PoolType.ArrowPool);
    }

    /// <summary>
    /// 화살 집어넣기 (Projectile에서 이 함수 호출)
    /// </summary>
    /// <param name="arrow"></param>
    public void ReturnArrow(ArcherProjectile arrow)
    {
        poolManager.Return<ArcherProjectile>(Enums.PoolType.ArrowPool, arrow);
    }

    /// <summary>
    /// 캐릭터 Sprite가 항상 카메라를 바라보게 하기
    /// 맵 상에 있는 Sprite들은 어지간하면 다 이걸 적용시켜줘야 함.
    /// </summary>
    void LookAtPCam()
    {
        // 카메라가 바라보는 방향의 반대 = 캐릭터가 향해야 할 방향
        Vector3 lookDir = pCam.transform.forward;

        // 카메라를 정면으로 마주보게 회전
        pSprite.rotation = Quaternion.LookRotation(lookDir);

    }

    /// <summary>
    /// 몬스터를 바라보기 + 좌 우 바라보기
    /// </summary>
    /// <param name="dirX"></param>
    public void SetFacing(float dirX)
    {
        if (dirX == 0) return;

        //Scale 조절로 좌우 반전하기
        Vector3 scale = pSprite.localScale;
        scale.x = dirX > 0 ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
        pSprite.localScale = scale;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="duration"></param>
    public void Stun(float duration)
    {
        if (!gameObject.activeInHierarchy) return;
        StopCoroutine(nameof(CoStun));
        StartCoroutine(CoStun(duration));
    }

    IEnumerator CoStun(float d)
    {
        //Debug.Log("스턴");
        isStunned = true;
        pa.canAtt = false;
        move.canMove = false;

        yield return new WaitForSeconds(d);

        isStunned = false;
        pa.canAtt = true;
        move.canMove = true;
    }
    public void ReverseInput(float duration)
    {
        if (!gameObject.activeInHierarchy) return;
        StopCoroutine(nameof(CoReverse));
        StartCoroutine(CoReverse(duration));
    }
    IEnumerator CoReverse(float d)
    {
        //Debug.Log("반전");
        isInputReversed = true;

        yield return new WaitForSeconds(d);

        isInputReversed = false;
    }

    public void ResetPlayer()
    {
        //플레이어 레벨, 경험치 초기화
        levelSystem.ResetLevelAndExp();
        //보유스킬, 스킬레벨, 스킬슬롯 초기화
        skillController.ResetAllSkills();

        //이동/ 공격 허용
        move.canMove = true;
        pa.canAtt = true;
        isDead = false;

        //회전 / 스케일 초기화
        pSprite.localRotation = Quaternion.identity;
        pSprite.localScale = Vector3.one * 4f;

        //골드 초기화
        goldSystem.ResetGold();

        //체력, 마나 초기화
        currentHp = finalStats.maxHp;
        currentMp = finalStats.maxMp;

        // 플레이어 스탯창 업데이트
        UIManager.Instance.playerStatUIController.UpdatePlayerStatUI();

        // 게임 시간 다시 흐르기
        Time.timeScale = 1f;
    }

    public void AddInvincible(Enums.InvincibleReason reason)
    {
        invincibleReasons.Add(reason);
    }

    public void RemoveInvincible(Enums.InvincibleReason reason)
    {
        invincibleReasons.Remove(reason);
    }
}
