using System;
using UnityEngine;

public class TargetSensorTrait : MonsterTrait
{
    [Header("Target Sensor Settings")]
    [Tooltip("적을 감지할 시야 반경")]
    public float detectRadius = 10f;
    [Tooltip("플레이어의 레이어 마스트")]
    public LayerMask targetLayer;
    [Tooltip("센서 작동 주기(초)")]
    public float scanInterval = 0.2f;

    //GC(가비지 컬렉터) 방지용 최적화 배열 (최대 1명)
    private Collider[] hitColliders = new Collider[1];
    private float lastScanTime;

    private void Update()
    {
        //0.2초마다 한 번씩만 두리번 거림
        if (Time.time >= lastScanTime + scanInterval)
        {
            ScanForTarget();
            lastScanTime = Time.time;
        }
    }

    private void ScanForTarget()
    {
        //이미 타겟이 있다면 찾지 않음
        if (controller.CurrentTarget != null) return;

        //본인 주변 detectRadius 반경 안에 targetLayer가 있는지 체크
        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, detectRadius, hitColliders, targetLayer);

        if (hitCount > 0)
        {
            //찾았으면 콜리더에 타겟 갱신
            controller.CurrentTarget = hitColliders[0].transform;
            Debug.Log($"<color=cyan>[Sensor]</color> {gameObject.name}이(가) 타겟 발견");
        }
    }

    //센서 반경 기즈모
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 1, 1, 0.3f);
        Gizmos.DrawSphere(transform.position, detectRadius);
    }
}
