using System.IO;
using UnityEditor;
using UnityEngine;
using RedMoon.Camera;

namespace RedMoon.Editor
{
    /// <summary>
    /// Builder tools for the RED MOON Phase 3 cinematic camera system.
    /// </summary>
    public static class CameraPhase3Builder
    {
        public const string ConfigFolder = "Assets/_Project/Settings/Camera";
        public const string ConfigPath = "Assets/_Project/Settings/Camera/Camera_Config.asset";
        public const string TestScenePath = "Assets/_Project/Scenes/Development/PlayerTest.unity";

        [MenuItem("RED MOON/Camera/Setup Test Scene Camera")]
        public static void SetupTestSceneCamera()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(TestScenePath);

            var cameraObject = GameObject.FindWithTag("MainCamera");
            if (cameraObject == null)
            {
                Debug.LogError("[RED MOON] Main Camera not found in " + TestScenePath);
                return;
            }

            var config = AssetDatabase.LoadAssetAtPath<CameraConfig>(ConfigPath);
            if (config == null)
            {
                CreateCameraConfig();
                config = AssetDatabase.LoadAssetAtPath<CameraConfig>(ConfigPath);
            }

            var player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                Debug.LogError("[RED MOON] Player not found in " + TestScenePath);
                return;
            }

            var cameraController = cameraObject.GetComponent<CameraController>();
            if (cameraController == null)
            {
                cameraController = cameraObject.AddComponent<CameraController>();
            }

            var so = new SerializedObject(cameraController);
            so.FindProperty("config").objectReferenceValue = config;
            so.FindProperty("target").objectReferenceValue = player.transform;
            so.FindProperty("playerController").objectReferenceValue = player.GetComponent<RedMoon.Player.PlayerController>();
            so.ApplyModifiedProperties();

            var harness = cameraObject.GetComponent<CameraTestHarness>();
            if (harness == null)
            {
                harness = cameraObject.AddComponent<CameraTestHarness>();
            }

            var harnessSo = new SerializedObject(harness);
            harnessSo.FindProperty("cameraController").objectReferenceValue = cameraController;
            harnessSo.FindProperty("playerController").objectReferenceValue = player.GetComponent<RedMoon.Player.PlayerController>();
            harnessSo.ApplyModifiedProperties();

            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            Debug.Log("[RED MOON] Configured CameraController on Main Camera in " + TestScenePath);
        }

        [MenuItem("RED MOON/Camera/Create Camera Config")]
        public static void CreateCameraConfig()
        {
            EnsureFolder(ConfigFolder);

            var existing = AssetDatabase.LoadAssetAtPath<CameraConfig>(ConfigPath);
            if (existing != null)
            {
                Debug.Log("[RED MOON] CameraConfig already exists at " + ConfigPath);
                return;
            }

            var config = ScriptableObject.CreateInstance<CameraConfig>();
            config.focusOffset = new Vector3(0f, 1.4f, 0f);
            config.defaultShoulderOffset = 0.45f;
            config.leftShoulderOffset = -0.45f;
            config.shoulderDamping = 4f;
            config.defaultDistance = 3.2f;
            config.minDistance = 1.5f;
            config.maxDistance = 5.0f;
            config.zoomSpeed = 2f;
            config.yawSensitivity = 120f;
            config.pitchSensitivity = 100f;
            config.invertY = false;
            config.minPitch = -35f;
            config.maxPitch = 65f;
            config.positionSmoothing = 0.08f;
            config.rotationSmoothing = 0.04f;
            config.collisionRadius = 0.2f;
            config.collisionPadding = 0.15f;
            config.collisionLayers = new LayerMask { value = (1 << 12) | (1 << 13) };
            config.defaultFOV = 60f;
            config.sprintFOV = 64f;
            config.fovBlendSpeed = 3f;

            AssetDatabase.CreateAsset(config, ConfigPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[RED MOON] Created CameraConfig at " + ConfigPath);
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            var parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            var name = Path.GetFileName(folder);

            if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(name))
            {
                EnsureFolder(parent);
                AssetDatabase.CreateFolder(parent, name);
            }
        }
    }
}

