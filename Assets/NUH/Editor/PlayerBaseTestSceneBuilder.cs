using System.IO;
using FlatVenture.NUH.Player;
using FlatVenture.NUH.Player.Combat;
using FlatVenture.NUH.Player.Aiming;
using FlatVenture.NUH.Player.Data;
using FlatVenture.NUH.Player.Debugging;
using FlatVenture.NUH.Player.Health;
using FlatVenture.NUH.Player.Input;
using FlatVenture.NUH.Player.Movement;
using FlatVenture.NUH.Player.Skills.Archer;
using FlatVenture.NUH.Player.Skills.Mage;
using FlatVenture.NUH.Player.Skills.Warrior;
using FlatVenture.NUH.Seed.Debugging;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FlatVenture.NUH.Editor
{
    /// <summary>
    /// 의현 담당 1단계 테스트 에셋과 씬을 재현 가능하게 생성합니다.
    /// </summary>
    public static class PlayerBaseTestSceneBuilder
    {
        // 테스트 씬을 단계별로 복사·확장할 때 사용하는 고정 에셋 경로입니다.
        // 문자열을 한곳에 모아 씬 이름이나 폴더 변경 시 누락을 막습니다.
        private const string SceneDirectory = "Assets/_Scenes/NUH";
        private const string ScenePath = SceneDirectory + "/Test_01_PlayerBase.unity";
        private const string Stage02ScenePath = SceneDirectory + "/Test_02_MovementCamera.unity";
        private const string Stage03ScenePath = SceneDirectory + "/Test_03_Dash.unity";
        private const string Stage04ScenePath = SceneDirectory + "/Test_04_HealthDeath.unity";
        private const string Stage05ScenePath = SceneDirectory + "/Test_05_AimBasicAttack.unity";
        private const string Stage06ScenePath = SceneDirectory + "/Test_06_SwordWave.unity";
        private const string Stage07ScenePath = SceneDirectory + "/Test_07_IntegratedDebugUI.unity";
        private const string Stage08ScenePath = SceneDirectory + "/Test_08_SeedDebug.unity";
        private const string Stage09ScenePath = SceneDirectory + "/Test_09_PlayerValidation.unity";
        private const string Stage10ScenePath = SceneDirectory + "/Test_10_ArcherBasicAttack.unity";
        private const string Stage11ScenePath = SceneDirectory + "/Test_11_ArcherActiveSkill.unity";
        private const string Stage12ScenePath = SceneDirectory + "/Test_12_MageBasicAttack.unity";
        private const string Stage13ScenePath = SceneDirectory + "/Test_13_MageActiveSkill.unity";
        private const string Stage14ScenePath = SceneDirectory + "/Test_14_JobSwitchCombatDebug.unity";
        private const string SkillPrefabDirectory = "Assets/NUH/Prefabs/Player/Skills";
        private const string BasicAttackPrefabDirectory = "Assets/NUH/Prefabs/Player/BasicAttacks";
        private const string SwordWavePrefabPath = SkillPrefabDirectory + "/Pfb_WarriorSwordWave.prefab";
        private const string ArcherPiercingShotPrefabPath = SkillPrefabDirectory + "/Pfb_ArcherPiercingShot.prefab";
        private const string ArcherArrowPrefabPath = BasicAttackPrefabDirectory + "/Pfb_ArcherArrow.prefab";
        private const string MageOrbPrefabPath = BasicAttackPrefabDirectory + "/Pfb_MageOrb.prefab";
        private const string MageFireballPrefabPath = SkillPrefabDirectory + "/Pfb_MageFireball.prefab";
        private const string TestMaterialDirectory = "Assets/NUH/Art/Test";
        private const string SwordWaveMaterialPath = TestMaterialDirectory + "/Mat_SwordWave_Test.asset";
        private const string ArcherPiercingShotMaterialPath = TestMaterialDirectory + "/Mat_ArcherPiercingShot_Test.asset";
        private const string ArcherArrowMaterialPath = TestMaterialDirectory + "/Mat_ArcherArrow_Test.asset";
        private const string MageOrbMaterialPath = TestMaterialDirectory + "/Mat_MageOrb_Test.asset";
        private const string MageFireballMaterialPath = TestMaterialDirectory + "/Mat_MageFireball_Test.asset";
        private const string DataDirectory = "Assets/NUH/Data/Player";
        private const string StatsPath = DataDirectory + "/PlayerStats_Warrior.asset";
        private const string ArcherStatsPath = DataDirectory + "/PlayerStats_Archer.asset";
        private const string MageStatsPath = DataDirectory + "/PlayerStats_Mage.asset";
        private const string InputActionsPath = "Assets/InputSystem_Actions.inputactions";

        /// <summary>Unity 메뉴에서 1~14단계 테스트 씬 전체 재생성을 실행합니다.</summary>
        [MenuItem("Flat Venture/NUH/전체 테스트 씬 다시 생성")]
        public static void RebuildAllStagesFromMenu()
        {
            RebuildAllStages();
            EditorUtility.DisplayDialog("Flat Venture", "1~14단계 테스트 씬을 모두 다시 생성했습니다.", "확인");
        }

        /// <summary>
        /// 앞 단계 결과를 다음 단계가 기반으로 사용하도록 1단계부터 순서대로 생성합니다.
        /// 테스트 씬 직렬화 참조가 깨졌을 때도 이 함수 하나로 복구할 수 있습니다.
        /// </summary>
        public static void RebuildAllStages()
        {
            Build();
            BuildStage02();
            BuildStage03();
            BuildStage04();
            BuildStage05();
            BuildStage06();
            BuildStage07();
            BuildStage08();
            BuildStage09();
            BuildStage10();
            BuildStage11();
            BuildStage12();
            BuildStage13();
            BuildStage14();
            Debug.Log("[NUH] 1~14단계 테스트 씬 전체 재생성 완료");
        }

        [MenuItem("Flat Venture/NUH/1단계 테스트 씬 생성")]
        public static void BuildFromMenu()
        {
            Build();
            EditorUtility.DisplayDialog("Flat Venture", "Test_01_PlayerBase 씬 생성을 완료했습니다.", "확인");
        }

        /// <summary>1단계 플레이어 데이터·입력·기본 환경 씬을 생성합니다.</summary>
        public static void Build()
        {
            EnsureDirectory(SceneDirectory);
            EnsureDirectory(DataDirectory);

            PlayerStatsData stats = LoadOrCreateStats();
            InputActionAsset inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (inputActions == null)
            {
                throw new FileNotFoundException("공유 InputActionAsset을 찾을 수 없습니다.", InputActionsPath);
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Test_01_PlayerBase";

            CreateEnvironment();
            CreatePlayer(stats, inputActions);
            CreateCamera();
            AssignMovementReference(Object.FindFirstObjectByType<PlayerLocomotionController>());
            CreateLight();
            CreateTestUi();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[NUH] 1단계 테스트 씬 생성 완료: {ScenePath}");
        }

        [MenuItem("Flat Venture/NUH/2단계 이동 카메라 씬 생성")]
        public static void BuildStage02FromMenu()
        {
            BuildStage02();
            EditorUtility.DisplayDialog("Flat Venture", "Test_02_MovementCamera 씬 생성을 완료했습니다.", "확인");
        }

        /// <summary>1단계 씬에 Cinemachine Orbit 카메라와 경사 지형을 추가합니다.</summary>
        public static void BuildStage02()
        {
            EnsureDirectory(SceneDirectory);

            if (!File.Exists(Path.GetFullPath(ScenePath)))
            {
                Build();
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject player = GameObject.Find("Player_Warrior_Test");
            if (player == null)
            {
                throw new MissingReferenceException("1단계 씬에서 Player_Warrior_Test를 찾을 수 없습니다.");
            }

            EnsurePlayerCoreComponents(player);

            ReplaceWithCinemachineCamera(player.transform);
            CreateSlopeTestArea();
            UpdateTestGuide();

            EditorSceneManager.SaveScene(scene, Stage02ScenePath, true);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[NUH] 2단계 테스트 씬 생성 완료: {Stage02ScenePath}");
        }

        [MenuItem("Flat Venture/NUH/3단계 대시 씬 생성")]
        public static void BuildStage03FromMenu()
        {
            BuildStage03();
            EditorUtility.DisplayDialog("Flat Venture", "Test_03_Dash 씬 생성을 완료했습니다.", "확인");
        }

        /// <summary>2단계 씬에 대시 입력·상태 표시·벽 충돌 테스트를 추가합니다.</summary>
        public static void BuildStage03()
        {
            EnsureDirectory(SceneDirectory);
            EnsureDashInputAction();

            if (!File.Exists(Path.GetFullPath(Stage02ScenePath)))
            {
                BuildStage02();
            }

            Scene scene = EditorSceneManager.OpenScene(Stage02ScenePath, OpenSceneMode.Single);
            PlayerController player = Object.FindFirstObjectByType<PlayerController>();
            if (player == null)
            {
                throw new MissingReferenceException("2단계 씬에서 PlayerController를 찾을 수 없습니다.");
            }

            EnsurePlayerCoreComponents(player.gameObject);

            CreateDashDebugUi(player);
            CreateDashWallTest();
            UpdateDashTestGuide();

            EditorSceneManager.SaveScene(scene, Stage03ScenePath, true);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[NUH] 3단계 테스트 씬 생성 완료: {Stage03ScenePath}");
        }

        [MenuItem("Flat Venture/NUH/4단계 HP 피격 사망 씬 생성")]
        public static void BuildStage04FromMenu()
        {
            BuildStage04();
            EditorUtility.DisplayDialog("Flat Venture", "Test_04_HealthDeath 씬 생성을 완료했습니다.", "확인");
        }

        /// <summary>3단계 씬에 HP·피격 무적·사망 UI와 피해 구역을 추가합니다.</summary>
        public static void BuildStage04()
        {
            EnsureDirectory(SceneDirectory);
            if (!File.Exists(Path.GetFullPath(Stage03ScenePath)))
                BuildStage03();

            Scene scene = EditorSceneManager.OpenScene(Stage03ScenePath, OpenSceneMode.Single);
            PlayerController player = Object.FindFirstObjectByType<PlayerController>();
            if (player == null)
                throw new MissingReferenceException("3단계 씬에서 PlayerController를 찾을 수 없습니다.");

            EnsurePlayerCoreComponents(player.gameObject);

            EnsureEventSystem();
            CreateHealthDebugUi(player);
            CreateDamageZone();
            UpdateHealthTestGuide();

            EditorSceneManager.SaveScene(scene, Stage04ScenePath, true);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[NUH] 4단계 테스트 씬 생성 완료: {Stage04ScenePath}");
        }

        [MenuItem("Flat Venture/NUH/5단계 조준 기본공격 씬 생성")]
        public static void BuildStage05FromMenu()
        {
            BuildStage05();
            EditorUtility.DisplayDialog("Flat Venture", "Test_05_AimBasicAttack 씬 생성을 완료했습니다.", "확인");
        }

        /// <summary>4단계 씬에 자동/수동 조준 기본 공격과 더미 타깃을 추가합니다.</summary>
        public static void BuildStage05()
        {
            EnsureDirectory(SceneDirectory);
            EnsureCombatInputActions();
            if (!File.Exists(Path.GetFullPath(Stage04ScenePath)))
                BuildStage04();

            Scene scene = EditorSceneManager.OpenScene(Stage04ScenePath, OpenSceneMode.Single);
            PlayerController player = Object.FindFirstObjectByType<PlayerController>();
            if (player == null)
                throw new MissingReferenceException("4단계 씬에서 PlayerController를 찾을 수 없습니다.");

            EnsurePlayerCoreComponents(player.gameObject);

            PlayerBasicAttackController attackController = player.GetComponent<PlayerBasicAttackController>();
            if (attackController == null)
                attackController = player.gameObject.AddComponent<PlayerBasicAttackController>();

            CreateAttackDummies();
            CreateAttackDebugView(player, attackController);
            UpdateAttackTestGuide();

            EditorSceneManager.SaveScene(scene, Stage05ScenePath, true);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[NUH] 5단계 테스트 씬 생성 완료: {Stage05ScenePath}");
        }

        [MenuItem("Flat Venture/NUH/6단계 전사 검기 씬 생성")]
        public static void BuildStage06FromMenu()
        {
            BuildStage06();
            EditorUtility.DisplayDialog("Flat Venture", "Test_06_SwordWave 씬 생성을 완료했습니다.", "확인");
        }

        /// <summary>5단계 씬에 검기 프리팹·오브젝트 풀·액티브 스킬 테스트를 추가합니다.</summary>
        public static void BuildStage06()
        {
            EnsureDirectory(SceneDirectory);
            EnsureDirectory(SkillPrefabDirectory);
            EnsureDirectory(TestMaterialDirectory);
            EnsureActiveSkillInputAction();

            if (!File.Exists(Path.GetFullPath(Stage05ScenePath)))
                BuildStage05();

            CreateSwordWavePrefab();
            Scene scene = EditorSceneManager.OpenScene(Stage05ScenePath, OpenSceneMode.Single);
            PlayerController player = Object.FindFirstObjectByType<PlayerController>();
            if (player == null)
                throw new MissingReferenceException("5단계 씬에서 PlayerController를 찾을 수 없습니다.");

            EnsurePlayerCoreComponents(player.gameObject);

            WarriorActiveSkillProjectile projectilePrefab =
                AssetDatabase.LoadAssetAtPath<WarriorActiveSkillProjectile>(SwordWavePrefabPath);
            if (projectilePrefab == null)
                throw new MissingReferenceException("검기 프리팹을 찾을 수 없습니다.");

            PlayerBasicAttackController basicAttack = player.GetComponent<PlayerBasicAttackController>();
            if (basicAttack != null)
                basicAttack.enabled = false;

            WarriorActiveSkillController skill = player.GetComponent<WarriorActiveSkillController>();
            if (skill == null)
                skill = player.gameObject.AddComponent<WarriorActiveSkillController>();

            SerializedObject skillObject = new SerializedObject(skill);
            skillObject.FindProperty("projectilePrefab").objectReferenceValue = projectilePrefab;
            skillObject.ApplyModifiedPropertiesWithoutUndo();

            CreateSwordWaveTargets();
            CreateWarriorActiveSkillDebugView(player, skill);
            UpdateSwordWaveTestGuide();

            EditorSceneManager.SaveScene(scene, Stage06ScenePath, true);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[NUH] 6단계 테스트 씬 생성 완료: {Stage06ScenePath}");
        }

        [MenuItem("Flat Venture/NUH/7단계 통합 테스트 UI 씬 생성")]
        public static void BuildStage07FromMenu()
        {
            BuildStage07();
            EditorUtility.DisplayDialog("Flat Venture", "Test_07_IntegratedDebugUI 씬 생성을 완료했습니다.", "확인");
        }

        /// <summary>플레이어 기능을 한 화면에서 조절할 통합 디버그 패널을 추가합니다.</summary>
        public static void BuildStage07()
        {
            EnsureDirectory(SceneDirectory);
            if (!File.Exists(Path.GetFullPath(Stage06ScenePath)))
                BuildStage06();

            Scene scene = EditorSceneManager.OpenScene(Stage06ScenePath, OpenSceneMode.Single);
            PlayerController player = Object.FindFirstObjectByType<PlayerController>();
            if (player != null)
                EnsurePlayerCoreComponents(player.gameObject);
            WarriorActiveSkillController warriorActiveSkill = Object.FindFirstObjectByType<WarriorActiveSkillController>();
            PlayerBasicAttackController basicAttack = Object.FindFirstObjectByType<PlayerBasicAttackController>();
            PlayerLocomotionController locomotion = Object.FindFirstObjectByType<PlayerLocomotionController>();
            PlayerHealthController health = Object.FindFirstObjectByType<PlayerHealthController>();
            if (player == null || locomotion == null || health == null || basicAttack == null || warriorActiveSkill == null)
                throw new MissingReferenceException("6단계 씬에서 플레이어 또는 검기 컨트롤러를 찾을 수 없습니다.");

            basicAttack.enabled = true;
            AssignPlayerInspectorReferences(player, locomotion, warriorActiveSkill);

            RemoveLegacyDebugUi();
            CreateIntegratedDebugPanel(player, locomotion, health, warriorActiveSkill);
            UpdateIntegratedUiGuide();

            EditorSceneManager.SaveScene(scene, Stage07ScenePath, true);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[NUH] 7단계 테스트 씬 생성 완료: {Stage07ScenePath}");
        }

        [MenuItem("Flat Venture/NUH/8단계 시드 디버그 씬 생성")]
        public static void BuildStage08FromMenu()
        {
            BuildStage08();
            EditorUtility.DisplayDialog("Flat Venture", "Test_08_SeedDebug 씬 생성을 완료했습니다.", "확인");
        }

        /// <summary>플레이어와 분리된 시드 생성·스트림 재현 전용 씬을 생성합니다.</summary>
        public static void BuildStage08()
        {
            EnsureDirectory(SceneDirectory);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Test_08_SeedDebug";
            EnsureEventSystem();
            CreateSeedDebugUi();

            EditorSceneManager.SaveScene(scene, Stage08ScenePath, true);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[NUH] 8단계 테스트 씬 생성 완료: {Stage08ScenePath}");
        }

        [MenuItem("Flat Venture/NUH/9단계 플레이어 모듈 검증 씬 생성")]
        public static void BuildStage09FromMenu()
        {
            BuildStage09();
            EditorUtility.DisplayDialog("Flat Venture", "Test_09_PlayerValidation 씬 생성을 완료했습니다.", "확인");
        }

        /// <summary>통합 플레이어 씬에 자동 검증 패널과 수동 테스트 안내를 추가합니다.</summary>
        public static void BuildStage09()
        {
            EnsureDirectory(SceneDirectory);
            if (!File.Exists(Path.GetFullPath(Stage07ScenePath)))
                BuildStage07();

            Scene scene = EditorSceneManager.OpenScene(Stage07ScenePath, OpenSceneMode.Single);
            PlayerController player = Object.FindFirstObjectByType<PlayerController>();
            PlayerInputReader inputReader = Object.FindFirstObjectByType<PlayerInputReader>();
            PlayerLocomotionController locomotion = Object.FindFirstObjectByType<PlayerLocomotionController>();
            PlayerHealthController health = Object.FindFirstObjectByType<PlayerHealthController>();
            PlayerBasicAttackController basicAttack = Object.FindFirstObjectByType<PlayerBasicAttackController>();
            WarriorActiveSkillController warriorActiveSkill = Object.FindFirstObjectByType<WarriorActiveSkillController>();
            if (player == null || inputReader == null || locomotion == null || health == null
                || basicAttack == null || warriorActiveSkill == null)
            {
                throw new MissingReferenceException("7단계 씬에서 플레이어 모듈 구성요소를 찾을 수 없습니다.");
            }

            basicAttack.enabled = true;
            AssignPlayerInspectorReferences(player, locomotion, warriorActiveSkill);
            CreatePlayerValidationPanel(player, inputReader, locomotion, health, basicAttack, warriorActiveSkill);
            UpdatePlayerValidationGuide();

            EditorSceneManager.SaveScene(scene, Stage09ScenePath, true);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[NUH] 9단계 테스트 씬 생성 완료: {Stage09ScenePath}");
        }

        [MenuItem("Flat Venture/NUH/10단계 궁수 원거리 기본공격 씬 생성")]
        public static void BuildStage10FromMenu()
        {
            BuildStage10();
            EditorUtility.DisplayDialog("Flat Venture", "Test_10_ArcherBasicAttack 씬 생성을 완료했습니다.", "확인");
        }

        /// <summary>공통 조준·공격 주기에 궁수 화살 프리팹과 원거리용 SO를 연결한 테스트 씬을 만듭니다.</summary>
        public static void BuildStage10()
        {
            EnsureDirectory(SceneDirectory);
            EnsureDirectory(BasicAttackPrefabDirectory);
            EnsureDirectory(TestMaterialDirectory);
            EnsureDirectory(DataDirectory);

            if (!File.Exists(Path.GetFullPath(Stage05ScenePath)))
                BuildStage05();

            LoadOrCreateArcherStats();
            CreateArcherArrowPrefab();
            Scene scene = EditorSceneManager.OpenScene(Stage05ScenePath, OpenSceneMode.Single);
            PlayerStatsData archerStats = AssetDatabase.LoadAssetAtPath<PlayerStatsData>(ArcherStatsPath);
            PlayerBasicAttackProjectile arrowPrefab =
                AssetDatabase.LoadAssetAtPath<PlayerBasicAttackProjectile>(ArcherArrowPrefabPath);
            PlayerController player = Object.FindFirstObjectByType<PlayerController>();
            PlayerBasicAttackController basicAttack = Object.FindFirstObjectByType<PlayerBasicAttackController>();
            if (player == null || basicAttack == null || archerStats == null || arrowPrefab == null)
                throw new MissingReferenceException("5단계 씬에서 플레이어 기본 공격 모듈을 찾을 수 없습니다.");

            player.name = "Player_Archer_Test";
            AssignPlayerStats(player, archerStats);
            AssignBasicAttackProjectile(basicAttack, arrowPrefab);
            CreateArcherAttackTargets();
            UpdateArcherAttackTestGuide();

            EditorSceneManager.SaveScene(scene, Stage10ScenePath, true);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[NUH] 10단계 궁수 원거리 기본공격 씬 생성 완료: {Stage10ScenePath}");
        }

        [MenuItem("Flat Venture/NUH/11단계 궁수 액티브 스킬 씬 생성")]
        public static void BuildStage11FromMenu()
        {
            BuildStage11();
            EditorUtility.DisplayDialog("Flat Venture", "Test_11_ArcherActiveSkill 씬 생성을 완료했습니다.", "확인");
        }

        /// <summary>궁수 기본공격 씬에 관통 사격 액티브 스킬과 전용 테스트 배치를 추가합니다.</summary>
        public static void BuildStage11()
        {
            EnsureDirectory(SceneDirectory);
            EnsureDirectory(SkillPrefabDirectory);
            EnsureDirectory(TestMaterialDirectory);
            EnsureDirectory(DataDirectory);
            EnsureActiveSkillInputAction();

            if (!File.Exists(Path.GetFullPath(Stage10ScenePath)))
                BuildStage10();

            LoadOrCreateArcherStats();
            CreateArcherPiercingShotPrefab();
            Scene scene = EditorSceneManager.OpenScene(Stage10ScenePath, OpenSceneMode.Single);
            PlayerStatsData archerStats = AssetDatabase.LoadAssetAtPath<PlayerStatsData>(ArcherStatsPath);
            ArcherActiveSkillProjectile piercingShotPrefab =
                AssetDatabase.LoadAssetAtPath<ArcherActiveSkillProjectile>(ArcherPiercingShotPrefabPath);
            PlayerController player = Object.FindFirstObjectByType<PlayerController>();
            PlayerBasicAttackController basicAttack = Object.FindFirstObjectByType<PlayerBasicAttackController>();
            if (player == null || basicAttack == null || archerStats == null || piercingShotPrefab == null)
                throw new MissingReferenceException("10단계 씬에서 궁수 플레이어 또는 관통 사격 프리팹을 찾을 수 없습니다.");

            player.name = "Player_Archer_ActiveSkill_Test";
            AssignPlayerStats(player, archerStats);
            ArcherActiveSkillController skill = player.GetComponent<ArcherActiveSkillController>();
            if (skill == null)
                skill = player.gameObject.AddComponent<ArcherActiveSkillController>();

            AssignArcherPiercingShotPrefab(skill, piercingShotPrefab);
            CreateArcherActiveSkillTargets();
            CreateArcherActiveSkillDebugView(player, skill);
            UpdateArcherActiveSkillGuide();

            EditorSceneManager.SaveScene(scene, Stage11ScenePath, true);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[NUH] 11단계 궁수 액티브 스킬 씬 생성 완료: {Stage11ScenePath}");
        }

        [MenuItem("Flat Venture/NUH/12단계 마법사 원거리 기본공격 씬 생성")]
        public static void BuildStage12FromMenu()
        {
            BuildStage12();
            EditorUtility.DisplayDialog("Flat Venture", "Test_12_MageBasicAttack 씬 생성을 완료했습니다.", "확인");
        }

        /// <summary>마법탄 직접 피해와 50% 스플래시 피해를 확인할 마법사 기본 공격 테스트 씬을 만듭니다.</summary>
        public static void BuildStage12()
        {
            EnsureDirectory(SceneDirectory);
            EnsureDirectory(BasicAttackPrefabDirectory);
            EnsureDirectory(TestMaterialDirectory);
            EnsureDirectory(DataDirectory);

            if (!File.Exists(Path.GetFullPath(Stage10ScenePath)))
                BuildStage10();

            LoadOrCreateMageStats();
            CreateMageOrbPrefab();
            Scene scene = EditorSceneManager.OpenScene(Stage10ScenePath, OpenSceneMode.Single);
            PlayerStatsData mageStats = AssetDatabase.LoadAssetAtPath<PlayerStatsData>(MageStatsPath);
            PlayerBasicAttackProjectile mageOrbPrefab =
                AssetDatabase.LoadAssetAtPath<PlayerBasicAttackProjectile>(MageOrbPrefabPath);
            PlayerController player = Object.FindFirstObjectByType<PlayerController>();
            PlayerBasicAttackController basicAttack = Object.FindFirstObjectByType<PlayerBasicAttackController>();
            if (player == null || basicAttack == null || mageStats == null || mageOrbPrefab == null)
                throw new MissingReferenceException("10단계 씬에서 마법사 기본 공격 구성요소를 찾을 수 없습니다.");

            player.name = "Player_Mage_BasicAttack_Test";
            AssignPlayerStats(player, mageStats);
            AssignBasicAttackProjectile(basicAttack, mageOrbPrefab);
            ApplyPreferredCinemachineOptions();
            CreateMageAttackTargets();
            UpdateMageAttackTestGuide();

            EditorSceneManager.SaveScene(scene, Stage12ScenePath, true);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[NUH] 12단계 마법사 원거리 기본공격 씬 생성 완료: {Stage12ScenePath}");
        }

        [MenuItem("Flat Venture/NUH/13단계 마법사 액티브 스킬 씬 생성")]
        public static void BuildStage13FromMenu()
        {
            BuildStage13();
            EditorUtility.DisplayDialog("Flat Venture", "Test_13_MageActiveSkill 씬 생성을 완료했습니다.", "확인");
        }

        /// <summary>마법사 기본공격 씬에 좌우 30도 부채꼴 파이어볼 액티브 스킬 테스트 구성을 추가합니다.</summary>
        public static void BuildStage13()
        {
            EnsureDirectory(SceneDirectory);
            EnsureDirectory(SkillPrefabDirectory);
            EnsureDirectory(TestMaterialDirectory);
            EnsureDirectory(DataDirectory);
            EnsureActiveSkillInputAction();

            if (!File.Exists(Path.GetFullPath(Stage12ScenePath)))
                BuildStage12();

            LoadOrCreateMageStats();
            CreateMageFireballPrefab();
            Scene scene = EditorSceneManager.OpenScene(Stage12ScenePath, OpenSceneMode.Single);
            PlayerStatsData mageStats = AssetDatabase.LoadAssetAtPath<PlayerStatsData>(MageStatsPath);
            PlayerBasicAttackProjectile fireballPrefab =
                AssetDatabase.LoadAssetAtPath<PlayerBasicAttackProjectile>(MageFireballPrefabPath);
            PlayerController player = Object.FindFirstObjectByType<PlayerController>();
            PlayerBasicAttackController basicAttack = Object.FindFirstObjectByType<PlayerBasicAttackController>();
            if (player == null || basicAttack == null || mageStats == null || fireballPrefab == null)
                throw new MissingReferenceException("12단계 씬에서 마법사 액티브 스킬 구성요소를 찾을 수 없습니다.");

            player.name = "Player_Mage_ActiveSkill_Test";
            AssignPlayerStats(player, mageStats);
            MageActiveSkillController skill = player.GetComponent<MageActiveSkillController>();
            if (skill == null)
                skill = player.gameObject.AddComponent<MageActiveSkillController>();

            AssignMageActiveSkillPrefab(skill, fireballPrefab);
            ApplyPreferredCinemachineOptions();
            CreateMageActiveSkillTargets();
            CreateMageActiveSkillDebugView(player, skill);
            UpdateMageActiveSkillGuide();

            EditorSceneManager.SaveScene(scene, Stage13ScenePath, true);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[NUH] 13단계 마법사 액티브 스킬 씬 생성 완료: {Stage13ScenePath}");
        }

        [MenuItem("Flat Venture/NUH/14단계 직업 변경 전투 디버그 씬 생성")]
        public static void BuildStage14FromMenu()
        {
            BuildStage14();
            EditorUtility.DisplayDialog("Flat Venture", "Test_14_JobSwitchCombatDebug 씬 생성을 완료했습니다.", "확인");
        }

        /// <summary>전사, 궁수, 마법사를 버튼으로 실시간 전환하며 기본 공격과 액티브 스킬을 디버깅하는 씬을 만듭니다.</summary>
        public static void BuildStage14()
        {
            EnsureDirectory(SceneDirectory);
            EnsureDirectory(SkillPrefabDirectory);
            EnsureDirectory(BasicAttackPrefabDirectory);
            EnsureDirectory(TestMaterialDirectory);
            EnsureDirectory(DataDirectory);
            EnsureActiveSkillInputAction();

            if (!File.Exists(Path.GetFullPath(Stage13ScenePath)))
                BuildStage13();

            PlayerStatsData warriorStats = LoadOrCreateStats();
            PlayerStatsData archerStats = LoadOrCreateArcherStats();
            PlayerStatsData mageStats = LoadOrCreateMageStats();
            WarriorActiveSkillProjectile swordWavePrefab = CreateSwordWavePrefab();
            ArcherActiveSkillProjectile archerActivePrefab = CreateArcherPiercingShotPrefab();
            PlayerBasicAttackProjectile archerBasicPrefab = CreateArcherArrowPrefab();
            PlayerBasicAttackProjectile mageBasicPrefab = CreateMageOrbPrefab();
            PlayerBasicAttackProjectile mageActivePrefab = CreateMageFireballPrefab();
            FillMissingJobSwitchAssets(
                ref warriorStats,
                ref archerStats,
                ref mageStats,
                ref swordWavePrefab,
                ref archerActivePrefab,
                ref archerBasicPrefab,
                ref mageBasicPrefab,
                ref mageActivePrefab);

            Scene scene = EditorSceneManager.OpenScene(Stage13ScenePath, OpenSceneMode.Single);
            PlayerController player = Object.FindFirstObjectByType<PlayerController>();
            PlayerBasicAttackController basicAttack = Object.FindFirstObjectByType<PlayerBasicAttackController>();
            PlayerLocomotionController locomotion = Object.FindFirstObjectByType<PlayerLocomotionController>();
            if (player == null || basicAttack == null)
                throw new MissingReferenceException("13단계 씬에서 직업 변경 테스트 구성요소를 찾을 수 없습니다.");

            player.name = "Player_Warrior_JobSwitch_Test";
            AssignPlayerStats(player, warriorStats);
            AssignBasicAttackProjectile(basicAttack, null);
            AssignMovementReference(locomotion);

            WarriorActiveSkillController warriorSkill = player.GetComponent<WarriorActiveSkillController>();
            if (warriorSkill == null)
                warriorSkill = player.gameObject.AddComponent<WarriorActiveSkillController>();
            ArcherActiveSkillController archerSkill = player.GetComponent<ArcherActiveSkillController>();
            if (archerSkill == null)
                archerSkill = player.gameObject.AddComponent<ArcherActiveSkillController>();
            MageActiveSkillController mageSkill = player.GetComponent<MageActiveSkillController>();
            if (mageSkill == null)
                mageSkill = player.gameObject.AddComponent<MageActiveSkillController>();

            AssignWarriorActiveSkillPrefab(warriorSkill, swordWavePrefab);
            AssignArcherPiercingShotPrefab(archerSkill, archerActivePrefab);
            AssignMageActiveSkillPrefab(mageSkill, mageActivePrefab);
            warriorSkill.enabled = true;
            archerSkill.enabled = false;
            mageSkill.enabled = false;

            ApplyPreferredCinemachineOptions();
            CreateJobSwitchTargets();
            CreateJobSwitchDebugPanel(
                player,
                basicAttack,
                warriorSkill,
                archerSkill,
                mageSkill,
                warriorStats,
                archerStats,
                mageStats,
                archerBasicPrefab,
                mageBasicPrefab);
            UpdateJobSwitchGuide();

            EditorSceneManager.SaveScene(scene, Stage14ScenePath, true);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[NUH] 14단계 직업 변경 전투 디버그 씬 생성 완료: {Stage14ScenePath}");
        }

        /// <summary>생성 메서드가 돌려준 참조를 우선 사용하고, 비어 있는 항목만 에셋 경로에서 다시 찾습니다.</summary>
        private static void FillMissingJobSwitchAssets(
            ref PlayerStatsData warriorStats,
            ref PlayerStatsData archerStats,
            ref PlayerStatsData mageStats,
            ref WarriorActiveSkillProjectile swordWavePrefab,
            ref ArcherActiveSkillProjectile archerActivePrefab,
            ref PlayerBasicAttackProjectile archerBasicPrefab,
            ref PlayerBasicAttackProjectile mageBasicPrefab,
            ref PlayerBasicAttackProjectile mageActivePrefab)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(StatsPath, ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.ImportAsset(ArcherStatsPath, ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.ImportAsset(MageStatsPath, ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.ImportAsset(SwordWavePrefabPath, ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.ImportAsset(ArcherPiercingShotPrefabPath, ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.ImportAsset(ArcherArrowPrefabPath, ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.ImportAsset(MageOrbPrefabPath, ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.ImportAsset(MageFireballPrefabPath, ImportAssetOptions.ForceSynchronousImport);

            if (warriorStats == null)
                warriorStats = AssetDatabase.LoadAssetAtPath<PlayerStatsData>(StatsPath);
            if (archerStats == null)
                archerStats = AssetDatabase.LoadAssetAtPath<PlayerStatsData>(ArcherStatsPath);
            if (mageStats == null)
                mageStats = AssetDatabase.LoadAssetAtPath<PlayerStatsData>(MageStatsPath);
            if (swordWavePrefab == null)
                swordWavePrefab = LoadPrefabComponent<WarriorActiveSkillProjectile>(SwordWavePrefabPath);
            if (archerActivePrefab == null)
                archerActivePrefab = LoadPrefabComponent<ArcherActiveSkillProjectile>(ArcherPiercingShotPrefabPath);
            if (archerBasicPrefab == null)
                archerBasicPrefab = LoadPrefabComponent<PlayerBasicAttackProjectile>(ArcherArrowPrefabPath);
            if (mageBasicPrefab == null)
                mageBasicPrefab = LoadPrefabComponent<PlayerBasicAttackProjectile>(MageOrbPrefabPath);
            if (mageActivePrefab == null)
                mageActivePrefab = LoadPrefabComponent<PlayerBasicAttackProjectile>(MageFireballPrefabPath);
        }

        /// <summary>프리팹 에셋의 루트 GameObject에서 지정 컴포넌트를 안정적으로 로드합니다.</summary>
        private static T LoadPrefabComponent<T>(string path) where T : Component
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            return prefab != null ? prefab.GetComponent<T>() : null;
        }

        /// <summary>
        /// 단계가 올라가도 플레이어 핵심 컴포넌트와 SO·카메라 참조가 빠지지 않도록 보장합니다.
        /// </summary>
        private static void EnsurePlayerCoreComponents(GameObject player)
        {
            PlayerController playerController = player.GetComponent<PlayerController>();
            if (playerController == null)
                playerController = player.AddComponent<PlayerController>();
            AssignPlayerStats(playerController, LoadOrCreateStats());

            if (player.GetComponent<PlayerLocomotionController>() == null)
                player.AddComponent<PlayerLocomotionController>();
            if (player.GetComponent<PlayerHealthController>() == null)
                player.AddComponent<PlayerHealthController>();
            if (player.GetComponent<PlayerAimResolver>() == null)
                player.AddComponent<PlayerAimResolver>();

            AssignMovementReference(player.GetComponent<PlayerLocomotionController>());
        }

        /// <summary>
        /// 플레이어 인스펙터에서 확인할 수 있도록 씬 오브젝트와 프리팹 참조를 명시적으로 연결합니다.
        /// 런타임 자동 탐색에만 의존하지 않아 누락된 참조를 에디터에서 바로 발견할 수 있습니다.
        /// </summary>
        private static void AssignPlayerInspectorReferences(
            PlayerController player,
            PlayerLocomotionController locomotion,
            WarriorActiveSkillController warriorActiveSkill)
        {
            AssignPlayerStats(player, LoadOrCreateStats());
            AssignMovementReference(locomotion);

            WarriorActiveSkillProjectile projectilePrefab =
                AssetDatabase.LoadAssetAtPath<WarriorActiveSkillProjectile>(SwordWavePrefabPath);
            if (projectilePrefab == null)
                throw new MissingReferenceException("검기 프리팹을 찾을 수 없습니다.");

            SerializedObject skillObject = new SerializedObject(warriorActiveSkill);
            skillObject.Update();
            skillObject.FindProperty("projectilePrefab").objectReferenceValue = projectilePrefab;
            skillObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(warriorActiveSkill);
        }

        /// <summary>
        /// 이동 방향 계산의 기준이 되는 Main Camera Transform을 Locomotion 인스펙터에 연결합니다.
        /// </summary>
        private static void AssignMovementReference(PlayerLocomotionController locomotion)
        {
            if (locomotion == null || Camera.main == null)
                return;

            SerializedObject locomotionObject = new SerializedObject(locomotion);
            locomotionObject.Update();
            locomotionObject.FindProperty("movementReference").objectReferenceValue = Camera.main.transform;
            locomotionObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(locomotion);
        }

        /// <summary>이전 단계의 개별 디버그 UI를 제거해 통합 패널과 겹치지 않게 합니다.</summary>
        private static void RemoveLegacyDebugUi()
        {
            string[] names =
            {
                "Pnl_HealthDebug",
                "Txt_DashDebug",
                "Txt_AttackDebug",
                "Txt_SwordWaveDebug",
                "Txt_WarriorActiveSkillDebug",
                "Btn_SwordWaveNoCooldown",
                "Btn_WarriorActiveSkillNoCooldown",
                "SwordWaveDebugView",
                "WarriorActiveSkillDebugView",
                "Pnl_IntegratedDebug"
            };

            foreach (string objectName in names)
            {
                GameObject target = GameObject.Find(objectName);
                if (target != null)
                    Object.DestroyImmediate(target);
            }
        }

        /// <summary>오른쪽에 능력치 조절·피해·초기화 버튼을 가진 통합 패널을 만듭니다.</summary>
        private static void CreateIntegratedDebugPanel(
            PlayerController player,
            PlayerLocomotionController locomotion,
            PlayerHealthController health,
            WarriorActiveSkillController warriorActiveSkill)
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            GameObject panel = new GameObject("Pnl_IntegratedDebug", typeof(RectTransform), typeof(Image), typeof(PlayerIntegratedDebugPanel));
            panel.transform.SetParent(canvas.transform, false);

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1f, 1f);
            panelRect.anchorMax = new Vector2(1f, 1f);
            panelRect.pivot = new Vector2(1f, 1f);
            panelRect.anchoredPosition = new Vector2(-20f, -20f);
            panelRect.sizeDelta = new Vector2(600f, 1000f);
            panel.GetComponent<Image>().color = new Color(0.02f, 0.03f, 0.05f, 0.88f);

            Text output = CreateUiText(panel.transform, "Txt_IntegratedStatus", new Vector2(20f, -20f), new Vector2(560f, 300f), 20, TextAnchor.UpperLeft);

            PlayerIntegratedDebugPanel debugPanel = panel.GetComponent<PlayerIntegratedDebugPanel>();
            SerializedObject serializedPanel = new SerializedObject(debugPanel);
            serializedPanel.FindProperty("player").objectReferenceValue = player;
            serializedPanel.FindProperty("locomotion").objectReferenceValue = locomotion;
            serializedPanel.FindProperty("health").objectReferenceValue = health;
            serializedPanel.FindProperty("warriorActiveSkill").objectReferenceValue = warriorActiveSkill;
            serializedPanel.FindProperty("output").objectReferenceValue = output;
            serializedPanel.ApplyModifiedPropertiesWithoutUndo();

            float y = 620f;
            CreateDebugButtonPair(panel.transform, "이동 속도", y, debugPanel.MoveSpeedDown, debugPanel.MoveSpeedUp); y -= 55f;
            CreateDebugButtonPair(panel.transform, "공격력", y, debugPanel.AttackPowerDown, debugPanel.AttackPowerUp); y -= 55f;
            CreateDebugButtonPair(panel.transform, "공격 속도", y, debugPanel.AttackSpeedDown, debugPanel.AttackSpeedUp); y -= 55f;
            CreateDebugButtonPair(panel.transform, "대시 거리", y, debugPanel.DashDistanceDown, debugPanel.DashDistanceUp); y -= 55f;
            CreateDebugButtonPair(panel.transform, "대시 쿨타임", y, debugPanel.DashCooldownDown, debugPanel.DashCooldownUp); y -= 55f;
            CreateDebugButtonPair(panel.transform, "대시 충전", y, debugPanel.DashChargesDown, debugPanel.DashChargesUp); y -= 55f;
            CreateDebugButtonPair(panel.transform, "검기 피해", y, debugPanel.SkillDamageDown, debugPanel.SkillDamageUp); y -= 55f;
            CreateDebugButtonPair(panel.transform, "검기 속도", y, debugPanel.SkillSpeedDown, debugPanel.SkillSpeedUp); y -= 55f;
            CreateDebugButtonPair(panel.transform, "검기 폭", y, debugPanel.SkillWidthDown, debugPanel.SkillWidthUp); y -= 55f;
            CreateDebugButtonPair(panel.transform, "검기 시전", y, debugPanel.SkillCastTimeDown, debugPanel.SkillCastTimeUp);

            Button damage = CreateUiButton(panel.transform, "Btn_DebugDamage", "피해 10", new Vector2(20f, 20f));
            Button heal = CreateUiButton(panel.transform, "Btn_DebugHeal", "체력 회복", new Vector2(165f, 20f));
            Button invincible = CreateUiButton(panel.transform, "Btn_DebugInvincible", "무적 토글", new Vector2(310f, 20f));
            Button reset = CreateUiButton(panel.transform, "Btn_DebugReset", "원본 초기화", new Vector2(455f, 20f));
            Button noCooldown = CreateUiButton(panel.transform, "Btn_DebugNoCooldown", "검기 쿨타임 제거", new Vector2(20f, 78f));
            noCooldown.GetComponent<RectTransform>().sizeDelta = new Vector2(190f, 48f);

            UnityEventTools.AddPersistentListener(damage.onClick, debugPanel.Damage10);
            UnityEventTools.AddPersistentListener(heal.onClick, debugPanel.HealFull);
            UnityEventTools.AddPersistentListener(invincible.onClick, debugPanel.ToggleInvincibility);
            UnityEventTools.AddPersistentListener(reset.onClick, debugPanel.ResetPlayer);
            UnityEventTools.AddPersistentListener(noCooldown.onClick, debugPanel.ToggleWarriorActiveSkillNoCooldown);
        }

        /// <summary>능력치 한 항목의 이름과 -/+ 버튼을 같은 행에 생성합니다.</summary>
        private static void CreateDebugButtonPair(Transform parent, string label, float y, UnityAction downAction, UnityAction upAction)
        {
            Text rowLabel = CreateUiText(parent, $"Txt_{label}", Vector2.zero, new Vector2(250f, 44f), 20, TextAnchor.MiddleLeft);
            RectTransform labelRect = rowLabel.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.zero;
            labelRect.pivot = Vector2.zero;
            labelRect.anchoredPosition = new Vector2(20f, y);
            rowLabel.text = label;

            Button down = CreateUiButton(parent, $"Btn_{label}_Down", "-", new Vector2(320f, y));
            Button up = CreateUiButton(parent, $"Btn_{label}_Up", "+", new Vector2(455f, y));
            down.GetComponent<RectTransform>().sizeDelta = new Vector2(110f, 44f);
            up.GetComponent<RectTransform>().sizeDelta = new Vector2(110f, 44f);
            UnityEventTools.AddPersistentListener(down.onClick, downAction);
            UnityEventTools.AddPersistentListener(up.onClick, upAction);
        }

        /// <summary>Test_07 상단 안내 문구를 통합 UI 사용법으로 바꿉니다.</summary>
        private static void UpdateIntegratedUiGuide()
        {
            GameObject guideObject = GameObject.Find("Txt_TestGuide");
            if (guideObject == null || !guideObject.TryGetComponent(out Text guide))
                return;

            guide.text = "Test_07_IntegratedDebugUI\n오른쪽 패널: 런타임 수치 조절\n원본 초기화: SO 기본값 복원 (SO 에셋은 변경되지 않음)";
        }

        /// <summary>Test_09 왼쪽에 실시간 상태와 자동 검증 버튼을 가진 패널을 만듭니다.</summary>
        private static void CreatePlayerValidationPanel(
            PlayerController player,
            PlayerInputReader inputReader,
            PlayerLocomotionController locomotion,
            PlayerHealthController health,
            PlayerBasicAttackController basicAttack,
            WarriorActiveSkillController warriorActiveSkill)
        {
            GameObject oldPanel = GameObject.Find("Pnl_PlayerValidation");
            if (oldPanel != null)
                Object.DestroyImmediate(oldPanel);

            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            GameObject panel = new GameObject(
                "Pnl_PlayerValidation",
                typeof(RectTransform),
                typeof(Image),
                typeof(PlayerModuleValidationPanel));
            panel.transform.SetParent(canvas.transform, false);

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.zero;
            panelRect.pivot = Vector2.zero;
            panelRect.anchoredPosition = new Vector2(20f, 20f);
            panelRect.sizeDelta = new Vector2(740f, 820f);
            panel.GetComponent<Image>().color = new Color(0.025f, 0.04f, 0.06f, 0.93f);

            Text title = CreateUiText(panel.transform, "Txt_ValidationTitle", new Vector2(20f, -16f), new Vector2(700f, 42f), 26, TextAnchor.UpperLeft);
            title.text = "플레이어 모듈 검증";

            Text liveStatus = CreateUiText(panel.transform, "Txt_ValidationLiveStatus", new Vector2(20f, -64f), new Vector2(700f, 145f), 18, TextAnchor.UpperLeft);
            Text results = CreateUiText(panel.transform, "Txt_ValidationResults", new Vector2(20f, -215f), new Vector2(700f, 150f), 18, TextAnchor.UpperLeft);
            results.text = "아래 자동 검증 버튼을 실행하세요.";

            Text manualGuide = CreateUiText(panel.transform, "Txt_ValidationManualGuide", new Vector2(20f, -375f), new Vector2(700f, 250f), 18, TextAnchor.UpperLeft);
            manualGuide.text =
                "[직접 조작 검증]\n" +
                "1. WASD 이동 중 좌클릭 조준·우클릭 검기·Space 대시\n" +
                "2. 이동 입력 없이 Space: 대시가 발생하지 않아야 함\n" +
                "3. 대시 중 공격·검기 가능, 방향은 처음 입력으로 고정\n" +
                "4. 타깃이 없을 때 자동 공격 정지, 좌클릭은 빈 공간 공격\n" +
                "5. 검기 3발 발사·관통·벽 반환 후 Pool Active가 0\n" +
                "6. 원본 초기화 후 HP·대시·쿨타임·무적 상태 확인";

            Button damageGrace = CreateUiButton(panel.transform, "Btn_CheckDamageGrace", "피격 무적", new Vector2(20f, 82f));
            Button overlap = CreateUiButton(panel.transform, "Btn_CheckInvincibility", "무적 중첩", new Vector2(195f, 82f));
            Button deathReset = CreateUiButton(panel.transform, "Btn_CheckDeathReset", "사망·초기화", new Vector2(370f, 82f));
            Button boundaries = CreateUiButton(panel.transform, "Btn_CheckBoundaries", "경계값", new Vector2(545f, 82f));
            foreach (Button button in new[] { damageGrace, overlap, deathReset, boundaries })
                button.GetComponent<RectTransform>().sizeDelta = new Vector2(155f, 48f);

            Button runAll = CreateUiButton(panel.transform, "Btn_RunAllPlayerChecks", "자동 검증 전체 실행", new Vector2(20f, 20f));
            runAll.GetComponent<RectTransform>().sizeDelta = new Vector2(220f, 48f);
            Button clear = CreateUiButton(panel.transform, "Btn_ClearPlayerChecks", "결과 지우기", new Vector2(260f, 20f));
            clear.GetComponent<RectTransform>().sizeDelta = new Vector2(155f, 48f);

            PlayerModuleValidationPanel validationPanel = panel.GetComponent<PlayerModuleValidationPanel>();
            SerializedObject serializedPanel = new SerializedObject(validationPanel);
            serializedPanel.FindProperty("player").objectReferenceValue = player;
            serializedPanel.FindProperty("inputReader").objectReferenceValue = inputReader;
            serializedPanel.FindProperty("locomotion").objectReferenceValue = locomotion;
            serializedPanel.FindProperty("health").objectReferenceValue = health;
            serializedPanel.FindProperty("basicAttack").objectReferenceValue = basicAttack;
            serializedPanel.FindProperty("warriorActiveSkill").objectReferenceValue = warriorActiveSkill;
            serializedPanel.FindProperty("liveStatusOutput").objectReferenceValue = liveStatus;
            serializedPanel.FindProperty("validationResultOutput").objectReferenceValue = results;
            serializedPanel.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddPersistentListener(damageGrace.onClick, validationPanel.RunDamageGraceCheck);
            UnityEventTools.AddPersistentListener(overlap.onClick, validationPanel.RunInvincibilityOverlapCheck);
            UnityEventTools.AddPersistentListener(deathReset.onClick, validationPanel.RunDeathResetCheck);
            UnityEventTools.AddPersistentListener(boundaries.onClick, validationPanel.RunBoundaryCheck);
            UnityEventTools.AddPersistentListener(runAll.onClick, validationPanel.RunAllChecks);
            UnityEventTools.AddPersistentListener(clear.onClick, validationPanel.ClearResults);
        }

        /// <summary>Test_09 상단 안내 문구를 검증 씬 사용법으로 바꿉니다.</summary>
        private static void UpdatePlayerValidationGuide()
        {
            GameObject guideObject = GameObject.Find("Txt_TestGuide");
            if (guideObject == null || !guideObject.TryGetComponent(out Text guide))
                return;

            guide.text =
                "Test_09_PlayerValidation\n" +
                "왼쪽: 자동 검증·실시간 상태 | 오른쪽: 능력치 조절\n" +
                "중앙 플레이 영역에서 이동·대시·조준·공격·검기를 함께 확인";
        }

        /// <summary>시드 입력·복사·스트림 선택·난수 출력을 한 화면에 구성합니다.</summary>
        private static void CreateSeedDebugUi()
        {
            GameObject canvasObject = new GameObject("Pnl_SeedTestUI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            GameObject background = new GameObject("Pnl_Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(canvasObject.transform, false);
            RectTransform backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;
            background.GetComponent<Image>().color = new Color(0.035f, 0.045f, 0.065f, 1f);

            GameObject panel = new GameObject("Pnl_SeedDebug", typeof(RectTransform), typeof(Image), typeof(SeedDebugPanel));
            panel.transform.SetParent(background.transform, false);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(1240f, 900f);
            panel.GetComponent<Image>().color = new Color(0.07f, 0.09f, 0.13f, 0.98f);

            Text title = CreateUiText(panel.transform, "Txt_Title", new Vector2(30f, -24f), new Vector2(1180f, 50f), 30, TextAnchor.UpperLeft);
            title.text = "Test_08_SeedDebug — MT19937 시드·스트림 재현 테스트";

            Text status = CreateUiText(panel.transform, "Txt_SeedStatus", new Vector2(30f, -82f), new Vector2(1180f, 86f), 23, TextAnchor.UpperLeft);
            status.text = "실행하면 무작위 6자리 RunSeed가 생성됩니다.";

            Text seedLabel = CreateUiText(panel.transform, "Txt_SeedInputLabel", new Vector2(30f, -190f), new Vector2(260f, 42f), 21, TextAnchor.MiddleLeft);
            seedLabel.text = "RunSeed (숫자 6자리)";
            InputField seedInput = CreateUiInputField(panel.transform, "Inp_RunSeed", "예: 123456", new Vector2(30f, 615f), new Vector2(260f, 52f));

            Button applySeed = CreateUiButton(panel.transform, "Btn_ApplySeed", "시드 적용", new Vector2(310f, 615f));
            Button randomSeed = CreateUiButton(panel.transform, "Btn_RandomSeed", "새 시드", new Vector2(455f, 615f));
            Button copySeed = CreateUiButton(panel.transform, "Btn_CopySeed", "시드 복사", new Vector2(600f, 615f));
            Button replaySeed = CreateUiButton(panel.transform, "Btn_ReplaySeed", "동일 시드 재실행", new Vector2(745f, 615f));
            replaySeed.GetComponent<RectTransform>().sizeDelta = new Vector2(190f, 48f);

            Text streamLabel = CreateUiText(panel.transform, "Txt_StreamInputLabel", new Vector2(30f, -315f), new Vector2(350f, 42f), 21, TextAnchor.MiddleLeft);
            streamLabel.text = "스트림 이름 (직접 입력 가능)";
            InputField streamInput = CreateUiInputField(panel.transform, "Inp_StreamName", "Shop", new Vector2(30f, 490f), new Vector2(260f, 52f));
            streamInput.text = "Shop";

            string[] presetLabels = { "Map", "Monster", "Item", "Shop", "Forge", "Event" };
            Button[] presetButtons = new Button[presetLabels.Length];
            for (int i = 0; i < presetLabels.Length; i++)
            {
                presetButtons[i] = CreateUiButton(
                    panel.transform,
                    $"Btn_Stream{presetLabels[i]}",
                    presetLabels[i],
                    new Vector2(310f + (i * 145f), 490f));
            }

            Button nextValues = CreateUiButton(panel.transform, "Btn_NextSeedValues", "다음 값 5개", new Vector2(30f, 410f));
            nextValues.GetComponent<RectTransform>().sizeDelta = new Vector2(190f, 52f);
            Button clearOutput = CreateUiButton(panel.transform, "Btn_ClearSeedOutput", "출력 지우기", new Vector2(240f, 410f));
            clearOutput.GetComponent<RectTransform>().sizeDelta = new Vector2(190f, 52f);

            Text guide = CreateUiText(panel.transform, "Txt_SeedGuide", new Vector2(480f, -432f), new Vector2(720f, 82f), 19, TextAnchor.UpperLeft);
            guide.text = "같은 RunSeed + 같은 스트림 + 같은 호출 순서 = 같은 결과\n한 스트림의 호출은 다른 스트림 결과에 영향을 주지 않습니다.";

            GameObject outputBackground = new GameObject("Pnl_StreamOutput", typeof(RectTransform), typeof(Image));
            outputBackground.transform.SetParent(panel.transform, false);
            RectTransform outputBackgroundRect = outputBackground.GetComponent<RectTransform>();
            outputBackgroundRect.anchorMin = Vector2.zero;
            outputBackgroundRect.anchorMax = Vector2.zero;
            outputBackgroundRect.pivot = Vector2.zero;
            outputBackgroundRect.anchoredPosition = new Vector2(30f, 30f);
            outputBackgroundRect.sizeDelta = new Vector2(1180f, 350f);
            outputBackground.GetComponent<Image>().color = new Color(0.02f, 0.025f, 0.04f, 1f);

            Text output = CreateUiText(outputBackground.transform, "Txt_StreamOutput", new Vector2(20f, -18f), new Vector2(1140f, 315f), 18, TextAnchor.UpperLeft);
            output.horizontalOverflow = HorizontalWrapMode.Wrap;
            output.verticalOverflow = VerticalWrapMode.Truncate;

            SeedDebugPanel debugPanel = panel.GetComponent<SeedDebugPanel>();
            SerializedObject serializedPanel = new SerializedObject(debugPanel);
            serializedPanel.FindProperty("seedInput").objectReferenceValue = seedInput;
            serializedPanel.FindProperty("streamNameInput").objectReferenceValue = streamInput;
            serializedPanel.FindProperty("statusOutput").objectReferenceValue = status;
            serializedPanel.FindProperty("streamOutput").objectReferenceValue = output;
            serializedPanel.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddPersistentListener(applySeed.onClick, debugPanel.ApplyInputSeed);
            UnityEventTools.AddPersistentListener(randomSeed.onClick, debugPanel.GenerateRandomSeed);
            UnityEventTools.AddPersistentListener(copySeed.onClick, debugPanel.CopyCurrentSeed);
            UnityEventTools.AddPersistentListener(replaySeed.onClick, debugPanel.ReplayCurrentSeed);
            UnityEventTools.AddPersistentListener(nextValues.onClick, debugPanel.DrawNextValues);
            UnityEventTools.AddPersistentListener(clearOutput.onClick, debugPanel.ClearOutput);
            UnityEventTools.AddPersistentListener(presetButtons[0].onClick, debugPanel.SelectMapStream);
            UnityEventTools.AddPersistentListener(presetButtons[1].onClick, debugPanel.SelectMonsterStream);
            UnityEventTools.AddPersistentListener(presetButtons[2].onClick, debugPanel.SelectItemStream);
            UnityEventTools.AddPersistentListener(presetButtons[3].onClick, debugPanel.SelectShopStream);
            UnityEventTools.AddPersistentListener(presetButtons[4].onClick, debugPanel.SelectForgeStream);
            UnityEventTools.AddPersistentListener(presetButtons[5].onClick, debugPanel.SelectEventStream);
        }

        /// <summary>레거시 UGUI InputField와 값 Text·Placeholder를 함께 생성합니다.</summary>
        private static InputField CreateUiInputField(
            Transform parent,
            string name,
            string placeholderText,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            GameObject inputObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(InputField));
            inputObject.transform.SetParent(parent, false);
            RectTransform rect = inputObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            inputObject.GetComponent<Image>().color = new Color(0.14f, 0.17f, 0.22f, 1f);

            Text valueText = CreateUiText(inputObject.transform, "Txt_Value", Vector2.zero, Vector2.zero, 22, TextAnchor.MiddleLeft);
            StretchInputText(valueText.rectTransform);

            Text placeholder = CreateUiText(inputObject.transform, "Txt_Placeholder", Vector2.zero, Vector2.zero, 20, TextAnchor.MiddleLeft);
            StretchInputText(placeholder.rectTransform);
            placeholder.text = placeholderText;
            placeholder.color = new Color(0.65f, 0.68f, 0.72f, 0.75f);
            placeholder.fontStyle = FontStyle.Italic;

            InputField input = inputObject.GetComponent<InputField>();
            input.textComponent = valueText;
            input.placeholder = placeholder;
            input.characterLimit = 32;
            input.lineType = InputField.LineType.SingleLine;
            return input;
        }

        /// <summary>InputField 내부 Text가 부모 Rect 안쪽 여백을 제외하고 늘어나게 설정합니다.</summary>
        private static void StretchInputText(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(14f, 6f);
            rect.offsetMax = new Vector2(-14f, -6f);
        }

        /// <summary>공유 InputActions에 ActiveSkill 액션이 없다면 우클릭 바인딩과 함께 추가합니다.</summary>
        private static void EnsureActiveSkillInputAction()
        {
            InputActionAsset inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (inputActions == null)
                throw new FileNotFoundException("공유 InputActionAsset을 찾을 수 없습니다.", InputActionsPath);

            InputActionMap playerMap = inputActions.FindActionMap("Player", true);
            if (playerMap.FindAction("ActiveSkill", false) != null)
                return;

            InputAction activeSkill = playerMap.AddAction("ActiveSkill", InputActionType.Button);
            activeSkill.AddBinding("<Mouse>/rightButton", groups: "Keyboard&Mouse");
            File.WriteAllText(Path.GetFullPath(InputActionsPath), inputActions.ToJson());
            AssetDatabase.ImportAsset(InputActionsPath, ImportAssetOptions.ForceUpdate);
        }

        /// <summary>검기 테스트 재질과 프리팹을 생성하거나 기존 에셋을 다시 사용합니다.</summary>
        private static WarriorActiveSkillProjectile CreateSwordWavePrefab()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(SwordWaveMaterialPath);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
                material = new Material(shader) { color = new Color(0.25f, 0.9f, 1f, 0.85f) };
                AssetDatabase.CreateAsset(material, SwordWaveMaterialPath);
            }

            GameObject projectileObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            projectileObject.name = "Pfb_WarriorSwordWave";
            projectileObject.GetComponent<BoxCollider>().isTrigger = true;
            projectileObject.GetComponent<Renderer>().sharedMaterial = material;
            Rigidbody body = projectileObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            projectileObject.AddComponent<WarriorActiveSkillProjectile>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(projectileObject, SwordWavePrefabPath);
            Object.DestroyImmediate(projectileObject);
            return prefab.GetComponent<WarriorActiveSkillProjectile>();
        }

        /// <summary>궁수 테스트용 단색 화살 재질과 풀링 가능한 투사체 프리팹을 생성합니다.</summary>
        private static PlayerBasicAttackProjectile CreateArcherArrowPrefab()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(ArcherArrowMaterialPath);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
                material = new Material(shader) { color = new Color(0.95f, 0.8f, 0.2f, 1f) };
                AssetDatabase.CreateAsset(material, ArcherArrowMaterialPath);
            }

            GameObject projectileObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            projectileObject.name = "Pfb_ArcherArrow";
            projectileObject.GetComponent<BoxCollider>().isTrigger = true;
            projectileObject.GetComponent<Renderer>().sharedMaterial = material;
            Rigidbody body = projectileObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            projectileObject.AddComponent<PlayerBasicAttackProjectile>();

            PrefabUtility.SaveAsPrefabAsset(projectileObject, ArcherArrowPrefabPath);
            Object.DestroyImmediate(projectileObject);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(ArcherArrowPrefabPath, ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<PlayerBasicAttackProjectile>(ArcherArrowPrefabPath);
        }

        /// <summary>마법사 테스트용 단색 마법탄 재질과 스플래시가 켜진 투사체 프리팹을 생성합니다.</summary>
        private static PlayerBasicAttackProjectile CreateMageOrbPrefab()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MageOrbMaterialPath);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
                material = new Material(shader) { color = new Color(0.65f, 0.35f, 1f, 1f) };
                AssetDatabase.CreateAsset(material, MageOrbMaterialPath);
            }

            GameObject projectileObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectileObject.name = "Pfb_MageOrb";
            Collider primitiveCollider = projectileObject.GetComponent<Collider>();
            if (primitiveCollider != null)
                Object.DestroyImmediate(primitiveCollider);
            projectileObject.GetComponent<Renderer>().sharedMaterial = material;
            Rigidbody body = projectileObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            PlayerBasicAttackProjectile projectile = projectileObject.AddComponent<PlayerBasicAttackProjectile>();
            projectileObject.GetComponent<BoxCollider>().isTrigger = true;

            SerializedObject projectileObjectData = new SerializedObject(projectile);
            projectileObjectData.FindProperty("splashRadius").floatValue = 2.25f;
            projectileObjectData.FindProperty("splashDamageMultiplier").floatValue = 0.5f;
            projectileObjectData.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(projectileObject, MageOrbPrefabPath);
            Object.DestroyImmediate(projectileObject);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(MageOrbPrefabPath, ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<PlayerBasicAttackProjectile>(MageOrbPrefabPath);
        }

        /// <summary>마법사 액티브 스킬용 단색 파이어볼 재질과 스플래시가 켜진 투사체 프리팹을 생성합니다.</summary>
        private static PlayerBasicAttackProjectile CreateMageFireballPrefab()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MageFireballMaterialPath);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
                material = new Material(shader) { color = new Color(1f, 0.28f, 0.05f, 1f) };
                AssetDatabase.CreateAsset(material, MageFireballMaterialPath);
            }

            GameObject projectileObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectileObject.name = "Pfb_MageFireball";
            Collider primitiveCollider = projectileObject.GetComponent<Collider>();
            if (primitiveCollider != null)
                Object.DestroyImmediate(primitiveCollider);
            projectileObject.GetComponent<Renderer>().sharedMaterial = material;
            Rigidbody body = projectileObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            PlayerBasicAttackProjectile projectile = projectileObject.AddComponent<PlayerBasicAttackProjectile>();
            projectileObject.GetComponent<BoxCollider>().isTrigger = true;

            SerializedObject projectileObjectData = new SerializedObject(projectile);
            projectileObjectData.FindProperty("splashRadius").floatValue = 2.25f;
            projectileObjectData.FindProperty("splashDamageMultiplier").floatValue = 0.5f;
            projectileObjectData.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(projectileObject, MageFireballPrefabPath);
            Object.DestroyImmediate(projectileObject);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(MageFireballPrefabPath, ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<PlayerBasicAttackProjectile>(MageFireballPrefabPath);
        }

        /// <summary>궁수 액티브 스킬 테스트용 단색 관통 사격 재질과 풀링 가능한 프리팹을 생성합니다.</summary>
        private static ArcherActiveSkillProjectile CreateArcherPiercingShotPrefab()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(ArcherPiercingShotMaterialPath);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
                material = new Material(shader) { color = new Color(0.2f, 1f, 0.35f, 0.9f) };
                AssetDatabase.CreateAsset(material, ArcherPiercingShotMaterialPath);
            }

            GameObject projectileObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            projectileObject.name = "Pfb_ArcherPiercingShot";
            projectileObject.GetComponent<BoxCollider>().isTrigger = true;
            projectileObject.GetComponent<Renderer>().sharedMaterial = material;
            Rigidbody body = projectileObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            projectileObject.AddComponent<ArcherActiveSkillProjectile>();

            PrefabUtility.SaveAsPrefabAsset(projectileObject, ArcherPiercingShotPrefabPath);
            Object.DestroyImmediate(projectileObject);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(ArcherPiercingShotPrefabPath, ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<ArcherActiveSkillProjectile>(ArcherPiercingShotPrefabPath);
        }

        /// <summary>궁수 원거리 기본 공격과 관통 사격이 사용할 SO를 만들고 테스트 수치를 기록합니다.</summary>
        private static PlayerStatsData LoadOrCreateArcherStats()
        {
            PlayerStatsData stats = AssetDatabase.LoadAssetAtPath<PlayerStatsData>(ArcherStatsPath);
            if (stats == null)
            {
                stats = ScriptableObject.CreateInstance<PlayerStatsData>();
                AssetDatabase.CreateAsset(stats, ArcherStatsPath);
            }

            SerializedObject statsObject = new SerializedObject(stats);
            statsObject.FindProperty("jobId").stringValue = "Archer";
            statsObject.FindProperty("basicAttackType").enumValueIndex = (int)BasicAttackType.Projectile;
            statsObject.FindProperty("basicAttackRange").floatValue = 8f;
            statsObject.FindProperty("basicAttackWidth").floatValue = 0.25f;
            statsObject.FindProperty("basicAttackProjectileSpeed").floatValue = 18f;
            statsObject.FindProperty("activeSkillCooldown").floatValue = 10f;
            statsObject.FindProperty("activeSkillCastTime").floatValue = 0.2f;
            statsObject.FindProperty("activeSkillDamage").floatValue = 70f;
            statsObject.FindProperty("activeSkillProjectileCount").intValue = 1;
            statsObject.FindProperty("activeSkillProjectileInterval").floatValue = 0f;
            statsObject.FindProperty("activeSkillProjectileSpeed").floatValue = 30f;
            statsObject.FindProperty("activeSkillProjectileWidth").floatValue = 1f;
            statsObject.FindProperty("activeSkillRange").floatValue = 20f;
            statsObject.FindProperty("activeSkillMaxHitTargets").intValue = 999;
            statsObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(stats);
            return stats;
        }

        /// <summary>마법사 원거리 기본 공격이 사용할 SO를 만들고 스플래시 테스트용 기본 수치를 기록합니다.</summary>
        private static PlayerStatsData LoadOrCreateMageStats()
        {
            PlayerStatsData stats = AssetDatabase.LoadAssetAtPath<PlayerStatsData>(MageStatsPath);
            if (stats == null)
            {
                stats = ScriptableObject.CreateInstance<PlayerStatsData>();
                AssetDatabase.CreateAsset(stats, MageStatsPath);
            }

            SerializedObject statsObject = new SerializedObject(stats);
            statsObject.FindProperty("jobId").stringValue = "Mage";
            statsObject.FindProperty("basicAttackType").enumValueIndex = (int)BasicAttackType.Projectile;
            statsObject.FindProperty("attackPower").floatValue = 12f;
            statsObject.FindProperty("attacksPerSecond").floatValue = 1f;
            statsObject.FindProperty("basicAttackRange").floatValue = 9f;
            statsObject.FindProperty("basicAttackWidth").floatValue = 0.45f;
            statsObject.FindProperty("basicAttackProjectileSpeed").floatValue = 14f;
            statsObject.FindProperty("activeSkillCooldown").floatValue = 10f;
            statsObject.FindProperty("activeSkillCastTime").floatValue = 0.2f;
            statsObject.FindProperty("activeSkillDamage").floatValue = 18f;
            statsObject.FindProperty("activeSkillProjectileCount").intValue = 5;
            statsObject.FindProperty("activeSkillProjectileInterval").floatValue = 0f;
            statsObject.FindProperty("activeSkillProjectileSpeed").floatValue = 16f;
            statsObject.FindProperty("activeSkillProjectileWidth").floatValue = 0.55f;
            statsObject.FindProperty("activeSkillRange").floatValue = 12f;
            statsObject.FindProperty("activeSkillMaxHitTargets").intValue = 999;
            statsObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(stats);
            return stats;
        }

        /// <summary>궁수 기본 공격 컨트롤러의 직렬화 필드에 화살 프리팹을 연결합니다.</summary>
        private static void AssignBasicAttackProjectile(
            PlayerBasicAttackController basicAttack,
            PlayerBasicAttackProjectile projectilePrefab)
        {
            SerializedObject attackObject = new SerializedObject(basicAttack);
            attackObject.Update();
            attackObject.FindProperty("projectilePrefab").objectReferenceValue = projectilePrefab;
            attackObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(basicAttack);
        }

        /// <summary>마법사 액티브 스킬 컨트롤러의 직렬화 필드에 파이어볼 프리팹을 연결합니다.</summary>
        private static void AssignMageActiveSkillPrefab(
            MageActiveSkillController skill,
            PlayerBasicAttackProjectile projectilePrefab)
        {
            SerializedObject skillObject = new SerializedObject(skill);
            skillObject.Update();
            skillObject.FindProperty("projectilePrefab").objectReferenceValue = projectilePrefab;
            skillObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(skill);
        }

        /// <summary>전사 액티브 스킬 컨트롤러의 직렬화 필드에 검기 프리팹을 연결합니다.</summary>
        private static void AssignWarriorActiveSkillPrefab(
            WarriorActiveSkillController skill,
            WarriorActiveSkillProjectile projectilePrefab)
        {
            SerializedObject skillObject = new SerializedObject(skill);
            skillObject.Update();
            skillObject.FindProperty("projectilePrefab").objectReferenceValue = projectilePrefab;
            skillObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(skill);
        }

        /// <summary>궁수 액티브 스킬 컨트롤러의 직렬화 필드에 관통 사격 프리팹을 연결합니다.</summary>
        private static void AssignArcherPiercingShotPrefab(
            ArcherActiveSkillController skill,
            ArcherActiveSkillProjectile projectilePrefab)
        {
            SerializedObject skillObject = new SerializedObject(skill);
            skillObject.Update();
            skillObject.FindProperty("projectilePrefab").objectReferenceValue = projectilePrefab;
            skillObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(skill);
        }

        /// <summary>자동 조준·수동 빈 공간 발사·벽 반환을 한 씬에서 확인할 궁수용 배치를 만듭니다.</summary>
        private static void CreateArcherAttackTargets()
        {
            GameObject oldRoot = GameObject.Find("Targets_BasicAttackTest");
            if (oldRoot != null)
                Object.DestroyImmediate(oldRoot);

            GameObject root = new GameObject("Targets_ArcherBasicAttackTest");
            CreateAttackDummy(root.transform, "ArcherDummy_01_Near", new Vector3(0f, 1f, 4f));
            CreateAttackDummy(root.transform, "ArcherDummy_02_Left", new Vector3(-4f, 1f, 5f));
            CreateAttackDummy(root.transform, "ArcherDummy_03_Right", new Vector3(4f, 1f, 5f));
            CreateAttackDummy(root.transform, "ArcherDummy_04_OutsideRange", new Vector3(0f, 1f, 9.5f));

            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "ArcherArrowWall_01";
            wall.transform.SetParent(root.transform);
            wall.transform.position = new Vector3(6f, 1.5f, 2f);
            wall.transform.localScale = new Vector3(0.5f, 3f, 4f);
        }

        /// <summary>10단계 테스트 씬의 조작법과 기대 결과를 화면 안내에 기록합니다.</summary>
        private static void UpdateArcherAttackTestGuide()
        {
            GameObject guideObject = GameObject.Find("Txt_TestGuide");
            if (guideObject == null || !guideObject.TryGetComponent(out Text guide))
                return;

            guide.text = "Test_10_ArcherBasicAttack\n자동: 8m 안의 가장 가까운 적에게 화살 발사\n좌클릭 유지: 빈 공간에도 수동 발사 / 적·벽·사거리 끝에서 풀 반환";
        }

        /// <summary>마법탄의 직접 피해, 50% 스플래시 피해, 범위 밖 미적중을 확인할 더미를 배치합니다.</summary>
        private static void CreateMageAttackTargets()
        {
            GameObject oldArcherTargets = GameObject.Find("Targets_ArcherBasicAttackTest");
            if (oldArcherTargets != null)
                Object.DestroyImmediate(oldArcherTargets);

            GameObject oldTargets = GameObject.Find("Targets_MageBasicAttackTest");
            if (oldTargets != null)
                Object.DestroyImmediate(oldTargets);

            GameObject oldArrowWall = GameObject.Find("ArcherArrowWall_01");
            if (oldArrowWall != null)
                Object.DestroyImmediate(oldArrowWall);

            GameObject root = new GameObject("Targets_MageBasicAttackTest");
            CreateAttackDummy(root.transform, "MageDummy_01_DirectHit", new Vector3(0f, 1f, 4f));
            CreateAttackDummy(root.transform, "MageDummy_02_SplashLeft", new Vector3(-1.4f, 1f, 4.4f));
            CreateAttackDummy(root.transform, "MageDummy_03_SplashBack", new Vector3(0f, 1f, 5.8f));
            CreateAttackDummy(root.transform, "MageDummy_04_OutsideSplash", new Vector3(3.2f, 1f, 4f));
            CreateAttackDummy(root.transform, "MageDummy_05_ManualAimTarget", new Vector3(-5f, 1f, 5.5f));

            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "MageOrbWall_01";
            wall.transform.SetParent(root.transform);
            wall.transform.position = new Vector3(5.5f, 1.5f, 2.5f);
            wall.transform.localScale = new Vector3(0.5f, 3f, 4f);
        }

        /// <summary>12단계 마법사 테스트 씬의 조작법과 기대 결과를 화면 안내에 기록합니다.</summary>
        private static void UpdateMageAttackTestGuide()
        {
            GameObject guideObject = GameObject.Find("Txt_TestGuide");
            if (guideObject == null || !guideObject.TryGetComponent(out Text guide))
                return;

            guide.text = "Test_12_MageBasicAttack\n자동: 9m 안의 가장 가까운 적에게 마법탄 발사\n직접 적중: 피해 12 / 폭발 반경 2.25m 주변 적: 피해 6\n좌클릭 유지: 빈 공간에도 수동 발사 / 벽 충돌 시 폭발 없이 소멸";
        }

        /// <summary>마법사 액티브 스킬의 부채꼴 발사, 직접 피해, 스플래시 피해를 확인할 더미를 배치합니다.</summary>
        private static void CreateMageActiveSkillTargets()
        {
            GameObject oldBasicTargets = GameObject.Find("Targets_MageBasicAttackTest");
            if (oldBasicTargets != null)
                Object.DestroyImmediate(oldBasicTargets);

            GameObject oldTargets = GameObject.Find("Targets_MageActiveSkillTest");
            if (oldTargets != null)
                Object.DestroyImmediate(oldTargets);

            GameObject oldWall = GameObject.Find("MageOrbWall_01");
            if (oldWall != null)
                Object.DestroyImmediate(oldWall);

            GameObject root = new GameObject("Targets_MageActiveSkillTest");
            CreateAttackDummy(root.transform, "MageActiveDummy_01_Left30", new Vector3(-3.4f, 1f, 6f));
            CreateAttackDummy(root.transform, "MageActiveDummy_02_Left15", new Vector3(-1.6f, 1f, 6.3f));
            CreateAttackDummy(root.transform, "MageActiveDummy_03_Center", new Vector3(0f, 1f, 6.5f));
            CreateAttackDummy(root.transform, "MageActiveDummy_04_Right15", new Vector3(1.6f, 1f, 6.3f));
            CreateAttackDummy(root.transform, "MageActiveDummy_05_Right30", new Vector3(3.4f, 1f, 6f));
            CreateAttackDummy(root.transform, "MageActiveDummy_06_CenterSplash", new Vector3(0.9f, 1f, 6.8f));
            CreateAttackDummy(root.transform, "MageActiveDummy_07_OutsideFan", new Vector3(5.6f, 1f, 6f));

            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "MageFireballWall_01";
            wall.transform.SetParent(root.transform);
            wall.transform.position = new Vector3(0f, 1.5f, 11.5f);
            wall.transform.localScale = new Vector3(5f, 3f, 0.5f);
        }

        /// <summary>마법사 액티브 스킬 테스트 전용 조준선·상태 Text·쿨타임 제거 버튼을 생성합니다.</summary>
        private static void CreateMageActiveSkillDebugView(PlayerController player, MageActiveSkillController skill)
        {
            GameObject oldView = GameObject.Find("MageActiveSkillDebugView");
            if (oldView != null)
                Object.DestroyImmediate(oldView);

            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            Text status = CreateUiText(canvas.transform, "Txt_MageActiveSkillDebug", new Vector2(24f, -300f), new Vector2(660f, 230f), 21, TextAnchor.UpperLeft);
            GameObject viewObject = new GameObject("MageActiveSkillDebugView", typeof(LineRenderer), typeof(MageActiveSkillDebugView));
            MageActiveSkillDebugView view = viewObject.GetComponent<MageActiveSkillDebugView>();
            SerializedObject serializedView = new SerializedObject(view);
            serializedView.FindProperty("skill").objectReferenceValue = skill;
            serializedView.FindProperty("player").objectReferenceValue = player;
            serializedView.FindProperty("output").objectReferenceValue = status;
            serializedView.ApplyModifiedPropertiesWithoutUndo();

            Button noCooldownButton = CreateUiButton(canvas.transform, "Btn_MageActiveSkillNoCooldown", "파이어볼 쿨타임 제거", new Vector2(24f, 300f));
            RectTransform buttonRect = noCooldownButton.GetComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(230f, 48f);
            UnityEventTools.AddPersistentListener(noCooldownButton.onClick, view.ToggleNoCooldown);
        }

        /// <summary>13단계 테스트 씬의 조작법과 기대 결과를 화면 안내에 기록합니다.</summary>
        private static void UpdateMageActiveSkillGuide()
        {
            GameObject guideObject = GameObject.Find("Txt_TestGuide");
            if (guideObject == null || !guideObject.TryGetComponent(out Text guide))
                return;

            guide.text = "Test_13_MageActiveSkill\n우클릭 유지: 중앙 방향 조준 / 우클릭 해제: 좌우 30도 총 60도 부채꼴 파이어볼 5발\n직접 적중: 피해 18 / 폭발 반경 2.25m 주변 적: 피해 9 / 벽 충돌 시 폭발 없이 소멸";
        }

        /// <summary>14단계 테스트 씬의 조작법과 기대 결과를 화면 안내에 기록합니다.</summary>
        private static void UpdateJobSwitchGuide()
        {
            GameObject guideObject = GameObject.Find("Txt_TestGuide");
            if (guideObject == null || !guideObject.TryGetComponent(out Text guide))
                return;

            guide.text =
                "Test_14_JobSwitchCombatDebug\n" +
                "오른쪽 버튼: 전사/궁수/마법사 실시간 직업 변경\n" +
                "좌클릭 유지: 현재 직업 기본공격 수동 조준 / 우클릭 유지 후 해제: 현재 직업 액티브 스킬\n" +
                "전환 시 SO·기본공격 프리팹·액티브 스킬 컨트롤러·풀 상태가 함께 바뀌는지 확인";
        }

        /// <summary>세 직업의 근접, 직선, 부채꼴, 스플래시 공격을 한 공간에서 확인할 더미와 벽을 배치합니다.</summary>
        private static void CreateJobSwitchTargets()
        {
            string[] oldRoots =
            {
                "Targets_BasicAttackTest",
                "Targets_ArcherBasicAttackTest",
                "Targets_ArcherActiveSkillTest",
                "Targets_MageBasicAttackTest",
                "Targets_MageActiveSkillTest",
                "Targets_JobSwitchCombatTest"
            };

            foreach (string rootName in oldRoots)
            {
                GameObject oldRoot = GameObject.Find(rootName);
                if (oldRoot != null)
                    Object.DestroyImmediate(oldRoot);
            }

            GameObject root = new GameObject("Targets_JobSwitchCombatTest");
            CreateAttackDummy(root.transform, "JobDummy_01_WarriorNear", new Vector3(0f, 1f, 1.35f));
            CreateAttackDummy(root.transform, "JobDummy_02_LineA", new Vector3(0f, 1f, 4f));
            CreateAttackDummy(root.transform, "JobDummy_03_LineB", new Vector3(0f, 1f, 6.5f));
            CreateAttackDummy(root.transform, "JobDummy_04_LineC", new Vector3(0f, 1f, 9f));
            CreateAttackDummy(root.transform, "JobDummy_05_LeftFan", new Vector3(-2.4f, 1f, 6.5f));
            CreateAttackDummy(root.transform, "JobDummy_06_RightFan", new Vector3(2.4f, 1f, 6.5f));
            CreateAttackDummy(root.transform, "JobDummy_07_SplashNear", new Vector3(1.2f, 1f, 4.3f));
            CreateAttackDummy(root.transform, "JobDummy_08_Outside", new Vector3(5.5f, 1f, 8f));

            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "JobSwitchSkillWall_01";
            wall.transform.SetParent(root.transform);
            wall.transform.position = new Vector3(0f, 1.5f, 14f);
            wall.transform.localScale = new Vector3(7f, 3f, 0.5f);
        }

        /// <summary>직업 변경 버튼, 현재 상태 Text, 쿨타임 제거·초기화 버튼을 가진 테스트 패널을 만듭니다.</summary>
        private static void CreateJobSwitchDebugPanel(
            PlayerController player,
            PlayerBasicAttackController basicAttack,
            WarriorActiveSkillController warriorSkill,
            ArcherActiveSkillController archerSkill,
            MageActiveSkillController mageSkill,
            PlayerStatsData warriorStats,
            PlayerStatsData archerStats,
            PlayerStatsData mageStats,
            PlayerBasicAttackProjectile archerBasicAttackPrefab,
            PlayerBasicAttackProjectile mageBasicAttackPrefab)
        {
            if (warriorStats == null || archerStats == null || mageStats == null
                || archerBasicAttackPrefab == null || mageBasicAttackPrefab == null)
            {
                string missing =
                    $"WarriorStats:{warriorStats == null}, " +
                    $"ArcherStats:{archerStats == null}, " +
                    $"MageStats:{mageStats == null}, " +
                    $"ArcherBasic:{archerBasicAttackPrefab == null}, " +
                    $"MageBasic:{mageBasicAttackPrefab == null}";
                Debug.LogWarning("직업 변경 디버그 패널 일부 참조가 비어 있습니다. 에디터 Play 중 fallback으로 다시 연결합니다. " + missing);
            }

            GameObject oldPanel = GameObject.Find("Pnl_JobSwitchDebug");
            if (oldPanel != null)
                Object.DestroyImmediate(oldPanel);

            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
                return;

            GameObject panel = new GameObject("Pnl_JobSwitchDebug", typeof(RectTransform), typeof(Image), typeof(PlayerJobSwitchDebugPanel));
            panel.transform.SetParent(canvas.transform, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(560f, 0f);
            panel.GetComponent<Image>().color = new Color(0.025f, 0.035f, 0.055f, 0.94f);

            Text title = CreateUiText(panel.transform, "Txt_JobSwitchTitle", new Vector2(20f, -20f), new Vector2(520f, 42f), 26, TextAnchor.UpperLeft);
            title.text = "직업 실시간 변경";

            Text output = CreateUiText(panel.transform, "Txt_JobSwitchStatus", new Vector2(20f, -70f), new Vector2(520f, 330f), 19, TextAnchor.UpperLeft);
            output.text = "Play 후 직업 버튼을 눌러 전투 모듈 전환을 확인하세요.";

            PlayerJobSwitchDebugPanel debugPanel = panel.GetComponent<PlayerJobSwitchDebugPanel>();
            debugPanel.ConfigureForTestScene(
                player,
                basicAttack,
                warriorSkill,
                archerSkill,
                mageSkill,
                warriorStats,
                archerStats,
                mageStats,
                archerBasicAttackPrefab,
                mageBasicAttackPrefab,
                output);
            EditorUtility.SetDirty(debugPanel);

            Button warriorButton = CreateUiButton(panel.transform, "Btn_SwitchWarrior", "전사", new Vector2(20f, 245f));
            Button archerButton = CreateUiButton(panel.transform, "Btn_SwitchArcher", "궁수", new Vector2(160f, 245f));
            Button mageButton = CreateUiButton(panel.transform, "Btn_SwitchMage", "마법사", new Vector2(300f, 245f));
            Button noCooldownButton = CreateUiButton(panel.transform, "Btn_CurrentActiveNoCooldown", "현재 스킬 쿨타임 제거", new Vector2(20f, 180f));
            Button resetButton = CreateUiButton(panel.transform, "Btn_ResetCurrentJob", "현재 직업 원본 초기화", new Vector2(260f, 180f));
            noCooldownButton.GetComponent<RectTransform>().sizeDelta = new Vector2(220f, 48f);
            resetButton.GetComponent<RectTransform>().sizeDelta = new Vector2(230f, 48f);

            UnityEventTools.AddPersistentListener(warriorButton.onClick, debugPanel.SwitchToWarrior);
            UnityEventTools.AddPersistentListener(archerButton.onClick, debugPanel.SwitchToArcher);
            UnityEventTools.AddPersistentListener(mageButton.onClick, debugPanel.SwitchToMage);
            UnityEventTools.AddPersistentListener(noCooldownButton.onClick, debugPanel.ToggleCurrentSkillNoCooldown);
            UnityEventTools.AddPersistentListener(resetButton.onClick, debugPanel.ResetCurrentJob);
        }

        /// <summary>새 테스트 씬에 사용할 Cinemachine Orbital Follow 옵션을 요청값으로 맞춥니다.</summary>
        private static void ApplyPreferredCinemachineOptions()
        {
            CinemachineOrbitalFollow orbitalFollow = Object.FindFirstObjectByType<CinemachineOrbitalFollow>();
            if (orbitalFollow == null)
                return;

            orbitalFollow.TargetOffset = Vector3.zero;
            orbitalFollow.Radius = 25f;
            orbitalFollow.OrbitStyle = CinemachineOrbitalFollow.OrbitStyles.Sphere;
            orbitalFollow.RecenteringTarget = CinemachineOrbitalFollow.ReferenceFrames.TrackingTarget;

            TrackerSettings trackerSettings = orbitalFollow.TrackerSettings;
            trackerSettings.BindingMode = BindingMode.WorldSpace;
            trackerSettings.PositionDamping = Vector3.zero;
            orbitalFollow.TrackerSettings = trackerSettings;

            if (orbitalFollow.GetComponent<CinemachineHardLookAt>() == null)
                orbitalFollow.gameObject.AddComponent<CinemachineHardLookAt>();
        }

        /// <summary>긴 사거리 테스트를 위해 바닥과 외곽 벽을 확장하고 전방 벽을 뒤로 옮깁니다.</summary>
        private static void ExpandArcherActiveSkillTestArea()
        {
            GameObject floor = GameObject.Find("Floor_01");
            if (floor != null)
                floor.transform.localScale = new Vector3(28f, 0.5f, 46f);

            GameObject frontWall = GameObject.Find("Wall_01");
            if (frontWall != null)
            {
                frontWall.transform.position = new Vector3(0f, 1f, 23f);
                frontWall.transform.localScale = new Vector3(28f, 2f, 0.5f);
            }

            GameObject backWall = GameObject.Find("Wall_02");
            if (backWall != null)
            {
                backWall.transform.position = new Vector3(0f, 1f, -12f);
                backWall.transform.localScale = new Vector3(28f, 2f, 0.5f);
            }

            GameObject rightWall = GameObject.Find("Wall_03");
            if (rightWall != null)
            {
                rightWall.transform.position = new Vector3(14f, 1f, 5f);
                rightWall.transform.localScale = new Vector3(0.5f, 2f, 36f);
            }

            GameObject leftWall = GameObject.Find("Wall_04");
            if (leftWall != null)
            {
                leftWall.transform.position = new Vector3(-14f, 1f, 5f);
                leftWall.transform.localScale = new Vector3(0.5f, 2f, 36f);
            }
        }

        /// <summary>관통 사격이 긴 직선상의 모든 적을 맞히고, 옆 더미와 벽 충돌을 구분하도록 배치합니다.</summary>
        private static void CreateArcherActiveSkillTargets()
        {
            ExpandArcherActiveSkillTestArea();

            GameObject oldBasicTargets = GameObject.Find("Targets_ArcherBasicAttackTest");
            if (oldBasicTargets != null)
                Object.DestroyImmediate(oldBasicTargets);

            GameObject oldTargets = GameObject.Find("Targets_ArcherActiveSkillTest");
            if (oldTargets != null)
                Object.DestroyImmediate(oldTargets);

            GameObject oldArrowWall = GameObject.Find("ArcherArrowWall_01");
            if (oldArrowWall != null)
                Object.DestroyImmediate(oldArrowWall);

            GameObject root = new GameObject("Targets_ArcherActiveSkillTest");
            for (int i = 0; i < 8; i++)
            {
                float z = 9f + (i * 1.2f);
                CreateAttackDummy(root.transform, $"PiercingShotDummy_{i + 1:00}_Line", new Vector3(0f, 1f, z));
            }

            CreateAttackDummy(root.transform, "PiercingShotDummy_09_SideOutsideWidth", new Vector3(1.4f, 1f, 12f));
            CreateAttackDummy(root.transform, "PiercingShotDummy_10_BehindWall", new Vector3(0f, 1f, 20.5f));

            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "PiercingShotWall_01";
            wall.transform.SetParent(root.transform);
            wall.transform.position = new Vector3(0f, 1.5f, 18.5f);
            wall.transform.localScale = new Vector3(4f, 3f, 0.5f);
        }

        /// <summary>관통 사격 테스트 전용 조준선·상태 Text·쿨타임 제거 버튼을 생성합니다.</summary>
        private static void CreateArcherActiveSkillDebugView(PlayerController player, ArcherActiveSkillController skill)
        {
            GameObject oldView = GameObject.Find("ArcherActiveSkillDebugView");
            if (oldView != null)
                Object.DestroyImmediate(oldView);

            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            Text status = CreateUiText(canvas.transform, "Txt_ArcherActiveSkillDebug", new Vector2(24f, -300f), new Vector2(620f, 210f), 21, TextAnchor.UpperLeft);
            GameObject viewObject = new GameObject("ArcherActiveSkillDebugView", typeof(LineRenderer), typeof(ArcherActiveSkillDebugView));
            ArcherActiveSkillDebugView view = viewObject.GetComponent<ArcherActiveSkillDebugView>();
            SerializedObject serializedView = new SerializedObject(view);
            serializedView.FindProperty("skill").objectReferenceValue = skill;
            serializedView.FindProperty("player").objectReferenceValue = player;
            serializedView.FindProperty("output").objectReferenceValue = status;
            serializedView.ApplyModifiedPropertiesWithoutUndo();

            Button noCooldownButton = CreateUiButton(canvas.transform, "Btn_ArcherActiveSkillNoCooldown", "관통 사격 쿨타임 제거", new Vector2(24f, 300f));
            RectTransform buttonRect = noCooldownButton.GetComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(230f, 48f);
            UnityEventTools.AddPersistentListener(noCooldownButton.onClick, view.ToggleNoCooldown);
        }

        /// <summary>11단계 테스트 씬의 조작법과 기대 결과를 화면 안내에 기록합니다.</summary>
        private static void UpdateArcherActiveSkillGuide()
        {
            GameObject guideObject = GameObject.Find("Txt_TestGuide");
            if (guideObject == null || !guideObject.TryGetComponent(out Text guide))
                return;

            guide.text = "Test_11_ArcherActiveSkill\n우클릭 유지: 조준 / 우클릭 해제: 0.2초 후 관통 사격 1발\n피해 70 / 폭 1m / 사거리 20m / 적 관통 제한 없음 / 벽 충돌 시 소멸";
        }

        private static void CreateSwordWaveTargets()
        {
            GameObject oldBasicTargets = GameObject.Find("Targets_BasicAttackTest");
            if (oldBasicTargets != null)
                Object.DestroyImmediate(oldBasicTargets);

            GameObject oldTargets = GameObject.Find("Targets_SwordWaveTest");
            if (oldTargets != null)
                Object.DestroyImmediate(oldTargets);

            GameObject root = new GameObject("Targets_SwordWaveTest");
            for (int i = 0; i < 12; i++)
            {
                float z = 3f + (i * 1.1f);
                CreateAttackDummy(root.transform, $"SwordWaveDummy_{i + 1:00}", new Vector3(0f, 1f, z));
            }

            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "SwordWaveWall_01";
            wall.transform.SetParent(root.transform);
            wall.transform.position = new Vector3(4f, 1.5f, 9f);
            wall.transform.localScale = new Vector3(4f, 3f, 0.5f);
        }

        private static void CreateWarriorActiveSkillDebugView(PlayerController player, WarriorActiveSkillController skill)
        {
            GameObject oldView = GameObject.Find("WarriorActiveSkillDebugView");
            if (oldView != null)
                Object.DestroyImmediate(oldView);

            oldView = GameObject.Find("SwordWaveDebugView");
            if (oldView != null)
                Object.DestroyImmediate(oldView);

            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            Text status = CreateUiText(canvas.transform, "Txt_WarriorActiveSkillDebug", new Vector2(24f, -300f), new Vector2(520f, 170f), 22, TextAnchor.UpperLeft);
            GameObject viewObject = new GameObject("WarriorActiveSkillDebugView", typeof(LineRenderer), typeof(WarriorActiveSkillDebugView));
            WarriorActiveSkillDebugView view = viewObject.GetComponent<WarriorActiveSkillDebugView>();
            SerializedObject serializedView = new SerializedObject(view);
            serializedView.FindProperty("skill").objectReferenceValue = skill;
            serializedView.FindProperty("player").objectReferenceValue = player;
            serializedView.FindProperty("output").objectReferenceValue = status;
            serializedView.ApplyModifiedPropertiesWithoutUndo();

            Button noCooldownButton = CreateUiButton(canvas.transform, "Btn_WarriorActiveSkillNoCooldown", "검기 쿨타임 제거", new Vector2(24f, 300f));
            RectTransform buttonRect = noCooldownButton.GetComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(190f, 48f);
            UnityEventTools.AddPersistentListener(noCooldownButton.onClick, view.ToggleNoCooldown);
        }

        private static void UpdateSwordWaveTestGuide()
        {
            GameObject guideObject = GameObject.Find("Txt_TestGuide");
            if (guideObject == null || !guideObject.TryGetComponent(out Text guide))
                return;

            guide.text = "Test_06_SwordWave\n우클릭 유지: 조준 / 우클릭 해제: 0.2초 후 검기 3연발\n관통 최대 10개 대상 / 벽 충돌 시 소멸";
        }

        private static void EnsureCombatInputActions()
        {
            InputActionAsset inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (inputActions == null)
                throw new FileNotFoundException("공유 InputActionAsset을 찾을 수 없습니다.", InputActionsPath);

            InputActionMap playerMap = inputActions.FindActionMap("Player", true);
            bool changed = false;

            if (playerMap.FindAction("AimPosition", false) == null)
            {
                InputAction aimPosition = playerMap.AddAction("AimPosition", InputActionType.PassThrough, expectedControlLayout: "Vector2");
                aimPosition.AddBinding("<Mouse>/position", groups: "Keyboard&Mouse");
                changed = true;
            }

            if (changed)
            {
                File.WriteAllText(Path.GetFullPath(InputActionsPath), inputActions.ToJson());
                AssetDatabase.ImportAsset(InputActionsPath, ImportAssetOptions.ForceUpdate);
            }
        }

        private static void CreateAttackDummies()
        {
            GameObject oldRoot = GameObject.Find("Targets_BasicAttackTest");
            if (oldRoot != null)
                Object.DestroyImmediate(oldRoot);

            GameObject root = new GameObject("Targets_BasicAttackTest");
            CreateAttackDummy(root.transform, "Dummy_01_Near", new Vector3(0f, 1f, 1.25f));
            CreateAttackDummy(root.transform, "Dummy_02_Left", new Vector3(-1.1f, 1f, 0.6f));
            CreateAttackDummy(root.transform, "Dummy_03_Right", new Vector3(1.1f, 1f, 0.6f));
            CreateAttackDummy(root.transform, "Dummy_04_OutsideRange", new Vector3(0f, 1f, 3.5f));
        }

        private static void CreateAttackDummy(Transform parent, string name, Vector3 position)
        {
            GameObject dummy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            dummy.name = name;
            dummy.transform.SetParent(parent);
            dummy.transform.position = position;
            dummy.transform.localScale = new Vector3(0.65f, 0.65f, 0.65f);
            dummy.GetComponent<Collider>().isTrigger = true;
            dummy.AddComponent<PlayerTargetDummy>();
        }

        private static void CreateAttackDebugView(PlayerController player, PlayerBasicAttackController attackController)
        {
            GameObject oldView = GameObject.Find("AttackDebugView");
            if (oldView != null)
                Object.DestroyImmediate(oldView);

            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            Text status = CreateUiText(canvas.transform, "Txt_AttackDebug", new Vector2(24f, -160f), new Vector2(520f, 120f), 22, TextAnchor.UpperLeft);

            GameObject viewObject = new GameObject("AttackDebugView", typeof(LineRenderer), typeof(PlayerAttackDebugView));
            PlayerAttackDebugView view = viewObject.GetComponent<PlayerAttackDebugView>();
            SerializedObject serializedView = new SerializedObject(view);
            serializedView.FindProperty("attackController").objectReferenceValue = attackController;
            serializedView.FindProperty("player").objectReferenceValue = player;
            serializedView.FindProperty("output").objectReferenceValue = status;
            serializedView.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void UpdateAttackTestGuide()
        {
            GameObject guideObject = GameObject.Find("Txt_TestGuide");
            if (guideObject == null || !guideObject.TryGetComponent(out Text guide))
                return;

            guide.text = "Test_05_AimBasicAttack\n자동: 1.5m 안의 가장 가까운 더미 공격\n좌클릭 유지: 마우스 방향 수동 공격 (대상 없어도 발동)";
        }

        /// <summary>EventSystem이 없다면 Input System UI 모듈과 함께 생성합니다.</summary>
        private static void EnsureEventSystem()
        {
            EventSystem existing = Object.FindFirstObjectByType<EventSystem>();
            if (existing != null)
                return;

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        private static void CreateHealthDebugUi(PlayerController player)
        {
            GameObject oldPanel = GameObject.Find("Pnl_HealthDebug");
            if (oldPanel != null)
                Object.DestroyImmediate(oldPanel);

            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            GameObject panel = new GameObject("Pnl_HealthDebug", typeof(RectTransform), typeof(Image), typeof(PlayerHealthDebugView));
            panel.transform.SetParent(canvas.transform, false);

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 0f);
            panelRect.anchorMax = new Vector2(0f, 0f);
            panelRect.pivot = new Vector2(0f, 0f);
            panelRect.anchoredPosition = new Vector2(24f, 24f);
            panelRect.sizeDelta = new Vector2(460f, 260f);
            panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);

            Text status = CreateUiText(panel.transform, "Txt_HealthStatus", new Vector2(20f, -20f), new Vector2(420f, 100f), 22, TextAnchor.UpperLeft);
            Button damageButton = CreateUiButton(panel.transform, "Btn_Damage10", "피해 10", new Vector2(20f, 70f));
            Button invincibleButton = CreateUiButton(panel.transform, "Btn_ToggleInvincible", "무적 토글", new Vector2(165f, 70f));
            Button restartButton = CreateUiButton(panel.transform, "Btn_Restart", "재시작", new Vector2(310f, 70f));

            PlayerHealthDebugView view = panel.GetComponent<PlayerHealthDebugView>();
            SerializedObject viewObject = new SerializedObject(view);
            viewObject.FindProperty("player").objectReferenceValue = player;
            viewObject.FindProperty("health").objectReferenceValue = player.GetComponent<PlayerHealthController>();
            viewObject.FindProperty("output").objectReferenceValue = status;
            viewObject.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddPersistentListener(damageButton.onClick, view.ApplyTestDamage);
            UnityEventTools.AddPersistentListener(invincibleButton.onClick, view.ToggleDebugInvincibility);
            UnityEventTools.AddPersistentListener(restartButton.onClick, view.ResetPlayer);
        }

        /// <summary>공통 스타일의 UGUI Text를 지정 부모와 위치에 생성합니다.</summary>
        private static Text CreateUiText(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, int fontSize, TextAnchor alignment)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = alignment;
            return text;
        }

        /// <summary>공통 스타일의 UGUI Button과 가운데 Label Text를 생성합니다.</summary>
        private static Button CreateUiButton(Transform parent, string name, string label, Vector2 anchoredPosition)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(130f, 48f);
            buttonObject.GetComponent<Image>().color = new Color(0.2f, 0.25f, 0.3f, 1f);

            Text labelText = CreateUiText(buttonObject.transform, "Txt_Label", Vector2.zero, new Vector2(130f, 48f), 19, TextAnchor.MiddleCenter);
            RectTransform labelRect = labelText.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.anchoredPosition = Vector2.zero;
            labelRect.sizeDelta = Vector2.zero;
            labelText.text = label;
            return buttonObject.GetComponent<Button>();
        }

        private static void CreateDamageZone()
        {
            GameObject existing = GameObject.Find("DamageZone_01");
            if (existing != null)
                Object.DestroyImmediate(existing);

            GameObject zone = GameObject.CreatePrimitive(PrimitiveType.Cube);
            zone.name = "DamageZone_01";
            zone.transform.position = new Vector3(0f, 0.1f, 5f);
            zone.transform.localScale = new Vector3(5f, 0.2f, 3f);

            BoxCollider collider = zone.GetComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(1f, 10f, 1f);
            Rigidbody body = zone.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            zone.AddComponent<PlayerDamageZone>();
            zone.GetComponent<Renderer>().material.color = new Color(0.8f, 0.1f, 0.1f, 1f);
        }

        private static void UpdateHealthTestGuide()
        {
            GameObject guideObject = GameObject.Find("Txt_TestGuide");
            if (guideObject == null || !guideObject.TryGetComponent(out Text guide))
                return;

            guide.text = "Test_04_HealthDeath\n빨간 구역: 0.5초마다 피해 10 / 피격 후 0.3초 무적\nHP 0: 즉시 사망 및 입력 정지";
        }

        private static void EnsureDashInputAction()
        {
            InputActionAsset inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (inputActions == null)
            {
                throw new FileNotFoundException("공유 InputActionAsset을 찾을 수 없습니다.", InputActionsPath);
            }

            InputActionMap playerMap = inputActions.FindActionMap("Player", true);
            InputAction dash = playerMap.FindAction("Dash", false);
            if (dash == null)
            {
                dash = playerMap.AddAction("Dash", InputActionType.Button);
                dash.AddBinding("<Keyboard>/space", groups: "Keyboard&Mouse");
                File.WriteAllText(Path.GetFullPath(InputActionsPath), inputActions.ToJson());
                AssetDatabase.ImportAsset(InputActionsPath, ImportAssetOptions.ForceUpdate);
            }
        }

        private static void CreateDashDebugUi(PlayerController player)
        {
            GameObject existing = GameObject.Find("Txt_DashDebug");
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }

            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            GameObject textObject = new GameObject("Txt_DashDebug", typeof(RectTransform), typeof(Text), typeof(PlayerDashDebugView));
            textObject.transform.SetParent(canvas.transform, false);

            RectTransform rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(1f, 1f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.pivot = new Vector2(1f, 1f);
            rectTransform.anchoredPosition = new Vector2(-24f, -24f);
            rectTransform.sizeDelta = new Vector2(420f, 180f);

            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 24;
            text.color = Color.white;
            text.alignment = TextAnchor.UpperRight;

            PlayerDashDebugView view = textObject.GetComponent<PlayerDashDebugView>();
            SerializedObject viewObject = new SerializedObject(view);
            viewObject.FindProperty("player").objectReferenceValue = player;
            viewObject.FindProperty("locomotion").objectReferenceValue = player.GetComponent<PlayerLocomotionController>();
            viewObject.FindProperty("output").objectReferenceValue = text;
            viewObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateDashWallTest()
        {
            GameObject existing = GameObject.Find("Environment_DashTest");
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }

            GameObject area = new GameObject("Environment_DashTest");
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "DashWall_01";
            wall.transform.SetParent(area.transform);
            wall.transform.position = new Vector3(-4f, 1f, 2f);
            wall.transform.localScale = new Vector3(0.5f, 2f, 6f);
        }

        private static void UpdateDashTestGuide()
        {
            GameObject guideObject = GameObject.Find("Txt_TestGuide");
            if (guideObject == null || !guideObject.TryGetComponent(out Text guide))
            {
                return;
            }

            guide.text = "Test_03_Dash\nWASD: 이동 / Space: 이동 입력 방향 대시\n2회 충전, 순차 회복, 대시 중 무적";
        }

        /// <summary>PlayerStats_Warrior SO를 로드하고 없을 때만 새로 생성합니다.</summary>
        private static PlayerStatsData LoadOrCreateStats()
        {
            PlayerStatsData stats = AssetDatabase.LoadAssetAtPath<PlayerStatsData>(StatsPath);
            if (stats != null)
            {
                return stats;
            }

            stats = ScriptableObject.CreateInstance<PlayerStatsData>();
            AssetDatabase.CreateAsset(stats, StatsPath);
            return stats;
        }

        /// <summary>바닥과 네 방향 벽으로 기본 테스트 공간을 만듭니다.</summary>
        private static void CreateEnvironment()
        {
            GameObject environment = new GameObject("Environment_Test");

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor_01";
            floor.transform.SetParent(environment.transform);
            floor.transform.position = new Vector3(0f, -0.25f, 0f);
            floor.transform.localScale = new Vector3(20f, 0.5f, 20f);

            CreateWall(environment.transform, "Wall_01", new Vector3(0f, 1f, 10f), new Vector3(20f, 2f, 0.5f));
            CreateWall(environment.transform, "Wall_02", new Vector3(0f, 1f, -10f), new Vector3(20f, 2f, 0.5f));
            CreateWall(environment.transform, "Wall_03", new Vector3(10f, 1f, 0f), new Vector3(0.5f, 2f, 20f));
            CreateWall(environment.transform, "Wall_04", new Vector3(-10f, 1f, 0f), new Vector3(0.5f, 2f, 20f));
        }

        private static void CreateWall(Transform parent, string name, Vector3 position, Vector3 scale)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent);
            wall.transform.position = position;
            wall.transform.localScale = scale;
        }

        /// <summary>Capsule 플레이어에 입력·상태·이동·체력·조준 컴포넌트를 구성합니다.</summary>
        private static void CreatePlayer(PlayerStatsData stats, InputActionAsset inputActions)
        {
            GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player_Warrior_Test";
            player.transform.position = new Vector3(0f, 1f, 0f);

            Collider primitiveCollider = player.GetComponent<Collider>();
            if (primitiveCollider != null)
            {
                Object.DestroyImmediate(primitiveCollider);
            }

            CharacterController controller = player.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.5f;
            controller.center = Vector3.zero;
            controller.stepOffset = 0.3f;
            controller.slopeLimit = 45f;

            PlayerInputReader inputReader = player.AddComponent<PlayerInputReader>();
            SerializedObject inputReaderObject = new SerializedObject(inputReader);
            inputReaderObject.FindProperty("inputActions").objectReferenceValue = inputActions;
            inputReaderObject.ApplyModifiedPropertiesWithoutUndo();

            PlayerController playerController = player.AddComponent<PlayerController>();
            AssignPlayerStats(playerController, stats);
            player.AddComponent<PlayerLocomotionController>();
            player.AddComponent<PlayerHealthController>();
            player.AddComponent<PlayerAimResolver>();
        }

        private static void AssignPlayerStats(PlayerController playerController, PlayerStatsData stats)
        {
            if (stats == null)
                stats = AssetDatabase.LoadAssetAtPath<PlayerStatsData>(StatsPath);

            if (playerController == null || stats == null)
                throw new MissingReferenceException("PlayerController 또는 PlayerStatsData를 찾을 수 없습니다.");

            SerializedObject controllerObject = new SerializedObject(playerController);
            controllerObject.Update();
            SerializedProperty baseStatsProperty = controllerObject.FindProperty("baseStats");
            if (baseStatsProperty == null)
                throw new MissingReferenceException("PlayerController의 baseStats 필드를 찾을 수 없습니다.");

            baseStatsProperty.objectReferenceValue = stats;
            controllerObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(playerController);
        }

        /// <summary>1단계에서 사용할 단순 Main Camera를 생성합니다.</summary>
        private static void CreateCamera()
        {
            GameObject cameraObject = new GameObject("Main Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 10f, -10f);
            cameraObject.transform.rotation = Quaternion.Euler(45f, 0f, 0f);
            camera.clearFlags = CameraClearFlags.Skybox;
        }

        /// <summary>
        /// 기존 카메라를 제거하고 Orbital Follow + Hard Look At 조합의 Cinemachine 카메라를 만듭니다.
        /// </summary>
        private static void ReplaceWithCinemachineCamera(Transform player)
        {
            Camera existingCamera = Object.FindFirstObjectByType<Camera>();
            if (existingCamera != null)
            {
                Object.DestroyImmediate(existingCamera.gameObject);
            }

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<CinemachineBrain>();

            GameObject cinemachineObject = new GameObject("CM_Player_OrbitalCamera");
            CinemachineCamera cinemachineCamera = cinemachineObject.AddComponent<CinemachineCamera>();
            cinemachineCamera.Follow = player;
            cinemachineCamera.LookAt = player;

            CinemachineOrbitalFollow orbitalFollow = cinemachineObject.AddComponent<CinemachineOrbitalFollow>();
            orbitalFollow.OrbitStyle = CinemachineOrbitalFollow.OrbitStyles.Sphere;
            orbitalFollow.TargetOffset = new Vector3(0f, 1f, 0f);
            orbitalFollow.Radius = 14f;

            var horizontalAxis = orbitalFollow.HorizontalAxis;
            horizontalAxis.Value = 0f;
            horizontalAxis.Range = new Vector2(-180f, 180f);
            horizontalAxis.Wrap = true;
            orbitalFollow.HorizontalAxis = horizontalAxis;

            var verticalAxis = orbitalFollow.VerticalAxis;
            verticalAxis.Value = 45f;
            verticalAxis.Range = new Vector2(15f, 65f);
            orbitalFollow.VerticalAxis = verticalAxis;

            var radialAxis = orbitalFollow.RadialAxis;
            radialAxis.Value = 1f;
            radialAxis.Range = new Vector2(1f, 1f);
            orbitalFollow.RadialAxis = radialAxis;

            cinemachineObject.AddComponent<CinemachineHardLookAt>();
            AssignMovementReference(player.GetComponent<PlayerLocomotionController>());
        }

        private static void CreateSlopeTestArea()
        {
            GameObject oldArea = GameObject.Find("Environment_SlopeTest");
            if (oldArea != null)
            {
                Object.DestroyImmediate(oldArea);
            }

            GameObject area = new GameObject("Environment_SlopeTest");

            GameObject ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ramp.name = "Slope_01";
            ramp.transform.SetParent(area.transform);
            ramp.transform.position = new Vector3(4f, 0.5f, 2f);
            ramp.transform.rotation = Quaternion.Euler(0f, 0f, -15f);
            ramp.transform.localScale = new Vector3(5f, 0.5f, 4f);

            GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform.name = "Platform_01";
            platform.transform.SetParent(area.transform);
            platform.transform.position = new Vector3(7f, 1.2f, 2f);
            platform.transform.localScale = new Vector3(2f, 0.5f, 4f);
        }

        private static void UpdateTestGuide()
        {
            GameObject guideObject = GameObject.Find("Txt_TestGuide");
            if (guideObject == null || !guideObject.TryGetComponent(out Text guide))
            {
                return;
            }

            guide.text = "Test_02_MovementCamera\nWASD: 카메라 기준 이동\nCinemachine Orbital Follow / 경사 이동 테스트";
        }

        private static void CreateLight()
        {
            GameObject lightObject = new GameObject("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private static void CreateTestUi()
        {
            GameObject canvasObject = new GameObject("Pnl_TestUI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            GameObject textObject = new GameObject("Txt_TestGuide", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(canvasObject.transform, false);
            RectTransform rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0f, 1f);
            rectTransform.anchoredPosition = new Vector2(24f, -24f);
            rectTransform.sizeDelta = new Vector2(800f, 120f);

            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 24;
            text.color = Color.white;
            text.alignment = TextAnchor.UpperLeft;
            text.text = "Test_01_PlayerBase\nWASD: 이동\n1단계: 플레이어 데이터 / 런타임 상태 / 입력 구조";
        }

        /// <summary>에셋 경로에 대응하는 실제 폴더가 없으면 생성합니다.</summary>
        private static void EnsureDirectory(string assetPath)
        {
            string systemPath = Path.GetFullPath(assetPath);
            if (!Directory.Exists(systemPath))
            {
                Directory.CreateDirectory(systemPath);
            }
        }
    }
}
