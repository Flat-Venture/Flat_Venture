using System.Collections;
using FlatVenture.NUH.Player;
using FlatVenture.NUH.Player.Combat;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace FlatVenture.NUH.Tests.EditMode
{
    /// <summary>궁수 테스트 씬의 SO·프리팹·오브젝트 풀 연결을 실제 PlayMode 진입으로 검증합니다.</summary>
    public sealed class ArcherBasicAttackIntegrationTests
    {
        private const string ArcherScenePath =
            "Assets/_Scenes/NUH/Test_10_ArcherBasicAttack.unity";

        /// <summary>테스트 뒤 EditMode로 돌아와 다른 테스트와 플레이 상태를 공유하지 않게 합니다.</summary>
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (EditorApplication.isPlaying)
                yield return new ExitPlayMode();
        }

        // 궁수 SO가 원거리 방식으로 복사되고 예열된 화살 하나가 가까운 적에게 자동 발사되는지 확인합니다.
        [UnityTest]
        public IEnumerator ArcherScene_LoadsProjectileStatsAndPrewarmedPool()
        {
            EditorSceneManager.OpenScene(ArcherScenePath, OpenSceneMode.Single);
            yield return new EnterPlayMode();
            yield return null;

            PlayerController player = Object.FindFirstObjectByType<PlayerController>();
            PlayerBasicAttackController basicAttack =
                Object.FindFirstObjectByType<PlayerBasicAttackController>();

            Assert.That(player, Is.Not.Null);
            Assert.That(basicAttack, Is.Not.Null);
            Assert.That(player.RuntimeState.BasicAttackType, Is.EqualTo(BasicAttackType.Projectile));
            Assert.That(player.RuntimeState.BasicAttackRange, Is.EqualTo(8f));
            Assert.That(player.RuntimeState.BasicAttackProjectileSpeed, Is.EqualTo(18f));
            Assert.That(basicAttack.PoolActiveCount, Is.EqualTo(1));
            Assert.That(basicAttack.PoolInactiveCount, Is.EqualTo(7));
            Assert.That(basicAttack.PoolTotalCount, Is.EqualTo(8));
        }
    }
}
