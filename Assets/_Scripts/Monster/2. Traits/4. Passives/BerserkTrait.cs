using UnityEngine;

/// <summary>
/// 체력이 일정 비율 이하로 떨어지면 이동 속도와  크기가 증가하는 광폭화 패시브
/// </summary>
public class BerserkTrait : MonsterTrait
{
    [Header("Berserk Settings")]
    [Tooltip("광폭화가 발동될 체력 비율 (0.0 ~ 1.0")]
    [Range(0.1f, 0.9f)] public float triggerHpRatio = 0.2f;

    [Tooltip("광폭화 시 증가할 이동 속도 배율")]
    public float speedMultiplier = 1.5f;

    [Tooltip("광폭화 시 커질 크기 배율")]
    public float scaleMultiplier = 1.3f;

    private bool isBerserkActive = false;
    private Renderer meshRenderer;
    private Color originalColor;

    public override void Setup(MonsterController controller)
    {
        base.Setup(controller);

        //임시 3D 모델의 색상 변경을 위한 Renderer 연결
        meshRenderer = GetComponentInChildren<Renderer>();
        
        if (meshRenderer != null) originalColor = meshRenderer.material.color;
    }

    public override void OnTakeDamage(float damage)
    {
        //이미 광폭화 상태라면 무시
        if (isBerserkActive) return;

        //현재 체력 비율 계산
        float currentHpRatio = controller.CurrentHP / controller.maxHP;

        //체력이 설정한 비율 이하로 떨어지면 광폭화 발동
        if (currentHpRatio <= triggerHpRatio) ActivateBerserk();
    }

    /// <summary>
    /// 광폭화 발동 처리
    /// </summary>
    private void ActivateBerserk()
    {
        isBerserkActive = true;
        Debug.Log($"<color=red>[Elite Trait]</color> {gameObject.name} 광폭화 발동");

        //이동 속도 증가
        controller.moveSpeed *= speedMultiplier;

        //크기 증가
        transform.localScale *= scaleMultiplier;

        //시각적 피드백: 몸 색상을 변경
        if (meshRenderer != null) meshRenderer.material.color = new Color(1f, 0.4f, 0f);

        // TODO: 나중에 여기에 땀방울이 튀거나 붉은 오라가 뿜어지는 파티클 효과를 Instantiate 할 수 있음
    }
}
