using UnityEngine;
using System.Collections;

public class KingSlimeBossController : MonoBehaviour
{
    [Header("Boss Settings")]
    public float phaseTwoHealthRatio = 0.7f;
    public float phaseThreeHealthRatio = 0.3f;
    
    [Header("Prefabs")]
    public GameObject poisonPoolPrefab;
    public GameObject slimeClonePrefab;
    public GameObject slimeWavePrefab;
    
    //추가된 체력 회복 기둥 프리팹과 소환 위치 배열
    public GameObject healingPillarPrefab;
    public Transform[] pillarSpawnPoints;

    private MonsterController controller;
    private int currentPhase = 1;
    private bool isTransitioning = false;

    private void Awake()
    {
        //컨트롤러 연결 및 초기화
        controller = GetComponent<MonsterController>();
    }

    private void Start()
    {
        StartCoroutine(BossPatternLoop());
    }

    private void Update()
    {
        //체력 비율에 따른 페이즈 전환 체크
        if (controller == null || !controller.IsAlive) return;
        CheckPhaseTransition();
    }

    private void CheckPhaseTransition()
    {
        //페이즈 전환중이거나 공격중이면 대기
        if (isTransitioning || controller.IsAttacking) return;

        float healthRatio = controller.CurrentHP / controller.maxHP;
        
        //3페이즈 진입 조건 체크
        if (currentPhase == 2 && healthRatio <= phaseThreeHealthRatio)
        {
            StartCoroutine(TransitionToPhase(3));
        }

        //2페이즈 진입 조건 체크
        else if (currentPhase == 1 && healthRatio <= phaseTwoHealthRatio)
        {
            StartCoroutine(TransitionToPhase(2));
        }
    }

    private IEnumerator TransitionToPhase(int nextPhase)
    {
        //페이즈 전환 상태 잠금
        isTransitioning = true;
        controller.IsAttacking = true;

        currentPhase = nextPhase;
        
        //무적 상태나 연출 대기 시간 부여
        yield return new WaitForSeconds(2.0f);

        if (currentPhase == 3)
        {
            //3페이즈 진입 시 광폭화 및 회복 패턴 발동
            TriggerEnrageAndPillars();
        }

        controller.IsAttacking = false;
        isTransitioning = false;
    }

    private IEnumerator BossPatternLoop()
    {
        //보스가 살아있는 동안 무한 반복
        while (controller != null && controller.IsAlive)
        {
            //공격중이 아닐 때만 새로운 패턴 실행
            if (!controller.IsAttacking && !isTransitioning)
            {
                ExecuteRandomPattern();
            }

            //다음 패턴까지 대기
            yield return new WaitForSeconds(3.0f);
        }
    }

    private void ExecuteRandomPattern()
    {
        //타겟이 없으면 패턴 취소
        if (controller.CurrentTarget == null) return;
        
        float randomValue = Random.value;

        //현재 페이즈에 따라 패턴 분기
        if (currentPhase == 1) ExecutePhaseOnePattern(randomValue);
        else if (currentPhase == 2) ExecutePhaseTwoPattern(randomValue);
        else if (currentPhase == 3) ExecutePhaseThreePattern(randomValue);
    }

    private void ExecutePhaseOnePattern(float rand)
    {
        //확률에 따라 기본 공격, 점프공격, 독장판
        if (rand < 0.4f) StartCoroutine(NormalAttack());
        else if (rand < 0.7f) StartCoroutine(JumpAttack());
        else StartCoroutine(SpawnPoisonPool());
    }

    private void ExecutePhaseTwoPattern(float rand)
    {
        //2페이즈 추가 기믹: 분신 투척 및 웨이브
        if (rand < 0.3f) StartCoroutine(ThrowSlimeClone());
        else if (rand < 0.6f) StartCoroutine(SlimeWave());
        else StartCoroutine(JumpAttack());
    }

    private void ExecutePhaseThreePattern(float rand)
    {
        //3페이즈 광폭화 상태 공격
        if (rand < 0.4f) StartCoroutine(SlimeWave());
        else if (rand < 0.8f) StartCoroutine(ThrowSlimeClone());
        else StartCoroutine(SpawnPoisonPool());
    }

    private IEnumerator NormalAttack()
    {
        controller.IsAttacking = true;

        //선 딜레이 대기
        yield return new WaitForSeconds(0.5f);

        //후 딜레이 대기
        yield return new WaitForSeconds(0.5f);
        controller.IsAttacking = false;
    }

    private IEnumerator JumpAttack()
    {
        controller.IsAttacking = true;

        //점프 공격 도약 전 대기
        yield return new WaitForSeconds(1.0f);
        
        //착지 후 경직 대기
        yield return new WaitForSeconds(1.0f);
        controller.IsAttacking = false;
    }

    private IEnumerator SpawnPoisonPool()
    {
        controller.IsAttacking = true;
        yield return new WaitForSeconds(0.5f);
        
        //독 장판 프리팹 생성
        if (poisonPoolPrefab != null)
        {
            Instantiate(poisonPoolPrefab, controller.CurrentTarget.position, Quaternion.identity);
        }
        
        yield return new WaitForSeconds(0.5f);
        controller.IsAttacking = false;
    }

    private IEnumerator ThrowSlimeClone()
    {
        controller.IsAttacking = true;
        yield return new WaitForSeconds(0.5f);
        
        //분신체 생성 및 투척
        if (slimeClonePrefab != null)
        {
            Instantiate(slimeClonePrefab, transform.position, Quaternion.identity);
        }
        
        yield return new WaitForSeconds(0.5f);
        controller.IsAttacking = false;
    }

    private IEnumerator SlimeWave()
    {
        controller.IsAttacking = true;
        yield return new WaitForSeconds(1.0f);
        
        //전방위 슬라임 웨이브 발사
        if (slimeWavePrefab != null)
        {
            Instantiate(slimeWavePrefab, transform.position, Quaternion.identity);
        }
        
        yield return new WaitForSeconds(1.0f);
        controller.IsAttacking = false;
    }

    private void TriggerEnrageAndPillars()
    {
        //광폭화 스탯 강화
        controller.moveSpeed *= 1.5f;

        //회복 패턴
        SpawnHealingPillars();
    }

    private void SpawnHealingPillars()
    {
        //기둥 프리팹이나 생성 위치가 설정되지 않았다면 안전하게 빠져나갑니다
        if (healingPillarPrefab == null || pillarSpawnPoints == null) return;

        //설정된 모든 위치에 기둥을 생성하고 보스 정보를 넘겨줍니다
        foreach (Transform spawnPoint in pillarSpawnPoints)
        {
            GameObject pillar = Instantiate(healingPillarPrefab, spawnPoint.position, Quaternion.identity);
            HealingPillar pillarScript = pillar.GetComponent<HealingPillar>();
            
            if (pillarScript != null)
            {
                pillarScript.Setup(controller);
            }
        }
    }
}