using UnityEngine;

public class PlantBuffBannerTrait : MonsterTrait
{
    [Header("Banner Settings")]
    [Tooltip("소환할 깃발 프리팹 (체력 스크립트와 버프 스크립트 포함)")]
    public GameObject bannerPrefab;
    
    [Tooltip("깃발 소환 쿨타임")]
    public float plantCooldown = 15f;
    
    private float nextPlantTime;

    public override void Setup(MonsterController controller)
    {
        base.Setup(controller);
        
        //등장 후 첫 깃발 생성 대기 시간 부여
        nextPlantTime = Time.time + 3f;
    }

    private void Update()
    {
        //생존 상태 및 스턴 여부 확인
        if (controller == null || !controller.IsAlive || controller.IsStunned) return;

        //쿨타임 도달 시 깃발 생성
        if (Time.time >= nextPlantTime)
        {
            PlantBanner();
            nextPlantTime = Time.time + plantCooldown;
        }
    }

    private void PlantBanner()
    {
        //프리팹 할당 누락 시 안전 종료
        if (bannerPrefab == null) return;

        //자신의 현재 위치 바닥에 깃발 소환 위치 설정
        Vector3 spawnPos = transform.position;
        spawnPos.y = 0f;

        Instantiate(bannerPrefab, spawnPos, Quaternion.identity);
        Debug.Log($"// {gameObject.name} 버프 깃발 설치 완료");
    }
}