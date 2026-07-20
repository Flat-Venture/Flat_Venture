using UnityEngine;

/// <summary>플레이어 공격 검증용 더미 타깃입니다.</summary>
[RequireComponent(typeof(Collider))]
public sealed class PlayerTargetDummy : MonoBehaviour, IPlayerAttackTarget
{
    // 테스트 더미의 최대 HP와 피해 상태를 색으로 보여 줄 Renderer입니다.
    [Min(1f)] [SerializeField] private float maxHealth = 30f;
    [SerializeField] private Renderer targetRenderer;

    // 사망 시 비활성화할 Collider와 현재 남은 HP입니다.
    private Collider targetCollider;
    private float currentHealth;

    public Transform TargetTransform { get { return transform; } }
    public bool IsAlive { get { return currentHealth > 0f; } }
    public float CurrentHealth { get { return currentHealth; } }

    /// <summary>Collider·Renderer를 찾고 최대 HP로 초기화합니다.</summary>
    private void Awake()
    {
        targetCollider = GetComponent<Collider>();
        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<Renderer>();

        currentHealth = maxHealth;
        UpdateColor();
    }

    /// <summary>살아 있을 때만 피해를 받고 HP가 0이면 Collider를 꺼 자동 조준에서 제외합니다.</summary>
    public void TakeDamage(float damage)
    {
        if (!IsAlive || damage <= 0f)
            return;

        currentHealth = Mathf.Max(0f, currentHealth - damage);
        if (!IsAlive)
            targetCollider.enabled = false;

        UpdateColor();
    }

    /// <summary>남은 HP 비율을 주황색 밝기로 표시합니다.</summary>
    private void UpdateColor()
    {
        if (targetRenderer == null)
            return;

        float ratio = maxHealth > 0f ? currentHealth / maxHealth : 0f;
        targetRenderer.material.color = Color.Lerp(Color.black, new Color(1f, 0.55f, 0.1f), ratio);
    }
}
