using System.Collections;
using FlatVenture.NUH.Player;
using FlatVenture.NUH.Player.Skills;
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
    /// <summary>전사, 궁수, 마법사 액티브 스킬이 공통 실행 상태 규칙을 지키는지 검증합니다.</summary>
    public sealed class ActiveSkillCommonIntegrationTests
    {
        private const string WarriorScenePath =
            "Assets/_Scenes/NUH/Test_09_PlayerValidation.unity";
        private const string ArcherScenePath =
            "Assets/_Scenes/NUH/Test_11_ArcherActiveSkill.unity";
        private const string MageScenePath =
            "Assets/_Scenes/NUH/Test_13_MageActiveSkill.unity";

        /// <summary>각 테스트 뒤 EditMode로 돌아와 다음 테스트가 독립된 PlayMode 상태에서 시작되게 합니다.</summary>
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (EditorApplication.isPlaying)
                yield return new ExitPlayMode();
        }

        // 전사 검기 스킬이 공통 상태 인터페이스를 통해 쿨타임 무시와 재시작 초기화를 제공하는지 확인합니다.
        [UnityTest]
        public IEnumerator WarriorActiveSkill_ExposesCommonStatusAndResets()
        {
            EditorSceneManager.OpenScene(WarriorScenePath, OpenSceneMode.Single);
            yield return new EnterPlayMode();
            yield return null;

            PlayerController player = Object.FindFirstObjectByType<PlayerController>();
            WarriorActiveSkillController skill = Object.FindFirstObjectByType<WarriorActiveSkillController>();

            yield return AssertCommonStatusAndReset(player, skill, 6);
        }

        // 궁수 관통 사격 스킬이 공통 상태 인터페이스를 통해 쿨타임 무시와 재시작 초기화를 제공하는지 확인합니다.
        [UnityTest]
        public IEnumerator ArcherActiveSkill_ExposesCommonStatusAndResets()
        {
            EditorSceneManager.OpenScene(ArcherScenePath, OpenSceneMode.Single);
            yield return new EnterPlayMode();
            yield return null;

            PlayerController player = Object.FindFirstObjectByType<PlayerController>();
            ArcherActiveSkillController skill = Object.FindFirstObjectByType<ArcherActiveSkillController>();

            yield return AssertCommonStatusAndReset(player, skill, 4);
        }

        // 마법사 부채꼴 파이어볼 스킬이 공통 상태 인터페이스를 통해 쿨타임 무시와 재시작 초기화를 제공하는지 확인합니다.
        [UnityTest]
        public IEnumerator MageActiveSkill_ExposesCommonStatusAndResets()
        {
            EditorSceneManager.OpenScene(MageScenePath, OpenSceneMode.Single);
            yield return new EnterPlayMode();
            yield return null;

            PlayerController player = Object.FindFirstObjectByType<PlayerController>();
            MageActiveSkillController skill = Object.FindFirstObjectByType<MageActiveSkillController>();

            yield return AssertCommonStatusAndReset(player, skill, 10);
        }

        /// <summary>액티브 스킬 공통 상태의 초기값, 테스트 쿨타임 토글, 플레이어 재시작 초기화를 검증합니다.</summary>
        private static IEnumerator AssertCommonStatusAndReset(
            PlayerController player,
            IPlayerActiveSkillStatus skill,
            int expectedPrewarmCount)
        {
            Assert.That(player, Is.Not.Null);
            Assert.That(skill, Is.Not.Null);
            Assert.That(skill.CooldownRemaining, Is.EqualTo(0f).Within(0.001f));
            Assert.That(skill.IsAiming, Is.False);
            Assert.That(skill.IsCasting, Is.False);
            Assert.That(skill.IgnoreCooldown, Is.False);
            Assert.That(skill.PoolActiveCount, Is.EqualTo(0));
            Assert.That(skill.PoolInactiveCount, Is.EqualTo(expectedPrewarmCount));
            Assert.That(skill.PoolTotalCount, Is.EqualTo(expectedPrewarmCount));

            skill.ToggleIgnoreCooldown();

            Assert.That(skill.IgnoreCooldown, Is.True);

            player.ResetPlayer();
            yield return null;

            Assert.That(skill.CooldownRemaining, Is.EqualTo(0f).Within(0.001f));
            Assert.That(skill.IsAiming, Is.False);
            Assert.That(skill.IsCasting, Is.False);
            Assert.That(skill.IgnoreCooldown, Is.False);
            Assert.That(skill.PoolActiveCount, Is.EqualTo(0));
        }
    }
}
