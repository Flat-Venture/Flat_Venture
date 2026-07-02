using System;
using System.Collections;
using FlatVenture.NUH.Common.Pooling;
using FlatVenture.NUH.Player.Combat;
using FlatVenture.NUH.Player.Input;
using UnityEngine;

namespace FlatVenture.NUH.Player.Skills.Warrior
{
    public enum ActiveSkillInputMode
    {
        HoldAndRelease,
        QuickCast
    }

    /// <summary>우클릭 조준과 3연발 검기 스킬의 시전·쿨타임을 관리합니다.</summary>
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(PlayerInputReader))]
    public sealed class WarriorSwordWaveController : MonoBehaviour
    {
        [SerializeField] private WarriorSwordWaveProjectile projectilePrefab;
        [SerializeField] private ActiveSkillInputMode inputMode = ActiveSkillInputMode.HoldAndRelease;

        private PlayerController player;
        private PlayerInputReader inputReader;
        private PlayerBasicAttackController basicAttack;
        private Camera mainCamera;
        private Vector3 aimDirection = Vector3.forward;
        private float cooldownRemaining;
        private bool isCasting;
        private bool ignoreCooldown;
        private ComponentObjectPool<WarriorSwordWaveProjectile> projectilePool;
        private Transform poolContainer;

        public Vector3 AimDirection => aimDirection;
        public float CooldownRemaining => cooldownRemaining;
        public bool IsAiming { get; private set; }
        public bool IsCasting => isCasting;
        public bool IgnoreCooldown => ignoreCooldown;
        public ActiveSkillInputMode InputMode => inputMode;
        public int PoolActiveCount => projectilePool?.CountActive ?? 0;
        public int PoolInactiveCount => projectilePool?.CountInactive ?? 0;
        public int PoolTotalCount => projectilePool?.CountAll ?? 0;

        public event Action<Vector3> CastStarted;
        public event Action<int, Vector3> ProjectileFired;

        private void Awake()
        {
            player = GetComponent<PlayerController>();
            inputReader = GetComponent<PlayerInputReader>();
            basicAttack = GetComponent<PlayerBasicAttackController>();
            mainCamera = Camera.main;
            InitializePool();
        }

        private void OnEnable()
        {
            inputReader = GetComponent<PlayerInputReader>();
            inputReader.ActiveSkillPressed += OnSkillPressed;
            inputReader.ActiveSkillReleased += OnSkillReleased;
        }

        private void OnDisable()
        {
            if (inputReader != null)
            {
                inputReader.ActiveSkillPressed -= OnSkillPressed;
                inputReader.ActiveSkillReleased -= OnSkillReleased;
            }

            IsAiming = false;
        }

        private void OnDestroy()
        {
            projectilePool?.Dispose();
            if (poolContainer != null)
                Destroy(poolContainer.gameObject);
        }

        private void Update()
        {
            if (ignoreCooldown)
                cooldownRemaining = 0f;
            else
                cooldownRemaining = Mathf.Max(0f, cooldownRemaining - Time.deltaTime);

            if (player.RuntimeState == null || player.RuntimeState.IsDead)
            {
                IsAiming = false;
                return;
            }

            if (IsAiming)
                UpdateAimDirection();
        }

        public void ToggleIgnoreCooldown()
        {
            ignoreCooldown = !ignoreCooldown;
            if (ignoreCooldown)
                cooldownRemaining = 0f;
        }

        private void OnSkillPressed()
        {
            if (!CanStartCast())
                return;

            UpdateAimDirection();
            if (inputMode == ActiveSkillInputMode.QuickCast)
                BeginCast();
            else
                IsAiming = true;
        }

        private void OnSkillReleased()
        {
            if (inputMode != ActiveSkillInputMode.HoldAndRelease || !IsAiming)
                return;

            UpdateAimDirection();
            BeginCast();
        }

        private bool CanStartCast()
        {
            return projectilePrefab != null
                && player.RuntimeState != null
                && !player.RuntimeState.IsDead
                && !isCasting
                && (ignoreCooldown || cooldownRemaining <= 0f);
        }

        private void BeginCast()
        {
            IsAiming = false;
            isCasting = true;
            Vector3 lockedDirection = aimDirection;
            if (!ignoreCooldown)
                cooldownRemaining = player.RuntimeState.ActiveSkillCooldown;

            CastStarted?.Invoke(lockedDirection);
            StartCoroutine(CastRoutine(lockedDirection));
        }

        private IEnumerator CastRoutine(Vector3 lockedDirection)
        {
            float castTime = player.RuntimeState.ActiveSkillCastTime;
            if (castTime > 0f)
                yield return new WaitForSeconds(castTime);

            int projectileCount = player.RuntimeState.ActiveSkillProjectileCount;
            for (int i = 0; i < projectileCount; i++)
            {
                if (player.RuntimeState.IsDead)
                    break;

                FireProjectile(i + 1, lockedDirection);
                if (i < projectileCount - 1 && player.RuntimeState.ActiveSkillProjectileInterval > 0f)
                    yield return new WaitForSeconds(player.RuntimeState.ActiveSkillProjectileInterval);
            }

            basicAttack?.ApplyPostSkillCooldown();
            isCasting = false;
        }

        private void FireProjectile(int sequence, Vector3 lockedDirection)
        {
            Vector3 spawnPosition = transform.position + (lockedDirection * 0.8f) + (Vector3.up * 0.1f);
            WarriorSwordWaveProjectile projectile = projectilePool.Get();
            projectile.transform.SetPositionAndRotation(spawnPosition, Quaternion.identity);
            projectile.Initialize(
                transform,
                lockedDirection,
                player.RuntimeState.ActiveSkillProjectileSpeed,
                player.RuntimeState.ActiveSkillDamage,
                player.RuntimeState.ActiveSkillProjectileWidth,
                player.RuntimeState.ActiveSkillMaxHitTargets,
                ReleaseProjectile);
            ProjectileFired?.Invoke(sequence, lockedDirection);
        }

        private void InitializePool()
        {
            if (projectilePrefab == null)
                return;

            GameObject containerObject = new GameObject("Pool_WarriorSwordWave");
            poolContainer = containerObject.transform;
            projectilePool = new ComponentObjectPool<WarriorSwordWaveProjectile>(
                projectilePrefab,
                poolContainer,
                defaultCapacity: 6,
                maxSize: 32);
            projectilePool.Prewarm(6);
        }

        private void ReleaseProjectile(WarriorSwordWaveProjectile projectile)
        {
            projectilePool?.Release(projectile);
        }

        private void UpdateAimDirection()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;
            if (mainCamera == null)
                return;

            Ray ray = mainCamera.ScreenPointToRay(inputReader.PointerScreenPosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, Physics.AllLayers, QueryTriggerInteraction.Ignore))
            {
                SetAimDirection(hit.point - transform.position);
                return;
            }

            Plane plane = new Plane(Vector3.up, transform.position);
            if (plane.Raycast(ray, out float distance))
                SetAimDirection(ray.GetPoint(distance) - transform.position);
        }

        private void SetAimDirection(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f)
                aimDirection = direction.normalized;
        }
    }
}
