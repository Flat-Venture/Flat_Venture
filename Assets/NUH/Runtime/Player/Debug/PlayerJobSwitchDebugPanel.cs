using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 테스트 씬에서 버튼으로 전사, 궁수, 마법사 직업을 즉시 전환하고 현재 전투 상태를 표시합니다.
/// </summary>
public sealed class PlayerJobSwitchDebugPanel : MonoBehaviour
{
#if UNITY_EDITOR
    private const string WarriorStatsPath = "Assets/NUH/Data/Player/PlayerStats_Warrior.asset";
    private const string ArcherStatsPath = "Assets/NUH/Data/Player/PlayerStats_Archer.asset";
    private const string MageStatsPath = "Assets/NUH/Data/Player/PlayerStats_Mage.asset";
    private const string ArcherBasicAttackPrefabPath = "Assets/NUH/Prefabs/Player/BasicAttacks/Pfb_ArcherArrow.prefab";
    private const string MageBasicAttackPrefabPath = "Assets/NUH/Prefabs/Player/BasicAttacks/Pfb_MageOrb.prefab";
#endif

    // 직업 변경에 필요한 플레이어 모듈과 직업별 데이터·프리팹 참조입니다.
    [SerializeField] private PlayerController player;
    [SerializeField] private PlayerBasicAttackController basicAttack;
    [SerializeField] private WarriorActiveSkillController warriorActiveSkill;
    [SerializeField] private ArcherActiveSkillController archerActiveSkill;
    [SerializeField] private MageActiveSkillController mageActiveSkill;
    [SerializeField] private PlayerStatsData warriorStats;
    [SerializeField] private PlayerStatsData archerStats;
    [SerializeField] private PlayerStatsData mageStats;
    [SerializeField] private PlayerBasicAttackProjectile archerBasicAttackProjectile;
    [SerializeField] private PlayerBasicAttackProjectile mageBasicAttackProjectile;
    [SerializeField] private Text output;

    private string currentJobId = "Warrior";

    /// <summary>테스트 씬 생성기가 필요한 참조를 한 번에 연결할 때 사용합니다.</summary>
    public void ConfigureForTestScene(
        PlayerController targetPlayer,
        PlayerBasicAttackController targetBasicAttack,
        WarriorActiveSkillController targetWarriorActiveSkill,
        ArcherActiveSkillController targetArcherActiveSkill,
        MageActiveSkillController targetMageActiveSkill,
        PlayerStatsData targetWarriorStats,
        PlayerStatsData targetArcherStats,
        PlayerStatsData targetMageStats,
        PlayerBasicAttackProjectile targetArcherBasicAttackProjectile,
        PlayerBasicAttackProjectile targetMageBasicAttackProjectile,
        Text targetOutput)
    {
        player = targetPlayer;
        basicAttack = targetBasicAttack;
        warriorActiveSkill = targetWarriorActiveSkill;
        archerActiveSkill = targetArcherActiveSkill;
        mageActiveSkill = targetMageActiveSkill;
        warriorStats = targetWarriorStats;
        archerStats = targetArcherStats;
        mageStats = targetMageStats;
        archerBasicAttackProjectile = targetArcherBasicAttackProjectile;
        mageBasicAttackProjectile = targetMageBasicAttackProjectile;
        output = targetOutput;
    }

    /// <summary>씬 시작 시 현재 PlayerController의 SO 기준으로 버튼 상태와 스킬 활성 상태를 맞춥니다.</summary>
    private void Start()
    {
        ResolveMissingEditorReferences();
        string jobId = player != null && player.BaseStats != null ? player.BaseStats.JobId : currentJobId;
        ApplyJob(jobId, false);
    }

    /// <summary>현재 직업, 기본 공격, 액티브 스킬, 풀링 상태를 매 프레임 표시합니다.</summary>
    private void Update()
    {
        if (output == null || player == null || player.RuntimeState == null)
            return;

        PlayerRuntimeState state = player.RuntimeState;
        IPlayerActiveSkillStatus activeSkill = GetCurrentActiveSkill();

        output.text =
            $"[직업 실시간 변경 테스트]\n" +
            $"현재 직업: {currentJobId} | SO: {state.Source.JobId}\n" +
            $"HP {state.CurrentHealth:0.#}/{state.MaxHealth:0.#} | 기본공격 {state.BasicAttackType}\n" +
            $"공격력 {state.AttackPower:0.#} | 공속 {state.AttacksPerSecond:0.##} | 범위 {state.BasicAttackRange:0.##} | 폭 {state.BasicAttackWidth:0.##}\n" +
            $"기본공격 쿨타임 {GetBasicAttackCooldown():0.##} | 기본공격 풀 {GetBasicPoolText()}\n" +
            $"액티브 피해 {state.ActiveSkillDamage:0.#} | 수 {state.ActiveSkillProjectileCount} | 속도 {state.ActiveSkillProjectileSpeed:0.#} | 폭 {state.ActiveSkillProjectileWidth:0.##}\n" +
            $"액티브 조준 {GetActiveAiming(activeSkill)} | 시전 {GetActiveCasting(activeSkill)} | 쿨타임 {GetActiveCooldown(activeSkill):0.##} | 제거 {GetActiveIgnoreCooldown(activeSkill)}\n" +
            $"액티브 풀 {GetActivePoolText(activeSkill)}\n" +
            $"전사/궁수/마법사 버튼으로 즉시 교체 후 좌클릭 기본공격, 우클릭 액티브를 확인하세요.";
    }

    /// <summary>전사 SO와 근접 기본 공격, 전사 액티브 스킬로 전환합니다.</summary>
    public void SwitchToWarrior()
    {
        ApplyJob("Warrior", true);
    }

    /// <summary>궁수 SO와 화살 기본 공격, 관통 사격 액티브 스킬로 전환합니다.</summary>
    public void SwitchToArcher()
    {
        ApplyJob("Archer", true);
    }

    /// <summary>마법사 SO와 마법탄 기본 공격, 부채꼴 파이어볼 액티브 스킬로 전환합니다.</summary>
    public void SwitchToMage()
    {
        ApplyJob("Mage", true);
    }

    /// <summary>현재 활성 직업의 액티브 스킬 쿨타임 무시 상태를 전환합니다.</summary>
    public void ToggleCurrentSkillNoCooldown()
    {
        IPlayerActiveSkillStatus activeSkill = GetCurrentActiveSkill();
        if (activeSkill != null)
            activeSkill.ToggleIgnoreCooldown();
    }

    /// <summary>현재 직업의 원본 SO 값으로 런타임 상태를 다시 초기화합니다.</summary>
    public void ResetCurrentJob()
    {
        ApplyJob(currentJobId, true);
    }

    /// <summary>직업 ID에 맞는 SO, 기본 공격 프리팹, 액티브 스킬 활성 상태를 한 번에 적용합니다.</summary>
    private void ApplyJob(string jobId, bool resetPosition)
    {
        ResolveMissingEditorReferences();
        PlayerStatsData stats = GetStats(jobId);
        if (stats == null || player == null)
            return;

        currentJobId = stats.JobId;
        player.ApplyStatsData(stats, resetPosition);
        player.name = "Player_" + currentJobId + "_JobSwitch_Test";
        ConfigureBasicAttack(currentJobId);
        ConfigureActiveSkill(currentJobId);
    }

    /// <summary>직업별 기본 공격 방식에 맞춰 기본 공격 투사체 풀을 교체합니다.</summary>
    private void ConfigureBasicAttack(string jobId)
    {
        if (basicAttack == null)
            return;

        if (jobId == "Archer")
            basicAttack.ConfigureProjectilePrefab(archerBasicAttackProjectile);
        else if (jobId == "Mage")
            basicAttack.ConfigureProjectilePrefab(mageBasicAttackProjectile);
        else
            basicAttack.ConfigureProjectilePrefab(null);
    }

    /// <summary>세 직업 액티브 스킬 중 현재 직업에 해당하는 컨트롤러만 입력을 받게 합니다.</summary>
    private void ConfigureActiveSkill(string jobId)
    {
        if (warriorActiveSkill != null)
            warriorActiveSkill.enabled = jobId == "Warrior";
        if (archerActiveSkill != null)
            archerActiveSkill.enabled = jobId == "Archer";
        if (mageActiveSkill != null)
            mageActiveSkill.enabled = jobId == "Mage";
    }

    /// <summary>문자열 직업 ID를 테스트 씬에 연결된 SO 참조로 변환합니다.</summary>
    private PlayerStatsData GetStats(string jobId)
    {
        if (jobId == "Archer")
            return archerStats;
        if (jobId == "Mage")
            return mageStats;
        return warriorStats;
    }

    /// <summary>
    /// 테스트 씬 직렬화 참조가 비어 있을 때 에디터에서만 에셋 경로로 다시 연결합니다.
    /// 실제 빌드에서는 UnityEditor API를 사용하지 않도록 컴파일에서 제외됩니다.
    /// </summary>
    private void ResolveMissingEditorReferences()
    {
#if UNITY_EDITOR
        if (warriorStats == null)
            warriorStats = AssetDatabase.LoadAssetAtPath<PlayerStatsData>(WarriorStatsPath);
        if (archerStats == null)
            archerStats = AssetDatabase.LoadAssetAtPath<PlayerStatsData>(ArcherStatsPath);
        if (mageStats == null)
            mageStats = AssetDatabase.LoadAssetAtPath<PlayerStatsData>(MageStatsPath);
        if (archerBasicAttackProjectile == null)
            archerBasicAttackProjectile = LoadPrefabComponent<PlayerBasicAttackProjectile>(ArcherBasicAttackPrefabPath);
        if (mageBasicAttackProjectile == null)
            mageBasicAttackProjectile = LoadPrefabComponent<PlayerBasicAttackProjectile>(MageBasicAttackPrefabPath);
#endif
    }

#if UNITY_EDITOR
    /// <summary>프리팹 루트 GameObject에서 지정 컴포넌트를 찾아 반환합니다.</summary>
    private static T LoadPrefabComponent<T>(string path) where T : Component
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        return prefab != null ? prefab.GetComponent<T>() : null;
    }
#endif

    private IPlayerActiveSkillStatus GetCurrentActiveSkill()
    {
        if (currentJobId == "Archer")
            return archerActiveSkill;
        if (currentJobId == "Mage")
            return mageActiveSkill;
        return warriorActiveSkill;
    }

    private float GetBasicAttackCooldown()
    {
        return basicAttack != null ? basicAttack.CooldownRemaining : 0f;
    }

    private string GetBasicPoolText()
    {
        if (basicAttack == null)
            return "None";

        return $"{basicAttack.PoolActiveCount}/{basicAttack.PoolInactiveCount}/{basicAttack.PoolTotalCount}";
    }

    private bool GetActiveAiming(IPlayerActiveSkillStatus activeSkill)
    {
        return activeSkill != null && activeSkill.IsAiming;
    }

    private bool GetActiveCasting(IPlayerActiveSkillStatus activeSkill)
    {
        return activeSkill != null && activeSkill.IsCasting;
    }

    private float GetActiveCooldown(IPlayerActiveSkillStatus activeSkill)
    {
        return activeSkill != null ? activeSkill.CooldownRemaining : 0f;
    }

    private bool GetActiveIgnoreCooldown(IPlayerActiveSkillStatus activeSkill)
    {
        return activeSkill != null && activeSkill.IgnoreCooldown;
    }

    private string GetActivePoolText(IPlayerActiveSkillStatus activeSkill)
    {
        if (activeSkill == null)
            return "None";

        return $"{activeSkill.PoolActiveCount}/{activeSkill.PoolInactiveCount}/{activeSkill.PoolTotalCount}";
    }
}
