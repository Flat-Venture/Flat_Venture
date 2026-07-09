using System.Collections;
using FlatVenture.NUH.Player;
using FlatVenture.NUH.Player.Combat;
using FlatVenture.NUH.Player.Skills.Archer;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

namespace FlatVenture.NUH.Tests.EditMode
{
    /// <summary>궁수 액티브 스킬 테스트 씬의 SO 수치, 프리팹 연결, 오브젝트 풀 준비 상태를 검증합니다.</summary>
    public sealed class ArcherActiveSkillIntegrationTests
    {
        private const string ArcherActiveScenePath =
            "Assets/_Scenes/NUH/Test_11_ArcherActiveSkill.unity";

        /// <summary>테스트 뒤 EditMode로 돌아와 다른 테스트와 플레이 상태를 공유하지 않게 합니다.</summary>
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (EditorApplication.isPlaying)
                yield return new ExitPlayMode();
        }

        // 관통 사격이 궁수 SO의 액티브 수치를 사용하고 풀을 예열하는지 확인합니다.
        [UnityTest]
        public IEnumerator ArcherActiveScene_LoadsPiercingShotStatsAndPrewarmedPool()
        {
            EditorSceneManager.OpenScene(ArcherActiveScenePath, OpenSceneMode.Single);
            yield return new EnterPlayMode();
            yield return null;

            PlayerController player = Object.FindFirstObjectByType<PlayerController>();
            PlayerBasicAttackController basicAttack = Object.FindFirstObjectByType<PlayerBasicAttackController>();
            ArcherActiveSkillController skill = Object.FindFirstObjectByType<ArcherActiveSkillController>();

            Assert.That(player, Is.Not.Null);
            Assert.That(basicAttack, Is.Not.Null);
            Assert.That(skill, Is.Not.Null);
            Assert.That(player.RuntimeState.BasicAttackType, Is.EqualTo(BasicAttackType.Projectile));
            Assert.That(player.RuntimeState.ActiveSkillDamage, Is.EqualTo(70f));
            Assert.That(player.RuntimeState.ActiveSkillProjectileCount, Is.EqualTo(1));
            Assert.That(player.RuntimeState.ActiveSkillProjectileWidth, Is.EqualTo(1f));
            Assert.That(player.RuntimeState.ActiveSkillProjectileSpeed, Is.EqualTo(30f));
            Assert.That(player.RuntimeState.ActiveSkillRange, Is.EqualTo(20f));
            Assert.That(player.RuntimeState.ActiveSkillDamage, Is.GreaterThan(20f * 3f));
            Assert.That(skill.PoolActiveCount, Is.EqualTo(0));
            Assert.That(skill.PoolInactiveCount, Is.EqualTo(4));
            Assert.That(skill.PoolTotalCount, Is.EqualTo(4));
        }
    }
}
