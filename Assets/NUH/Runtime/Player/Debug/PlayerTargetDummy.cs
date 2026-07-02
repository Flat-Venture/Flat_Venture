using FlatVenture.NUH.Player.Combat;
using UnityEngine;

namespace FlatVenture.NUH.Player.Debugging
{
    /// <summary>플레이어 공격 검증용 더미 타깃입니다.</summary>
    [RequireComponent(typeof(Collider))]
    public sealed class PlayerTargetDummy : MonoBehaviour, IPlayerAttackTarget
    {
        [Min(1f)] [SerializeField] private float maxHealth = 30f;
        [SerializeField] private Renderer targetRenderer;

        private Collider targetCollider;
        private float currentHealth;

        public Transform TargetTransform => transform;
        public bool IsAlive => currentHealth > 0f;
        public float CurrentHealth => currentHealth;

        private void Awake()
        {
            targetCollider = GetComponent<Collider>();
            if (targetRenderer == null)
                targetRenderer = GetComponentInChildren<Renderer>();

            currentHealth = maxHealth;
            UpdateColor();
        }

        public void TakeDamage(float damage)
        {
            if (!IsAlive || damage <= 0f)
                return;

            currentHealth = Mathf.Max(0f, currentHealth - damage);
            if (!IsAlive)
                targetCollider.enabled = false;

            UpdateColor();
        }

        private void UpdateColor()
        {
            if (targetRenderer == null)
                return;

            float ratio = maxHealth > 0f ? currentHealth / maxHealth : 0f;
            targetRenderer.material.color = Color.Lerp(Color.black, new Color(1f, 0.55f, 0.1f), ratio);
        }
    }
}
