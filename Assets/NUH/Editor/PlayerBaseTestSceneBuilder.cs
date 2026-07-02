using System.IO;
using FlatVenture.NUH.Player;
using FlatVenture.NUH.Player.Combat;
using FlatVenture.NUH.Player.Data;
using FlatVenture.NUH.Player.Debugging;
using FlatVenture.NUH.Player.Input;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FlatVenture.NUH.Editor
{
    /// <summary>
    /// 의현 담당 1단계 테스트 에셋과 씬을 재현 가능하게 생성합니다.
    /// </summary>
    public static class PlayerBaseTestSceneBuilder
    {
        private const string SceneDirectory = "Assets/_Scenes/NUH";
        private const string ScenePath = SceneDirectory + "/Test_01_PlayerBase.unity";
        private const string Stage02ScenePath = SceneDirectory + "/Test_02_MovementCamera.unity";
        private const string Stage03ScenePath = SceneDirectory + "/Test_03_Dash.unity";
        private const string Stage04ScenePath = SceneDirectory + "/Test_04_HealthDeath.unity";
        private const string Stage05ScenePath = SceneDirectory + "/Test_05_AimBasicAttack.unity";
        private const string DataDirectory = "Assets/NUH/Data/Player";
        private const string StatsPath = DataDirectory + "/PlayerStats_Warrior.asset";
        private const string InputActionsPath = "Assets/InputSystem_Actions.inputactions";

        [MenuItem("Flat Venture/NUH/1단계 테스트 씬 생성")]
        public static void BuildFromMenu()
        {
            Build();
            EditorUtility.DisplayDialog("Flat Venture", "Test_01_PlayerBase 씬 생성을 완료했습니다.", "확인");
        }

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

        public static void BuildStage04()
        {
            EnsureDirectory(SceneDirectory);
            if (!File.Exists(Path.GetFullPath(Stage03ScenePath)))
                BuildStage03();

            Scene scene = EditorSceneManager.OpenScene(Stage03ScenePath, OpenSceneMode.Single);
            PlayerController player = Object.FindFirstObjectByType<PlayerController>();
            if (player == null)
                throw new MissingReferenceException("3단계 씬에서 PlayerController를 찾을 수 없습니다.");

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

        private static void EnsureEventSystem()
        {
            EventSystem existing = Object.FindFirstObjectByType<EventSystem>();
            if (existing != null)
                return;

            GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystemObject.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
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
            viewObject.FindProperty("output").objectReferenceValue = status;
            viewObject.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddPersistentListener(damageButton.onClick, view.ApplyTestDamage);
            UnityEventTools.AddPersistentListener(invincibleButton.onClick, view.ToggleDebugInvincibility);
            UnityEventTools.AddPersistentListener(restartButton.onClick, view.ResetPlayer);
        }

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
            SerializedObject controllerObject = new SerializedObject(playerController);
            controllerObject.FindProperty("baseStats").objectReferenceValue = stats;
            controllerObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateCamera()
        {
            GameObject cameraObject = new GameObject("Main Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 10f, -10f);
            cameraObject.transform.rotation = Quaternion.Euler(45f, 0f, 0f);
            camera.clearFlags = CameraClearFlags.Skybox;
        }

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

            cinemachineObject.AddComponent<CinemachineRotationComposer>();
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
