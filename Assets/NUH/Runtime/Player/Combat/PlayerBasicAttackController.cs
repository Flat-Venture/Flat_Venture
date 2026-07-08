using System;
using System.Collections.Generic;
using FlatVenture.NUH.Player.Aiming;
using FlatVenture.NUH.Player.Input;
using UnityEngine;

namespace FlatVenture.NUH.Player.Combat
{
    /// <summary>
    /// 가장 가까운 대상을 자동 조준하거나 마우스 방향으로 수동 조준하여 전방 근접 공격을 실행합니다.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(PlayerInputReader))]
    [RequireComponent(typeof(PlayerAimResolver))]
    public sealed class PlayerBasicAttackController : MonoBehaviour
    {
        // 공격 대상으로 검색할 Collider 레이어와 마우스 조준 Ray가 사용할 표면 레이어입니다.
        [SerializeField] private LayerMask targetMask = ~0;
        [SerializeField] private LayerMask aimSurfaceMask = ~0;

        // 매 공격마다 배열을 만들지 않도록 재사용하는 NonAlloc 물리 검색 버퍼입니다.
        private readonly Collider[] targetResults = new Collider[32];
        // 한 번의 공격 판정에서 Collider가 여러 개인 같은 대상을 중복 타격하지 않게 합니다.
        private readonly HashSet<IPlayerAttackTarget> damagedTargets = new HashSet<IPlayerAttackTarget>();
        // 같은 플레이어 오브젝트의 상태·입력·조준 모듈입니다.
        private PlayerController player;
        private PlayerInputReader inputReader;
        private PlayerAimResolver aimResolver;
        // 자동 조준이 이번 프레임에 선택한 가장 가까운 살아 있는 대상입니다.
        private IPlayerAttackTarget currentTarget;
        // 0이 되면 다음 기본 공격을 실행할 수 있습니다.
        private float cooldownRemaining;
        // 실제 공격 판정과 캐릭터 좌우 표현이 공유하는 XZ 조준 방향입니다.
        private Vector3 aimDirection = Vector3.forward;

        public Vector3 AimDirection { get { return aimDirection; } }
        public float CooldownRemaining { get { return cooldownRemaining; } }
        public bool IsManualAim { get { return inputReader != null && inputReader.IsManualAimHeld; } }
        public string CurrentTargetName
        {
            get
            {
                return currentTarget?.TargetTransform != null ? currentTarget.TargetTransform.name : "None";
            }
        }

        // 디버그 뷰가 공격 방향·조준 방식·적중 수를 표시할 때 구독합니다.
        public event Action<Vector3, bool, int> AttackPerformed;

        /// <summary>액티브 스킬 사용 후 기본 공격에 최소 한 공격 주기의 후딜레이를 적용합니다.</summary>
        public void ApplyPostSkillCooldown()
        {
            if (player?.RuntimeState == null)
                return;

            float attackInterval = 1f / Mathf.Max(0.01f, player.RuntimeState.AttacksPerSecond);
            cooldownRemaining = Mathf.Max(cooldownRemaining, attackInterval);
        }

        /// <summary>같은 오브젝트의 모듈 참조를 캐시합니다.</summary>
        private void Awake()
        {
            player = GetComponent<PlayerController>();
            inputReader = GetComponent<PlayerInputReader>();
            aimResolver = GetComponent<PlayerAimResolver>();
        }

        /// <summary>플레이어 재시작 시 공격 상태도 초기화되도록 이벤트를 구독합니다.</summary>
        private void Start()
        {
            player.RuntimeState.ResetCompleted += ResetAttack;
        }

        /// <summary>오브젝트 제거 시 초기화 이벤트 구독을 해제합니다.</summary>
        private void OnDestroy()
        {
            if (player?.RuntimeState != null)
                player.RuntimeState.ResetCompleted -= ResetAttack;
        }

        /// <summary>
        /// 매 프레임 쿨타임을 줄이고 수동 또는 자동 조준을 선택한 뒤 공격 가능하면 공격합니다.
        /// 자동 조준은 대상이 없으면 멈추고, 수동 조준은 빈 공간에도 공격합니다.
        /// </summary>
        private void Update()
        {
            if (player.RuntimeState == null || player.RuntimeState.IsDead)
                return;

            cooldownRemaining = Mathf.Max(0f, cooldownRemaining - Time.deltaTime);

            bool manualAim = inputReader.IsManualAimHeld;
            if (manualAim)
            {
                UpdateManualAimDirection();
                currentTarget = null;
            }
            else
            {
                currentTarget = FindNearestTarget();
                if (currentTarget != null)
                    SetAimDirection(currentTarget.TargetTransform.position - transform.position);
            }

            if (cooldownRemaining > 0f || (!manualAim && currentTarget == null))
                return;

            PerformAttack(manualAim);
        }

        /// <summary>공격 범위 안의 Collider 중 가장 가까운 살아 있는 공격 대상을 찾습니다.</summary>
        private IPlayerAttackTarget FindNearestTarget()
        {
            float range = player.RuntimeState.BasicAttackRange;
            int count = Physics.OverlapSphereNonAlloc(
                transform.position,
                range,
                targetResults,
                targetMask,
                QueryTriggerInteraction.Collide);

            IPlayerAttackTarget nearest = null;
            float nearestSqrDistance = float.PositiveInfinity;

            for (int i = 0; i < count; i++)
            {
                Collider candidateCollider = targetResults[i];
                if (candidateCollider == null)
                    continue;

                IPlayerAttackTarget candidate = candidateCollider.GetComponentInParent<IPlayerAttackTarget>();
                if (candidate == null || !candidate.IsAlive)
                    continue;

                Vector3 offset = candidate.TargetTransform.position - transform.position;
                offset.y = 0f;
                float sqrDistance = offset.sqrMagnitude;
                if (sqrDistance >= nearestSqrDistance)
                    continue;

                nearest = candidate;
                nearestSqrDistance = sqrDistance;
            }

            return nearest;
        }

        /// <summary>현재 마우스 좌표를 XZ 방향으로 변환해 수동 조준 방향을 갱신합니다.</summary>
        private void UpdateManualAimDirection()
        {
            if (aimResolver.TryResolvePointerDirection(transform, aimSurfaceMask, out Vector3 direction))
                aimDirection = direction;
        }

        /// <summary>높이 성분을 제거하고 길이 1인 유효한 조준 방향만 저장합니다.</summary>
        private void SetAimDirection(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f)
                aimDirection = direction.normalized;
        }

        /// <summary>
        /// 플레이어 앞쪽에 회전된 Box 판정을 만들고 각 대상을 한 번씩 타격합니다.
        /// 실행 후 공격속도로 다음 공격까지의 쿨타임을 계산합니다.
        /// </summary>
        private void PerformAttack(bool manualAim)
        {
            float range = player.RuntimeState.BasicAttackRange;
            float width = player.RuntimeState.BasicAttackWidth;
            Vector3 center = transform.position + (aimDirection * (range * 0.5f));
            Vector3 halfExtents = new Vector3(width * 0.5f, 1f, range * 0.5f);
            Quaternion rotation = Quaternion.LookRotation(aimDirection, Vector3.up);

            int count = Physics.OverlapBoxNonAlloc(
                center,
                halfExtents,
                targetResults,
                rotation,
                targetMask,
                QueryTriggerInteraction.Collide);

            damagedTargets.Clear();
            for (int i = 0; i < count; i++)
            {
                Collider hitCollider = targetResults[i];
                if (hitCollider == null)
                    continue;

                IPlayerAttackTarget target = hitCollider.GetComponentInParent<IPlayerAttackTarget>();
                if (target == null || !target.IsAlive || !damagedTargets.Add(target))
                    continue;

                target.TakeDamage(player.RuntimeState.AttackPower);
            }

            cooldownRemaining = 1f / Mathf.Max(0.01f, player.RuntimeState.AttacksPerSecond);
            AttackPerformed?.Invoke(aimDirection, manualAim, damagedTargets.Count);
        }

        /// <summary>재시작 시 타깃·쿨타임·방향·중복 타격 목록을 초기 상태로 돌립니다.</summary>
        private void ResetAttack()
        {
            currentTarget = null;
            cooldownRemaining = 0f;
            aimDirection = Vector3.forward;
            damagedTargets.Clear();
        }

        /// <summary>선택된 오브젝트에서 현재 공격 방향을 Scene Gizmo로 표시합니다.</summary>
        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying || player?.RuntimeState == null)
                return;

            Gizmos.color = IsManualAim ? Color.cyan : Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position + (aimDirection * player.RuntimeState.BasicAttackRange));
        }
    }
}
