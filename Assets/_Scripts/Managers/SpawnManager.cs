using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // LINQ 사용을 위해 추가
using TMPro; // TextMeshPro 사용을 위해 추가
using static Enums; // RoomType Enum 접근을 위해 필요
using Random = UnityEngine.Random; // UnityEngine.Random 명시
using UnityEngine.UI;
using Unity.VisualScripting; // 버튼 UI 제어를 위해 추가

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance;

    public event System.Action OnAllEnemiesCleared;

    [System.Serializable]
    public class PhaseData
    {
        [Tooltip("이 페이즈에서 소환할 적 그룹들")]
        public List<SpawnInfo> spawns = new List<SpawnInfo>();
    }

    [System.Serializable]
    public class SpawnInfo
    {
        [Tooltip("소환될 수 있는 에너미 프리팹 목록 (이 중 랜덤으로 선택됨)")]
        public List<GameObject> enemyPrefabs = new List<GameObject>();

        public Transform spawnPoint;

        [Header("랜덤 소환 개수 설정")]
        [Tooltip("최소 소환 개수")]
        public int minCount = 1;
        [Tooltip("최대 소환 개수")]
        public int maxCount = 3;

        public float spawnOffset = 1.0f;
    }

    [Header("UI 설정")]
    [Tooltip("카운트다운 및 페이즈 알림을 표시할 TextMeshProUGUI")]
    public TextMeshProUGUI infoText;

    [Header("Game Over UI")]
    [Tooltip("플레이어 사망 시 활성화될 부모 오브젝트 (마을로 버튼 포함)")]
    public GameObject gameOverGroup;
    [Tooltip("마을로 이동하는 기능을 가진 버튼")]
    public Button backToTownButton;


    [Header("전역 설정")]
    [Tooltip("모든 몬스터 생성 시 공통으로 사용할 텔레포트/소환 이펙트 프리팹")]
    public GameObject globalSpawnEffectPrefab;

    [Tooltip("다음 페이즈로 넘어가기까지의 인터벌 시간")]
    public float IntervalTime = 3f;
    [Tooltip("게임 시작 전 대기 시간 (초)")]
    public float startWaitTime = 5f;

    // -----------------------------------------------------
    [Header("룸 타입별 페이즈 데이터")]
    [Tooltip("Normal 방에서 사용할 페이즈 설정")]
    public List<PhaseData> normalPhases = new List<PhaseData>();

    [Tooltip("Elite 방에서 사용할 페이즈 설정")]
    public List<PhaseData> elitePhases = new List<PhaseData>();

    [Tooltip("Boss 방에서 사용할 페이즈 설정")]
    public List<PhaseData> bossPhases = new List<PhaseData>();
    // -----------------------------------------------------

    // -----------------------------------------------------
    [Header("방 클리어 보상 및 포탈 설정")]

    [Tooltip("모든 리워드 슬롯에 사용될 프리팹")]
    public GameObject rewardPrefab; // Reward 프리팹 하나만 사용

    [Tooltip("리워드가 스폰될 첫 번째 위치")]
    public Transform reward1SpawnPoint;
    [Tooltip("리워드가 스폰될 두 번째 위치")]
    public Transform reward2SpawnPoint;
    [Tooltip("리워드가 스폰될 세 번째 위치")]
    public Transform reward3SpawnPoint;

    [Tooltip("씬에 미리 배치되어 비활성화된 포탈 오브젝트를 연결하세요.")]
    public GameObject portalObject;
    // -----------------------------------------------------

    List<GameObject> aliveEnemies = new List<GameObject>();
    bool spawningFinished = false;
    bool isPhaseActive = false;
    bool isPlayerDead = false; // 사망 중복 처리 방지

    public List<RewardItemUIController> rewards = new List<RewardItemUIController>();

    private void Awake()
    {
        // 씬 내에 오직 하나만 존재하도록 보장
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 게임 시작 시 포탈을 자동으로 비활성화합니다.
        if (portalObject != null)
        {
            portalObject.SetActive(false);
            Debug.Log("[SpawnManager] 포탈을 초기 비활성화 상태로 설정했습니다.");
        }

        // 초기 텍스트 설정
        if (infoText != null)
        {
            infoText.alpha = 0f;
        }

        // 게임 오버 UI 초기 비활성화
        if (gameOverGroup != null)
        {
            gameOverGroup.SetActive(false);
        }

        // 마을 이동 버튼 리스너 등록
        if (backToTownButton != null)
        {
            backToTownButton.onClick.AddListener(GoToTown);
        }
    }

    void Start()

    {
        // 플레이어 HP 변화 이벤트 구독하여 사망 감지
        if (Player.Instance != null)
        {
            Player.Instance.OnHpChanged += OnCheckPlayerDeath;
        }

        // DungeonManager의 인스턴스를 찾아옵니다.
        DungeonManager dungeonManager = DungeonManager.Instance;

        if (dungeonManager == null)
        {
            Debug.LogError("DungeonManager.Instance를 찾을 수 없습니다. 스폰을 시작할 수 없습니다.");
            return;
        }

        // DungeonManager에서 현재 설정된 룸 타입을 가져옵니다.
        Enums.RoomType currentRoomType = dungeonManager.GetCurrentRoomType();

        // 가져온 룸 타입을 사용하여 스폰 로직 시작
        Debug.Log($"[SpawnManager] Start()에서 DungeonManager를 통해 룸 타입 ({currentRoomType}) 확인 후 스폰 시작.");

        // 코루틴 시작
        StartCoroutine(StartSpawningFlow(currentRoomType));
    }

    /// <summary>
    /// 플레이어 HP 변화를 감시하여 0 이하일 때 사망 처리를 수행합니다.
    /// </summary>
    private void OnCheckPlayerDeath(float currentHp, float maxHp)
    {
        if (currentHp <= 0 && !isPlayerDead)
        {
            isPlayerDead = true;
            HandlePlayerDeath();
        }
    }

    private void HandlePlayerDeath()
    {
        StartCoroutine(CoPlayerDie());
    }

    IEnumerator CoPlayerDie()
    {
        foreach(var m in aliveEnemies)
        {
            Destroy(m);
        }
        
        Player.Instance.Die();
        yield return new WaitForSecondsRealtime(2f);
        Time.timeScale = 0f;

        // "사망했습니다." 애니메이션 출력
        StartCoroutine(PlayTextAnim("사망했습니다."));

        // 마을로 버튼이 포함된 UI 그룹 활성화
        if (gameOverGroup != null)
        {
            gameOverGroup.SetActive(true);
        }

    }

    /// <summary>
    /// 마을로 버튼 클릭 시 호출되는 함수입니다.
    /// </summary>
    public void GoToTown()
    {
        DungeonManager.Instance.ResetDungeonData();

        // SceneLoaderManager를 통해 마을 씬으로 이동
        SceneLoaderManager.Instance.LoadScene(SceneNames.Town);
    }

    IEnumerator StartSpawningFlow(RoomType roomType)
    {
        // 초기 대기 및 카운트다운 로직 (5초 대기)
        float timer = 0f;
        bool hasCounted3 = false;
        bool hasCounted2 = false;
        bool hasCounted1 = false;

        while (timer < startWaitTime)
        {
            timer += Time.deltaTime;
            float remaining = startWaitTime - timer;

            // 3초 남았을 때부터 카운트다운 시작
            if (remaining <= 3f && !hasCounted3)
            {
                StartCoroutine(PlayTextAnim("시작까지 3"));
                hasCounted3 = true;
            }
            else if (remaining <= 2f && !hasCounted2)
            {
                StartCoroutine(PlayTextAnim("시작까지 2"));
                hasCounted2 = true;
            }
            else if (remaining <= 1f && !hasCounted1)
            {
                StartCoroutine(PlayTextAnim("시작까지 1"));
                hasCounted1 = true;
            }

            yield return null;
        }

        // 실제 스폰 로직 시작
        StartCoroutine(SpawnFlow(roomType));
    }

    IEnumerator SpawnFlow(RoomType currentRoomType)
    {
        // 1. 현재 룸 타입에 맞는 페이즈 리스트 선택
        List<PhaseData> currentPhases = GetPhasesForRoomType(currentRoomType);

        if (currentPhases == null || currentPhases.Count == 0)
        {
            Debug.LogWarning($"{currentRoomType} 타입에 설정된 페이즈 데이터가 없습니다. 스폰을 건너뜁니다.");
            spawningFinished = true;
            StartCoroutine(CheckClearState()); // 페이즈가 없더라도 클리어 상태 체크는 시작
            yield break;
        }

        for (int i = 0; i < currentPhases.Count; i++)
        {
            var phase = currentPhases[i];
            Debug.Log($"== {currentRoomType} - {i + 1} 페이즈 시작 ==");

            // 페이즈 시작 텍스트 알림
            StartCoroutine(PlayTextAnim($"페이즈 {i + 1} 시작"));

            isPhaseActive = true;

            // 페이즈 시작 즉시 스폰
            foreach (var spawn in phase.spawns)
            {
                SpawnEnemiesRandomly(spawn);
            }

            float timer = 0f;

            // IntervalTime 동안 다음 두 조건을 체크하며 대기합니다.
            while (timer < IntervalTime && isPhaseActive)
            {
                // 모든 적이 제거되었다면 (aliveEnemies.Count == 0) 즉시 루프 탈출
                if (aliveEnemies.Count == 0)
                {
                    Debug.Log($"모든 적 제거! {IntervalTime - timer:F2}초 남았지만 즉시 다음 페이즈로 이동");
                    break;
                }

                timer += Time.deltaTime;
                yield return null; // 1프레임 대기
            }

            isPhaseActive = false;

            // 만약 인터벌 타임이 초과되었는데도 적이 남아있다면 경고 로그 출력
            if (aliveEnemies.Count > 0 && timer >= IntervalTime)
            {
                Debug.Log($"인터벌 시간 초과 ({IntervalTime}초). 다음 페이즈로 강제 진입 (남은 적: {aliveEnemies.Count}마리)");
            }
        }

        spawningFinished = true;

        // 모든 페이즈가 끝났으므로 최종 클리어 상태 체크를 시작
        StartCoroutine(CheckClearState());
    }

    /// <summary>
    /// UI 텍스트 애니메이션: 크기 확장 + 페이드 아웃
    /// </summary>
    IEnumerator PlayTextAnim(string message)
    {
        if (infoText == null) yield break;

        infoText.text = message;
        infoText.alpha = 1f;
        infoText.transform.localScale = Vector3.one;

        float duration = 1.0f; // 애니메이션 총 시간
        float elapsed = 0f;

        Vector3 startScale = Vector3.one;
        Vector3 targetScale = Vector3.one * 1.5f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // 1. 사이즈 점점 커짐
            infoText.transform.localScale = Vector3.Lerp(startScale, targetScale, t);

            // 2. 점점 투명화 (알파값 감소)
            infoText.alpha = Mathf.Lerp(1f, 0f, t);

            yield return null;
        }

        infoText.alpha = 0f;
    }

    /// <summary>
    /// 현재 룸 타입에 맞는 페이즈 리스트를 반환합니다.
    /// </summary>
    private List<PhaseData> GetPhasesForRoomType(RoomType roomType)
    {
        switch (roomType)
        {
            case RoomType.Normal:
                return normalPhases;
            case RoomType.Elite:
                return elitePhases;
            case RoomType.Boss:
                return bossPhases;
            default:
                Debug.LogWarning($"RoomType {roomType}에는 스폰 로직이 정의되어 있지 않습니다.");
                return null;
        }
    }

    /// <summary>
    /// 설정된 프리팹 리스트 중 랜덤하게 선택하고, 지정된 범위 내의 개수만큼 적을 소환합니다.
    /// </summary>
    void SpawnEnemiesRandomly(SpawnInfo info)
    {
        if (info.enemyPrefabs == null || info.enemyPrefabs.Count == 0)
        {
            Debug.LogWarning($"{info.spawnPoint.name}에 할당된 에너미 프리팹이 없습니다.");
            return;
        }

        // 최소~최대 범위 내에서 소환할 개수 결정
        int finalSpawnCount = Random.Range(info.minCount, info.maxCount + 1);

        for (int i = 0; i < finalSpawnCount; i++)
        {
            // 리스트에 등록된 프리팹 중 하나를 랜덤으로 선택
            int randomIndex = Random.Range(0, info.enemyPrefabs.Count);
            GameObject selectedPrefab = info.enemyPrefabs[randomIndex];

            if (selectedPrefab == null) continue;

            float offsetX = Random.Range(-info.spawnOffset, info.spawnOffset);
            float offsetZ = Random.Range(-info.spawnOffset, info.spawnOffset);

            Vector3 pos = info.spawnPoint.position + new Vector3(offsetX, 0f, offsetZ);

            // [전역 설정] 모든 몬스터 생성 시 공통 이펙트 소환
            if (globalSpawnEffectPrefab != null)
            {
                GameObject effect = Instantiate(globalSpawnEffectPrefab, pos, Quaternion.identity);
                // 생성 직후 0.8초 뒤에 자동으로 파괴되도록 설정
                Destroy(effect, 0.8f);
            }

            GameObject enemy = Instantiate(selectedPrefab, pos, info.spawnPoint.rotation);

            aliveEnemies.Add(enemy);

            // 적이 죽었을 때 리스트에서 제거하도록 하는 바인더 추가
            EnemyLifeBinder binder = enemy.AddComponent<EnemyLifeBinder>();
            binder.manager = this;
        }

        Debug.Log($"{info.spawnPoint.name}에서 {finalSpawnCount}마리의 적을 랜덤하게 소환했습니다.");
    }

    // Enemy 가 사망 시 호출될 함수
    public void NotifyEnemyDeath(GameObject enemy)
    {
        aliveEnemies.Remove(enemy);
    }

    IEnumerator CheckClearState()
    {
        if (Player.Instance.isDead) yield break;
        while (true)
        {
            if (spawningFinished && aliveEnemies.Count == 0)
            {
                Debug.Log("모든 적 제거 → 던전 클리어");

                if(DungeonManager.Instance.currentRoomType == RoomType.Boss)
                {
                    SoundManager.Instance.StopBGM();
                    SoundManager.Instance.PlayBGM("bossVictory");
                }
                else
                {
                    SoundManager.Instance.StopBGM();
                    SoundManager.Instance.PlayBGM("battleVictory");
                }

                    // 클리어 텍스트 애니메이션 실행
                    StartCoroutine(PlayTextAnim("클리어"));

                OnAllEnemiesCleared?.Invoke(); // 방 클리어 이벤트 발생
                HandleRoomCleared(); // 리워드 생성/포탈 활성화 로직 호출
                yield break;
            }

            yield return new WaitForSeconds(0.2f);
        }
    }

    /// <summary>
    /// 방 클리어 시 리워드 생성 및 포탈 활성화 로직
    /// </summary>
    private void HandleRoomCleared()
    {
        Debug.Log("== 던전 방 클리어 (리워드 생성 / 포탈 활성화) ==");

        SpawnAllRewards();
        ActivatePortal();
    }

    /// <summary>
    /// 설정된 3개의 리워드 슬롯을 확인하고 각각 스폰합니다.
    /// </summary>
    private void SpawnAllRewards()
    {
        int rewardsSpawned = 0;

        if (SpawnSingleReward(rewardPrefab, reward1SpawnPoint, 1))
            rewardsSpawned++;

        if (SpawnSingleReward(rewardPrefab, reward2SpawnPoint, 2))
            rewardsSpawned++;

        if (SpawnSingleReward(rewardPrefab, reward3SpawnPoint, 3))
            rewardsSpawned++;

        Debug.Log($"총 {rewardsSpawned}개의 리워드가 생성되었습니다.");
    }

    private bool SpawnSingleReward(GameObject prefab, Transform spawnPoint, int slotIndex)
    {
        if (prefab == null)
        {
            if (spawnPoint != null)
                Debug.LogWarning($"리워드 {slotIndex}의 프리팹이 설정되지 않았습니다. 스폰을 건너뜁니다.");
            return false;
        }

        if (spawnPoint == null)
        {
            Debug.LogWarning($"리워드 {slotIndex}의 스폰 위치가 설정되지 않았습니다. 스폰을 건너뜁니다.");
            return false;
        }

        GameObject go = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
        RewardItemUIController spawnedReward = go.GetComponent<RewardItemUIController>();
        rewards.Add(spawnedReward);
        Debug.Log($"리워드 {slotIndex} 생성 완료: {prefab.name} @ {spawnPoint.name}");
        return true;
    }

    public void DestroyAllRewards()
    {
        for (int i = 0; i < rewards.Count; i++)
        {
            if (rewards[i] != null)
                Destroy(rewards[i].gameObject);
        }
        rewards.Clear();
    }

    /// <summary>
    /// 씬 내에 배치된 포탈 오브젝트를 활성화합니다.
    /// </summary>
    private void ActivatePortal()
    {
        if (portalObject != null)
        {
            portalObject.SetActive(true);
            Debug.Log("포탈 활성화 완료");
        }
        else
        {
            Debug.LogWarning("활성화할 포탈 오브젝트(portalObject)가 연결되지 않았습니다.");
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}

public class EnemyLifeBinder : MonoBehaviour
{
    public SpawnManager manager;

    private void OnDestroy()
    {
        if (manager != null)
            manager.NotifyEnemyDeath(gameObject);
    }
}