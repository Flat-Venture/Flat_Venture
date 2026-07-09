using System.Collections;
using FlatVenture.NUH.Player;
using FlatVenture.NUH.Player.Combat;
using FlatVenture.NUH.Player.Debugging;
using FlatVenture.NUH.Player.Skills.Archer;
using FlatVenture.NUH.Player.Skills.Mage;
using FlatVenture.NUH.Player.Skills.Warrior;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

namespace FlatVenture.NUH.Tests.EditMode
{
    /// <summary>직업 변경 전투 디버그 씬에서 세 직업 전투 모듈이 버튼 메서드로 전환되는지 검증합니다.</summary>
    public sealed class JobSwitchCombatDebugIntegrationTests
    {
        private const string JobSwitchScenePath =
            "Assets/_Scenes/NUH/Test_14_JobSwitchCombatDebug.unity";

        /// <summary>각 테스트 뒤 EditMode로 돌아와 다른 테스트와 PlayMode 상태를 공유하지 않게 합니다.</summary>
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (EditorApplication.isPlaying)
                yield return new ExitPlayMode();
        }

        // 씬 시작 시 전사 상태로 준비되고, 버튼 메서드 호출로 궁수·마법사·전사 전환이 즉시 반영되는지 확인합니다.
        [UnityTest]
        public IEnumerator JobSwitchScene_SwitchesStatsBasicAttackAndActiveSkillControllers()
        {
            EditorSceneManager.OpenScene(JobSwitchScenePath, OpenSceneMode.Single);
            yield return new EnterPlayMode();
            yield return null;

            PlayerController player = Object.FindFirstObjectByType<PlayerController>();
            PlayerBasicAttackController basicAttack = Object.FindFirstObjectByType<PlayerBasicAttackController>();
            WarriorActiveSkillController warriorSkill = Object.FindFirstObjectByType<WarriorActiveSkillController>();
            ArcherActiveSkillController archerSkill = Object.FindFirstObjectByType<ArcherActiveSkillController>(FindObjectsInactive.Include);
            MageActiveSkillController mageSkill = Object.FindFirstObjectByType<MageActiveSkillController>(FindObjectsInactive.Include);
            PlayerJobSwitchDebugPanel panel = Object.FindFirstObjectByType<PlayerJobSwitchDebugPanel>();

            Assert.That(player, Is.Not.Null);
            Assert.That(basicAttack, Is.Not.Null);
            Assert.That(warriorSkill, Is.Not.Null);
            Assert.That(archerSkill, Is.Not.Null);
            Assert.That(mageSkill, Is.Not.Null);
            Assert.That(panel, Is.Not.Null);

            AssertCurrentJob(player, basicAttack, warriorSkill, archerSkill, mageSkill, "Warrior", BasicAttackType.Melee, 0, true, false, false);

            panel.SwitchToArcher();
            yield return null;

            AssertCurrentJob(player, basicAttack, warriorSkill, archerSkill, mageSkill, "Archer", BasicAttackType.Projectile, 8, false, true, false);

            panel.ToggleCurrentSkillNoCooldown();
            Assert.That(archerSkill.IgnoreCooldown, Is.True);

            panel.SwitchToMage();
            yield return null;

            Assert.That(archerSkill.IgnoreCooldown, Is.False);
            AssertCurrentJob(player, basicAttack, warriorSkill, archerSkill, mageSkill, "Mage", BasicAttackType.Projectile, 8, false, false, true);

            panel.SwitchToWarrior();
            yield return null;

            AssertCurrentJob(player, basicAttack, warriorSkill, archerSkill, mageSkill, "Warrior", BasicAttackType.Melee, 0, true, false, false);
        }

        /// <summary>현재 직업의 SO, 기본 공격 타입, 기본 공격 풀, 액티브 컨트롤러 활성 상태를 검증합니다.</summary>
        private static void AssertCurrentJob(
            PlayerController player,
            PlayerBasicAttackController basicAttack,
            WarriorActiveSkillController warriorSkill,
            ArcherActiveSkillController archerSkill,
            MageActiveSkillController mageSkill,
            string expectedJobId,
            BasicAttackType expectedBasicAttackType,
            int expectedBasicPoolCount,
            bool expectedWarriorEnabled,
            bool expectedArcherEnabled,
            bool expectedMageEnabled)
        {
            Assert.That(player.RuntimeState.Source.JobId, Is.EqualTo(expectedJobId));
            Assert.That(player.RuntimeState.BasicAttackType, Is.EqualTo(expectedBasicAttackType));
            Assert.That(basicAttack.PoolTotalCount, Is.EqualTo(expectedBasicPoolCount));
            Assert.That(warriorSkill.enabled, Is.EqualTo(expectedWarriorEnabled));
            Assert.That(archerSkill.enabled, Is.EqualTo(expectedArcherEnabled));
            Assert.That(mageSkill.enabled, Is.EqualTo(expectedMageEnabled));
        }
    }
}
