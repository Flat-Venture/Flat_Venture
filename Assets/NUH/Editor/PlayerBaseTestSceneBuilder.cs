using System.IO;
using FlatVenture.NUH.Player;
using FlatVenture.NUH.Player.Data;
using FlatVenture.NUH.Player.Debugging;
using FlatVenture.NUH.Player.Input;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
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
