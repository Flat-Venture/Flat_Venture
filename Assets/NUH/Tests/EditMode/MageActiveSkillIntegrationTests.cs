using System.Collections;
using FlatVenture.NUH.Player;
using FlatVenture.NUH.Player.Combat;
using FlatVenture.NUH.Player.Skills.Mage;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

namespace FlatVenture.NUH.Tests.EditMode
{
    /// <summary>마법사 액티브 스킬 테스트 씬의 SO 수치, 프리팹 연결, 풀 준비 상태를 검증합니다.</summary>
    public sealed class MageActiveSkillIntegrationTests
    {
        private const string MageActiveScenePath =
            "Assets/_Scenes/NUH/Test_13_MageActiveSkill.unity";

        /// <summary>테스트 뒤 EditMode로 돌아와 다른 테스트와 플레이 상태를 공유하지 않게 합니다.</summary>
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (EditorApplication.isPlaying)
                yield return new ExitPlayMode();
        }

        // 마법사 액티브 스킬이 5발·60도 부채꼴·스플래시 파이어볼 수치로 준비되는지 확인합니다.
        [UnityTest]
        public IEnumerator MageActiveScene_LoadsFireballFanStatsAndPrewarmedPool()
        {
            EditorSceneManager.OpenScene(MageActiveScenePath, OpenSceneMode.Single);
            yield return new EnterPlayMode();
            yield return null;

            PlayerController player = Object.FindFirstObjectByType<PlayerController>();
            PlayerBasicAttackController basicAttack = Object.FindFirstObjectByType<PlayerBasicAttackController>();
            MageActiveSkillController skill = Object.FindFirstObjectByType<MageActiveSkillController>();

            Assert.That(player, Is.Not.Null);
            Assert.That(basicAttack, Is.Not.Null);
            Assert.That(skill, Is.Not.Null);
            Assert.That(player.RuntimeState.BasicAttackType, Is.EqualTo(BasicAttackType.Projectile));
            Assert.That(player.RuntimeState.ActiveSkillDamage, Is.EqualTo(18f));
            Assert.That(player.RuntimeState.ActiveSkillProjectileCount, Is.EqualTo(5));
            Assert.That(player.RuntimeState.ActiveSkillProjectileWidth, Is.EqualTo(0.55f));
            Assert.That(player.RuntimeState.ActiveSkillProjectileSpeed, Is.EqualTo(16f));
            Assert.That(player.RuntimeState.ActiveSkillRange, Is.EqualTo(12f));
            Assert.That(skill.TotalSpreadAngle, Is.EqualTo(60f));
            Assert.That(skill.PoolActiveCount, Is.EqualTo(0));
            Assert.That(skill.PoolInactiveCount, Is.EqualTo(10));
            Assert.That(skill.PoolTotalCount, Is.EqualTo(10));
        }
    }
}
