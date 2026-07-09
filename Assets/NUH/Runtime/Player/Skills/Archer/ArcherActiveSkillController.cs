using System.Collections;
using System.Collections.Generic;
using FlatVenture.NUH.Common.Pooling;
using FlatVenture.NUH.Player.Aiming;
using FlatVenture.NUH.Player.Combat;
using FlatVenture.NUH.Player.Input;
using FlatVenture.NUH.Player.Skills;
using UnityEngine;

namespace FlatVenture.NUH.Player.Skills.Archer
{
    /// <summary>궁수 액티브 스킬인 관통 사격의 입력, 조준, 선딜레이, 쿨타임, 풀링을 관리합니다.</summary>
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(PlayerInputReader))]
    [RequireComponent(typeof(PlayerAimResolver))]
    public sealed class ArcherActiveSkillController : MonoBehaviour
    {
        // 풀에서 복제해 사용할 궁수 액티브 투사체 프리팹입니다.
        [SerializeField] private ArcherActiveSkillProjectile projectilePrefab;
        // 기본은 우클릭을 누르는 동안 조준하고 뗄 때 발동하는 방식입니다.
        [SerializeField] private ActiveSkillInputMode inputMode = ActiveSkillInputMode.HoldAndRelease;
        // 마우스 조준 Ray가 닿을 수 있는 표면 레이어입니다.
        [SerializeField] private LayerMask aimSurfaceMask = ~0;

        // 같은 플레이어 오브젝트에서 가져오는 상태·입력·기본공격·조준 모듈입니다.
        private PlayerController player;
        private PlayerInputReader inputReader;
        private PlayerBasicAttackController basicAttack;
        private PlayerAimResolver aimResolver;
        // 우클릭 조준 중 계속 갱신되는 XZ 방향입니다.
        private Vector3 aimDirection = Vector3.forward;
        // 0이 되면 다음 액티브 스킬을 사용할 수 있습니다.
        private float cooldownRemaining;
        // 선딜레이 Coroutine이 진행 중인지 기록합니다.
        private bool isCasting;
        // 테스트 UI에서 쿨타임을 무시할 때 사용합니다.
        private bool ignoreCooldown;
        // 관통 사격 투사체의 재사용 풀과 현재 활성 투사체 목록입니다.
        private ComponentObjectPool<ArcherActiveSkillProjectile> projectilePool;
        private Transform poolContainer;
        private readonly List<ArcherActiveSkillProjectile> activeProjectiles =
            new List<ArcherActiveSkillProjectile>();

        public Vector3 AimDirection { get { return aimDirection; } }
        public float CooldownRemaining { get { return cooldownRemaining; } }
        public bool IsAiming { get; private set; }
        public bool IsCasting { get { return isCasting; } }
        public bool IgnoreCooldown { get { return ignoreCooldown; } }
        public ActiveSkillInputMode InputMode { get { return inputMode; } }
        public int PoolActiveCount { get { return projectilePool?.CountActive ?? 0; } }
        public int PoolInactiveCount { get { return projectilePool?.CountInactive ?? 0; } }
        public int PoolTotalCount { get { return projectilePool?.CountAll ?? 0; } }

        /// <summary>필수 컴포넌트를 캐시하고 프리팹이 있으면 투사체 풀을 준비합니다.</summary>
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

        /// <summary>플레이어 재시작 시 스킬 상태도 초기화되도록 이벤트를 구독합니다.</summary>
        private void Start()
        {
            player.RuntimeState.ResetCompleted += ResetSkill;
        }

        /// <summary>비활성화될 때 입력 이벤트를 해제하고 조준 상태를 끕니다.</summary>
        private void OnDisable()
        {
            if (inputReader != null)
            {
                inputReader.ActiveSkillPressed -= OnSkillPressed;
                inputReader.ActiveSkillReleased -= OnSkillReleased;
            }

            IsAiming = false;
        }

        /// <summary>오브젝트 제거 시 이벤트와 풀을 정리합니다.</summary>
        private void OnDestroy()
        {
            if (player?.RuntimeState != null)
                player.RuntimeState.ResetCompleted -= ResetSkill;

            projectilePool?.Dispose();
            activeProjectiles.Clear();
            if (poolContainer != null)
                Destroy(poolContainer.gameObject);
        }

        /// <summary>쿨타임을 줄이고 우클릭 조준 중이면 마우스 방향을 계속 갱신합니다.</summary>
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

        /// <summary>우클릭을 누르는 순간 조준 또는 즉시 시전을 시작합니다.</summary>
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

        /// <summary>우클릭을 뗄 때 HoldAndRelease 방식이면 현재 방향으로 시전합니다.</summary>
        private void OnSkillReleased()
        {
            if (inputMode != ActiveSkillInputMode.HoldAndRelease || !IsAiming)
                return;

            UpdateAimDirection();
            BeginCast();
        }

        /// <summary>프리팹, 생존 상태, 시전 중복, 쿨타임 조건을 검사합니다.</summary>
        private bool CanStartCast()
        {
            return projectilePrefab != null
                && player.RuntimeState != null
                && !player.RuntimeState.IsDead
                && !isCasting
                && (ignoreCooldown || cooldownRemaining <= 0f);
        }

        /// <summary>현재 조준 방향을 잠그고 쿨타임을 시작한 뒤 시전 Coroutine을 실행합니다.</summary>
        private void BeginCast()
        {
            IsAiming = false;
            isCasting = true;
            Vector3 lockedDirection = aimDirection;
            if (!ignoreCooldown)
                cooldownRemaining = player.RuntimeState.ActiveSkillCooldown;

            StartCoroutine(CastRoutine(lockedDirection));
        }

        /// <summary>설정된 선딜레이 후 한 발의 관통 사격을 발사합니다.</summary>
        private IEnumerator CastRoutine(Vector3 lockedDirection)
        {
            float castTime = player.RuntimeState.ActiveSkillCastTime;
            if (castTime > 0f)
                yield return new WaitForSeconds(castTime);

            if (!player.RuntimeState.IsDead)
                FireProjectile(lockedDirection);

            basicAttack?.ApplyPostSkillCooldown();
            isCasting = false;
        }

        /// <summary>풀에서 관통 사격 하나를 꺼내 런타임 스탯과 함께 초기화합니다.</summary>
        private void FireProjectile(Vector3 lockedDirection)
        {
            const float spawnForwardOffset = 0.8f;
            Vector3 spawnPosition = transform.position + (lockedDirection * spawnForwardOffset) + (Vector3.up * 0.15f);
            ArcherActiveSkillProjectile projectile = projectilePool.Get();
            activeProjectiles.Add(projectile);
            projectile.Initialize(
                transform,
                spawnPosition,
                lockedDirection,
                player.RuntimeState.ActiveSkillProjectileSpeed,
                player.RuntimeState.ActiveSkillDamage,
                player.RuntimeState.ActiveSkillProjectileWidth,
                Mathf.Max(0.01f, player.RuntimeState.ActiveSkillRange - spawnForwardOffset),
                ReleaseProjectile);
        }

        /// <summary>직렬화된 프리팹으로 관통 사격 풀을 만들고 초기 수량을 미리 생성합니다.</summary>
        private void InitializePool()
        {
            if (projectilePrefab == null)
                return;

            GameObject containerObject = new GameObject("Pool_ArcherPiercingShot");
            poolContainer = containerObject.transform;
            projectilePool = new ComponentObjectPool<ArcherActiveSkillProjectile>(
                projectilePrefab,
                poolContainer,
                defaultCapacity: 4,
                maxSize: 32);
            projectilePool.Prewarm(4);
        }

        /// <summary>사라진 투사체를 활성 목록에서 제거하고 풀에 반환합니다.</summary>
        private void ReleaseProjectile(ArcherActiveSkillProjectile projectile)
        {
            activeProjectiles.Remove(projectile);
            projectilePool?.Release(projectile);
        }

        /// <summary>재시작 시 Coroutine, 쿨타임, 조준 상태, 활성 투사체를 초기화합니다.</summary>
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

        /// <summary>마우스 위치를 월드 XZ 방향으로 변환해 현재 조준 방향에 저장합니다.</summary>
        private void UpdateAimDirection()
        {
            if (aimResolver.TryResolvePointerDirection(transform, aimSurfaceMask, out Vector3 direction))
                aimDirection = direction.normalized;
        }
    }
}
