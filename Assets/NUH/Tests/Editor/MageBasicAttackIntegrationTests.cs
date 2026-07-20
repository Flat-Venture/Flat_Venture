using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>마법사 기본 공격 테스트 씬에서 원거리 투사체와 스플래시 피해가 함께 동작하는지 검증합니다.</summary>
public sealed class MageBasicAttackIntegrationTests
{
    private const string MageScenePath =
        "Assets/_Scenes/NUH/Test_12_MageBasicAttack.unity";

    /// <summary>테스트 뒤 EditMode로 돌아와 다른 테스트와 플레이 상태를 공유하지 않게 합니다.</summary>
    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (EditorApplication.isPlaying)
            yield return new ExitPlayMode();
    }

    // 마법탄이 직접 대상에게 100%, 폭발 반경 안의 주변 대상에게 50% 피해를 주는지 확인합니다.
    [UnityTest]
    public IEnumerator MageScene_ProjectileHitAppliesDirectAndSplashDamage()
    {
        EditorSceneManager.OpenScene(MageScenePath, OpenSceneMode.Single);
        yield return new EnterPlayMode();
        yield return null;

        PlayerController player = Object.FindFirstObjectByType<PlayerController>();
        PlayerBasicAttackController basicAttack =
            Object.FindFirstObjectByType<PlayerBasicAttackController>();
        PlayerTargetDummy direct = GameObject.Find("MageDummy_01_DirectHit").GetComponent<PlayerTargetDummy>();
        PlayerTargetDummy splashLeft = GameObject.Find("MageDummy_02_SplashLeft").GetComponent<PlayerTargetDummy>();
        PlayerTargetDummy splashBack = GameObject.Find("MageDummy_03_SplashBack").GetComponent<PlayerTargetDummy>();
        PlayerTargetDummy outside = GameObject.Find("MageDummy_04_OutsideSplash").GetComponent<PlayerTargetDummy>();

        float timeout = Time.time + 2f;
        while (Mathf.Approximately(direct.CurrentHealth, 30f) && Time.time < timeout)
            yield return null;

        Assert.That(player, Is.Not.Null);
        Assert.That(basicAttack, Is.Not.Null);
        Assert.That(player.RuntimeState.BasicAttackType, Is.EqualTo(BasicAttackType.Projectile));
        Assert.That(player.RuntimeState.AttackPower, Is.EqualTo(12f));
        Assert.That(player.RuntimeState.BasicAttackRange, Is.EqualTo(9f));
        Assert.That(player.RuntimeState.BasicAttackProjectileSpeed, Is.EqualTo(14f));
        string damageState =
            $"direct={direct.CurrentHealth}, left={splashLeft.CurrentHealth}, " +
            $"back={splashBack.CurrentHealth}, outside={outside.CurrentHealth}, " +
            $"pool={basicAttack.PoolActiveCount}/{basicAttack.PoolInactiveCount}/{basicAttack.PoolTotalCount}";
        Assert.That(direct.CurrentHealth, Is.EqualTo(18f).Within(0.001f), damageState);
        Assert.That(splashLeft.CurrentHealth, Is.EqualTo(24f).Within(0.001f));
        Assert.That(splashBack.CurrentHealth, Is.EqualTo(24f).Within(0.001f));
        Assert.That(outside.CurrentHealth, Is.EqualTo(30f).Within(0.001f));
        Assert.That(basicAttack.PoolActiveCount, Is.EqualTo(0));
        Assert.That(basicAttack.PoolInactiveCount, Is.EqualTo(8));
        Assert.That(basicAttack.PoolTotalCount, Is.EqualTo(8));
    }
}
