using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 몬스터 전용 디버그 룸 매니저
/// UI 버튼과 연동하여 몬스터 소환, 패턴/특성 강제 주입, 행동 제어(AI On/Off)를 수행
/// </summary>
public class MonsterDebugManager : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("테스트할 몬스터 프리팹들을 할당합니다.")]
    public List<GameObject> monsterPrefabs;
    
    [Tooltip("몬스터가 소환될 디버그 룸 중앙 위치")]
    public Transform spawnPoint;

    [Header("Debug Traits & Patterns")]
    [Tooltip("테스트용 공격 패턴 프리팹 리스트")]
    public List<MonsterTrait> debugPatternPrefabs;
    
    [Tooltip("테스트용 엘리트 특성 프리팹 리스트")]
    public List<MonsterTrait> debugEliteTraitPrefabs;

    //현재 디버그 룸에 소환된 몬스터 객체
    private GameObject currentMonsterObject;
    private MonsterController currentMonsterController;
    
    //현재 부여된 특성들을 추적하기 위한 리스트
    private List<MonsterTrait> activeTraits = new List<MonsterTrait>();

    /// <summary>
    /// UI 버튼에서 호출하여 특정 인덱스의 몬스터를 소환
    /// </summary>
    public void SpawnMonster(int monsterIndex)
    {
        if (monsterPrefabs == null || monsterIndex < 0 || monsterIndex >= monsterPrefabs.Count)
        {
            Debug.LogWarning("<color=red>[Debug]</color> 유효하지 않은 몬스터 인덱스입니다.");
            return;
        }

        //기존 몬스터가 있다면 삭제하여 룸을 초기화
        ClearCurrentMonster();

        //새로운 몬스터를 스폰합니다.
        currentMonsterObject = Instantiate(monsterPrefabs[monsterIndex], spawnPoint.position, Quaternion.identity);
        currentMonsterController = currentMonsterObject.GetComponent<MonsterController>();

        //디버그 모드이므로 기본 패턴 매니저가 자동 작동하지 않도록 끔
        MonsterPatternManager patternManager = currentMonsterObject.GetComponent<MonsterPatternManager>();
        if (patternManager != null)
        {
            patternManager.enabled = false;
        }

        Debug.Log($"<color=green>[Debug]</color> 몬스터 소환 완료: {currentMonsterObject.name}");
    }

    /// <summary>
    /// UI 버튼에서 호출하여 현재 소환된 몬스터를 삭제
    /// </summary>
    public void ClearCurrentMonster()
    {
        if (currentMonsterObject != null)
        {
            Destroy(currentMonsterObject);
            currentMonsterObject = null;
            currentMonsterController = null;
            activeTraits.Clear();
            Debug.Log("<color=yellow>[Debug]</color> 몬스터 삭제 완료");
        }
    }

    /// <summary>
    /// 특정 인덱스의 공격 패턴을 몬스터에게 강제 부여 (UI 버튼 연동용)
    /// </summary>
    public void ApplyPattern(int patternIndex)
    {
        if (currentMonsterController == null) return;
        if (debugPatternPrefabs == null || patternIndex < 0 || patternIndex >= debugPatternPrefabs.Count) return;

        MonsterTrait spawnedTrait = Instantiate(debugPatternPrefabs[patternIndex], currentMonsterObject.transform);
        currentMonsterController.AttachDynamicTrait(spawnedTrait);
        activeTraits.Add(spawnedTrait);

        Debug.Log($"<color=cyan>[Debug]</color> 패턴 부여 완료: {spawnedTrait.gameObject.name}");
    }

    /// <summary>
    /// 특정 인덱스의 엘리트 특성을 몬스터에게 강제 부여 (UI 버튼 연동용)
    /// </summary>
    public void ApplyEliteTrait(int traitIndex)
    {
        if (currentMonsterController == null) return;
        if (debugEliteTraitPrefabs == null || traitIndex < 0 || traitIndex >= debugEliteTraitPrefabs.Count) return;

        MonsterTrait spawnedTrait = Instantiate(debugEliteTraitPrefabs[traitIndex], currentMonsterObject.transform);
        currentMonsterController.AttachDynamicTrait(spawnedTrait);
        activeTraits.Add(spawnedTrait);

        Debug.Log($"<color=cyan>[Debug]</color> 엘리트 특성 부여 완료: {spawnedTrait.gameObject.name}");
    }

    /// <summary>
    /// 현재 몬스터에게 부여된 모든 패턴과 특성을 제거
    /// </summary>
    public void ClearAllTraits()
    {
        if (currentMonsterController == null) return;

        foreach (var trait in activeTraits)
        {
            if (trait != null)
            {
                Destroy(trait.gameObject);
            }
        }
        activeTraits.Clear();
        Debug.Log("<color=yellow>[Debug]</color> 모든 패턴/특성 초기화 완료");
    }

    /// <summary>
    /// 기획서 요구사항인 몬스터의 '행동 여부(AI)'를 강제로 켜고 끔
    /// </summary>
    public void ToggleMonsterAI(bool isActive)
    {
        if (currentMonsterController != null)
        {
            currentMonsterController.enabled = isActive;
            Debug.Log($"<color=magenta>[Debug]</color> 몬스터 AI 상태: {isActive}");
        }
    }
}