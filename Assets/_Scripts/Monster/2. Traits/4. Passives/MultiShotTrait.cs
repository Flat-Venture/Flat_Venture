using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MultiShotTrait : MonsterTrait
{
    [Header("Attack Settings")]
    public float attackRange = 10f;
    public float attackCooldown = 3f;
    public float projectileDamage = 10f;
    public float preAttackDelay = 0.5f;
    public float postAttackDelay = 0.5f;

    [Header("Multi Shot Settings")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float projectileSpeed = 10f;
    public int projectileCount = 3;
    public float spreadAngle = 45f;

    private float nextFireTime = 0f;

    public override void Setup(MonsterController controller)
    {
        base.Setup(controller);
        nextFireTime = Time.time + attackCooldown;
    }

    private void Update()
    {
        //기본 생존 및 타겟 존재여부 확인
        if (controller != null && controller.IsAlive && !controller.IsStunned && controller.CurrentTarget != null)
        {
            if (controller.IsAttacking) return;

            float distanceToTarget = Vector3.Distance(transform.position, controller.CurrentTarget.position);

            //조건이 맞으면 다발사격 코루틴 실행
            if (distanceToTarget <= attackRange && Time.time >= nextFireTime)
            {
                StartCoroutine(MultiShotSequence());
            }
        }
    }

    private IEnumerator MultiShotSequence()
    {
        controller.IsAttacking = true;

        //발사 전 대기
        yield return new WaitForSeconds(preAttackDelay);

        if (controller == null || !controller.IsAlive || controller.IsStunned)
        {
            if (controller != null) controller.IsAttacking = false;
            yield break;
        }

        ShootMultipleProjectiles();

        //발사 후 대기
        yield return new WaitForSeconds(postAttackDelay);

        if (controller != null) controller.IsAttacking = false;
        nextFireTime = Time.time + attackCooldown;
    }

    private void ShootMultipleProjectiles()
    {
        if (projectilePrefab == null) return;

        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position + (Vector3.up * 1f);
        Vector3 baseDirection = (controller.CurrentTarget.position - spawnPos).normalized;
        baseDirection.y = 0;

        //설정된 각도와 개수만큼 나눠서 투사체를 발사함
        float angleStep = spreadAngle / (projectileCount - 1);
        float startingAngle = -(spreadAngle / 2f);

        for (int i = 0; i < projectileCount; i++)
        {
            float currentAngle = startingAngle + (angleStep * i);
            Quaternion rotation = Quaternion.Euler(0, currentAngle, 0);
            Vector3 fireDirection = rotation * baseDirection;

            GameObject bullet = Instantiate(projectilePrefab, spawnPos, Quaternion.LookRotation(fireDirection));
            
            Rigidbody rb = bullet.GetComponent<Rigidbody>();
            if (rb != null) rb.linearVelocity = fireDirection * projectileSpeed;

            EnemyProjectile projScript = bullet.GetComponent<EnemyProjectile>();
            if (projScript != null) projScript.SetupDamage(projectileDamage);
        }
    }
}