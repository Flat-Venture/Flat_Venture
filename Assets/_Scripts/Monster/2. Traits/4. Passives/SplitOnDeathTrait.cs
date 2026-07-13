using UnityEngine;

/// <summary>
/// 몬스터 사망 시 지정된 프리팹(작은 몬스터 등)을 소환하여 분열하는 특성
/// </summary>
public class SplitOnDeathTrait : MonsterTrait
{
    [Header("Split Settings")]
    [Tooltip("사망 시 소환할 분열 몬스터 프리팹")]
    public GameObject splitMonsterPrefab;
    [Tooltip("분열할 몬스터의 수")]
    public int splitCount = 2;
    [Tooltip("분열 시 퍼져나갈 반경")]
    public float spawnRadius = 1.5f;

    /// <summary>
    /// 몬스터의 체력이 0이 되어 사망할 때 컨트롤러가 자동으로 호출해주는 함수
    /// </summary>
    public override void OnDeath()
    {
        base.OnDeath(); // 부모 클래스의 OnDeath 우선 실행

        if (splitMonsterPrefab != null)
        {
            for (int i = 0; i < splitCount; i++)
            {
                //현재 위치 주변의 평면(X, Z) 상 랜덤 위치 계산
                Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
                Vector3 spawnPos = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);

                //분열 몬스터 생성
                Instantiate(splitMonsterPrefab, spawnPos, Quaternion.identity);
            }
            
            Debug.Log($"<color=orange>[Split]</color> {gameObject.name}이(가) 사망하며 {splitCount}마리로 분열했습니다!");
        }
    }
}