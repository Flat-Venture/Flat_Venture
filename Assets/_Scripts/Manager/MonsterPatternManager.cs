using UnityEngine;
using System.Collections.Generic;

public enum ActLevel
{
    Act1 = 1,
    Act2 = 2,
    Act3 = 3
}

public class MonsterPatternManager : MonoBehaviour
{
    [Header("Stage Settings")]
    public ActLevel currentAct = ActLevel.Act1;
    public int currentFloor = 1;
    public bool isElite = false;

    [Header("Pattern Pool")]
    //근접 몬스터면 근접 패턴만 원거리면 원거리 패턴만 인스펙터에서 할당
    public List<MonsterTrait> patternPrefabs;
    
    //엘리트 전용 특성인 물리 저항 증가 체력 리젠 등을 별도로 담는 리스트
    public List<MonsterTrait> eliteTraitPrefabs;

    private MonsterController controller;

    private void Awake()
    {
        controller = GetComponent<MonsterController>();
    }

    private void Start()
    {
        //게임 시작 시 막과 층수에 맞춰 패턴을 조합
        ApplyPatternsByStage();
    }

    private void ApplyPatternsByStage()
    {
        //프리팹 리스트가 비어있으면 오류를 방지하기 위해 빠져나감
        if (patternPrefabs == null || patternPrefabs.Count == 0) return;

        //일반 몬스터는 기본적으로 단일 패턴만 가짐
        int targetPatternCount = 1;

        //층이 올라갈수록 몬스터가 강화되도록 패턴 수를 늘려줌
        targetPatternCount += currentFloor / 5;

        //막이 진행됨에 따라 기본적으로 가지는 공격 패턴 수를 올림
        if (currentAct == ActLevel.Act2) targetPatternCount += 1;
        else if (currentAct == ActLevel.Act3) targetPatternCount += 2;

        List<MonsterTrait> shuffledPool = new List<MonsterTrait>(patternPrefabs);
        ShuffleList(shuffledPool);

        int finalCount = Mathf.Min(targetPatternCount, shuffledPool.Count);

        for (int i = 0; i < finalCount; i++)
        {
            MonsterTrait spawnedTrait = Instantiate(shuffledPool[i], transform);
            
            if (controller != null)
            {
                controller.AttachDynamicTrait(spawnedTrait);
            }
        }

        //엘리트 몬스터일 경우 막에 따라 별도의 특성을 추가로 부여
        if (isElite && eliteTraitPrefabs != null && eliteTraitPrefabs.Count > 0)
        {
            ApplyEliteTraits();
        }
    }

    private void ApplyEliteTraits()
    {
        //엘리트는 난이도인 막에 비례해 특성 수량이 증가
        int eliteTraitCount = (int)currentAct;

        List<MonsterTrait> shuffledElitePool = new List<MonsterTrait>(eliteTraitPrefabs);
        ShuffleList(shuffledElitePool);

        int finalEliteCount = Mathf.Min(eliteTraitCount, shuffledElitePool.Count);

        for (int i = 0; i < finalEliteCount; i++)
        {
            MonsterTrait spawnedTrait = Instantiate(shuffledElitePool[i], transform);
            
            if (controller != null)
            {
                controller.AttachDynamicTrait(spawnedTrait);
            }
        }
    }

    private void ShuffleList<T>(List<T> list)
    {
        //리스트를 무작위로 섞어 매번 다른 패턴이 나오게 만듦
        for (int i = 0; i < list.Count; i++)
        {
            T temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
}