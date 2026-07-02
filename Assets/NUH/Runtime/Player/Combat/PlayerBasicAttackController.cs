using System;
using System.Collections.Generic;
using FlatVenture.NUH.Player.Input;
using UnityEngine;

namespace FlatVenture.NUH.Player.Combat
{
    /// <summary>
    /// 가장 가까운 대상을 자동 조준하거나 마우스 방향으로 수동 조준하여 전방 근접 공격을 실행합니다.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(PlayerInputReader))]
    public sealed class PlayerBasicAttackController : MonoBehaviour
    {
        [SerializeField] private LayerMask targetMask = ~0;
        [SerializeField] private LayerMask aimSurfaceMask = ~0;

        private readonly Collider[] targetResults = new Collider[32];
        private readonly HashSet<IPlayerAttackTarget> damagedTargets = new HashSet<IPlayerAttackTarget>();
        private PlayerController player;
        private PlayerInputReader inputReader;
        private Camera mainCamera;
        private IPlayerAttackTarget currentTarget;
        private float cooldownRemaining;
        private Vector3 aimDirection = Vector3.forward;

        public Vector3 AimDirection => aimDirection;
        public float CooldownRemaining => cooldownRemaining;
        public bool IsManualAim => inputReader != null && inputReader.IsManualAimHeld;
        public string CurrentTargetName => currentTarget?.TargetTransform != null
            ? currentTarget.TargetTransform.name
            : "None";

        public event Action<Vector3, bool, int> AttackPerformed;

        private void Awake()
        {
            player = GetComponent<PlayerController>();
            inputReader = GetComponent<PlayerInputReader>();
            mainCamera = Camera.main;
        }

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

        private void UpdateManualAimDirection()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;

            if (mainCamera == null)
                return;

            Ray ray = mainCamera.ScreenPointToRay(inputReader.PointerScreenPosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, aimSurfaceMask, QueryTriggerInteraction.Ignore))
            {
                SetAimDirection(hit.point - transform.position);
                return;
            }

            Plane playerPlane = new Plane(Vector3.up, transform.position);
            if (playerPlane.Raycast(ray, out float distance))
                SetAimDirection(ray.GetPoint(distance) - transform.position);
        }

        private void SetAimDirection(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f)
                aimDirection = direction.normalized;
        }

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

        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying || player?.RuntimeState == null)
                return;

            Gizmos.color = IsManualAim ? Color.cyan : Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position + (aimDirection * player.RuntimeState.BasicAttackRange));
        }
    }
}
