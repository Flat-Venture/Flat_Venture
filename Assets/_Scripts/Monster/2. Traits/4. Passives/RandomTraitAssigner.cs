using UnityEngine;

/// <summary>
/// 몬스터 생성 시 지정된 특성(프리팹) 목록 중 하나를 무작위로 부착하는 독립 모듈
/// </summary>
public class RandomTraitAssigner : MonoBehaviour
{
    [Header("Random Traits")]
    [Tooltip("무작위로 하나를 뽑을 특성 프리팹 목록 (버프용 또는 디버프용만 할당)")]
    public GameObject[] traitPrefabs;

    private void Start()
    {
        AssignRandomTrait();
    }

    private void AssignRandomTrait()
    {
        //목록이 비어있으면 조기 종료
        if (traitPrefabs == null || traitPrefabs.Length == 0) return;

        //목록에서 무작위로 인덱스 하나를 선택
        int randomIndex = Random.Range(0, traitPrefabs.Length);
        GameObject selectedPrefab = traitPrefabs[randomIndex];

        if (selectedPrefab != null)
        {
            //몬스터의 자식 오브젝트로 특성 생성
            GameObject traitObj = Instantiate(selectedPrefab, transform.position, Quaternion.identity, transform);

            //자신의 MonsterController를 찾아 동적 특성 부착 함수 호출
            MonsterController controller = GetComponent<MonsterController>();
            MonsterTrait traitComponent = traitObj.GetComponent<MonsterTrait>();

            if (controller != null && traitComponent != null)
            {
                controller.AttachDynamicTrait(traitComponent);
                Debug.Log($"// {gameObject.name}에 {selectedPrefab.name} 특성이 전용으로 부여되었습니다");
            }
        }
    }
}