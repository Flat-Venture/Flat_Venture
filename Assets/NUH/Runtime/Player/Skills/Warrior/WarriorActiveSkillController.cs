using System.Collections;
using System.Collections.Generic;
using FlatVenture.NUH.Common.Pooling;
using FlatVenture.NUH.Player.Aiming;
using FlatVenture.NUH.Player.Combat;
using FlatVenture.NUH.Player.Input;
using FlatVenture.NUH.Player.Skills;
using UnityEngine;

namespace FlatVenture.NUH.Player.Skills.Warrior
{
    /// <summary>우클릭 조준과 3연발 검기 스킬의 시전·쿨타임을 관리합니다.</summary>
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(PlayerInputReader))]
    [RequireComponent(typeof(PlayerAimResolver))]
    public sealed class WarriorActiveSkillController : MonoBehaviour, IPlayerActiveSkillStatus
    {
        // 풀에서 복제·재사용할 검기 원본 프리팹입니다.
        [SerializeField] private WarriorActiveSkillProjectile projectilePrefab;
        // 환경설정에서 바꿀 수 있도록 분리한 스킬 입력 방식입니다.
        [SerializeField] private ActiveSkillInputMode inputMode = ActiveSkillInputMode.HoldAndRelease;

        // 플레이어 상태, 입력, 기본공격 후딜레이, 마우스 방향 계산에 사용하는 모듈 참조입니다.
        private PlayerController player;
        private PlayerInputReader inputReader;
        private PlayerBasicAttackController basicAttack;
        private PlayerAimResolver aimResolver;
        // 우클릭 조준 중 계속 갱신되며 시전 시작 순간 lockedDirection으로 복사됩니다.
        private Vector3 aimDirection = Vector3.forward;
        // 스킬 재사용까지 남은 게임 시간입니다.
        private float cooldownRemaining;
        // 선딜레이 또는 연발 Coroutine이 진행 중인지 나타냅니다.
        private bool isCasting;
        // 테스트 UI에서 쿨타임을 무시할 때만 사용하는 플래그입니다.
        private bool ignoreCooldown;
        // Instantiate/Destroy 반복을 피하기 위한 검기 오브젝트 풀입니다.
        private ComponentObjectPool<WarriorActiveSkillProjectile> projectilePool;
        // Hierarchy에서 비활성 검기들을 정리해 둘 부모 Transform입니다.
        private Transform poolContainer;
        // 재시작할 때 아직 날아가는 검기를 모두 풀로 돌려보내기 위한 목록입니다.
        private readonly List<WarriorActiveSkillProjectile> activeProjectiles =
            new List<WarriorActiveSkillProjectile>();

        public Vector3 AimDirection { get { return aimDirection; } }
        public float CooldownRemaining { get { return cooldownRemaining; } }
        public bool IsAiming { get; private set; }
        public bool IsCasting { get { return isCasting; } }
        public bool IgnoreCooldown { get { return ignoreCooldown; } }
        public ActiveSkillInputMode InputMode { get { return inputMode; } }
        public int PoolActiveCount { get { return projectilePool?.CountActive ?? 0; } }
        public int PoolInactiveCount { get { return projectilePool?.CountInactive ?? 0; } }
        public int PoolTotalCount { get { return projectilePool?.CountAll ?? 0; } }

        /// <summary>필수 모듈을 찾고 직렬화된 검기 프리팹으로 풀을 준비합니다.</summary>
        private void Awake()
        {
            player = GetComponent<PlayerController>();
            inputReader = GetComponent<PlayerInputReader>();
            basicAttack = GetComponent<PlayerBasicAttackController>();
            aimResolver = GetComponent<PlayerAimResolver>();
            InitializePool();
        }

        /// <summary>우클릭 Press/Release 입력 이벤트를 구독합니다.</summary>
        private void OnEnable()
        {
            inputReader = GetComponent<PlayerInputReader>();
            inputReader.ActiveSkillPressed += OnSkillPressed;
            inputReader.ActiveSkillReleased += OnSkillReleased;
        }

        /// <summary>재시작 이벤트를 구독해 쿨타임과 활성 투사체를 함께 초기화합니다.</summary>
        private void Start()
        {
            player.RuntimeState.ResetCompleted += ResetSkill;
        }

        /// <summary>입력 이벤트를 해제하고 조준 상태를 종료합니다.</summary>
        private void OnDisable()
        {
            if (inputReader != null)
            {
                inputReader.ActiveSkillPressed -= OnSkillPressed;
                inputReader.ActiveSkillReleased -= OnSkillReleased;
            }

            IsAiming = false;
        }

        /// <summary>이벤트·풀·풀 컨테이너를 정리합니다.</summary>
        private void OnDestroy()
        {
            if (player?.RuntimeState != null)
                player.RuntimeState.ResetCompleted -= ResetSkill;

            projectilePool?.Dispose();
            activeProjectiles.Clear();
            if (poolContainer != null)
                Destroy(poolContainer.gameObject);
        }

        /// <summary>쿨타임을 감소시키고 우클릭 조준 중이면 마우스 방향을 계속 갱신합니다.</summary>
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

        /// <summary>테스트용 쿨타임 무시 상태를 전환합니다.</summary>
        public void ToggleIgnoreCooldown()
        {
            ignoreCooldown = !ignoreCooldown;
            if (ignoreCooldown)
                cooldownRemaining = 0f;
        }

        /// <summary>우클릭을 누른 순간 시전 가능 여부를 확인하고 조준 또는 즉시 시전을 시작합니다.</summary>
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

        /// <summary>HoldAndRelease 모드에서 우클릭을 뗄 때 방향을 확정하고 시전합니다.</summary>
        private void OnSkillReleased()
        {
            if (inputMode != ActiveSkillInputMode.HoldAndRelease || !IsAiming)
                return;

            UpdateAimDirection();
            BeginCast();
        }

        /// <summary>프리팹·생존·시전 중복·쿨타임 조건을 한곳에서 검사합니다.</summary>
        private bool CanStartCast()
        {
            return projectilePrefab != null
                && player.RuntimeState != null
                && !player.RuntimeState.IsDead
                && !isCasting
                && (ignoreCooldown || cooldownRemaining <= 0f);
        }

        /// <summary>현재 방향과 쿨타임을 확정하고 검기 연발 Coroutine을 시작합니다.</summary>
        private void BeginCast()
        {
            IsAiming = false;
            isCasting = true;
            Vector3 lockedDirection = aimDirection;
            if (!ignoreCooldown)
                cooldownRemaining = player.RuntimeState.ActiveSkillCooldown;

            StartCoroutine(CastRoutine(lockedDirection));
        }

        /// <summary>선딜레이 후 같은 방향으로 설정된 수만큼 검기를 순차 발사합니다.</summary>
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

                FireProjectile(lockedDirection);
                if (i < projectileCount - 1 && player.RuntimeState.ActiveSkillProjectileInterval > 0f)
                    yield return new WaitForSeconds(player.RuntimeState.ActiveSkillProjectileInterval);
            }

            basicAttack?.ApplyPostSkillCooldown();
            isCasting = false;
        }

        /// <summary>풀에서 검기 하나를 꺼내 위치와 런타임 능력치를 전달합니다.</summary>
        private void FireProjectile(Vector3 lockedDirection)
        {
            Vector3 spawnPosition = transform.position + (lockedDirection * 0.8f) + (Vector3.up * 0.1f);
            WarriorActiveSkillProjectile projectile = projectilePool.Get();
            activeProjectiles.Add(projectile);
            projectile.transform.SetPositionAndRotation(spawnPosition, Quaternion.identity);
            projectile.Initialize(
                transform,
                lockedDirection,
                player.RuntimeState.ActiveSkillProjectileSpeed,
                player.RuntimeState.ActiveSkillDamage,
                player.RuntimeState.ActiveSkillProjectileWidth,
                player.RuntimeState.ActiveSkillMaxHitTargets,
                ReleaseProjectile);
        }

        /// <summary>직렬화된 프리팹으로 범용 풀을 만들고 초기 6개를 미리 생성합니다.</summary>
        private void InitializePool()
        {
            if (projectilePrefab == null)
                return;

            GameObject containerObject = new GameObject("Pool_WarriorActiveSkill");
            poolContainer = containerObject.transform;
            projectilePool = new ComponentObjectPool<WarriorActiveSkillProjectile>(
                projectilePrefab,
                poolContainer,
                defaultCapacity: 6,
                maxSize: 32);
            projectilePool.Prewarm(6);
        }

        /// <summary>수명이 끝나거나 충돌한 검기를 활성 목록에서 빼고 풀로 반환합니다.</summary>
        private void ReleaseProjectile(WarriorActiveSkillProjectile projectile)
        {
            activeProjectiles.Remove(projectile);
            projectilePool?.Release(projectile);
        }

        /// <summary>재시작 시 Coroutine·쿨타임·조준·활성 검기를 모두 초기화합니다.</summary>
        private void ResetSkill()
        {
            StopAllCoroutines();
            IsAiming = false;
            isCasting = false;
            ignoreCooldown = false;
            cooldownRemaining = 0f;

            if (projectilePool == null)
            {
                activeProjectiles.Clear();
                return;
            }

            for (int i = activeProjectiles.Count - 1; i >= 0; i--)
                projectilePool.Release(activeProjectiles[i]);

            activeProjectiles.Clear();
        }

        /// <summary>마우스 화면 좌표를 검기가 날아갈 XZ 방향으로 갱신합니다.</summary>
        private void UpdateAimDirection()
        {
            if (aimResolver.TryResolvePointerDirection(transform, Physics.AllLayers, out Vector3 direction))
                aimDirection = direction.normalized;
        }
    }
}
