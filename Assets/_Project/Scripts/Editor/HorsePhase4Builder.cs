using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RedMoon.Horse;
using RedMoon.Player;
using RedMoon.Camera;

namespace RedMoon.Editor
{
    /// <summary>
    /// Builder tools for the RED MOON Phase 4 Horse system.
    /// Constructs the Horse prefab using the custom 3D model with URP materials,
    /// and generates the fully playable HorseTest.unity scene.
    /// </summary>
    public static class HorsePhase4Builder
    {
        public const string PrefabFolder = "Assets/_Project/Prefabs/Horse";
        public const string PrefabPath = "Assets/_Project/Prefabs/Horse/Horse.prefab";
        public const string ConfigPath = "Assets/_Project/Settings/Horse/Horse_Config.asset";
        public const string ScenePath = "Assets/_Project/Scenes/Development/HorseTest.unity";
        public const string PlayerPrefabPath = "Assets/_Project/Prefabs/Characters/Player_Interim.prefab";
        public const string InputActionsPath = "Assets/_Project/Settings/Input/REDMOON_InputActions.inputactions";
        public const string CameraConfigPath = "Assets/_Project/Settings/Camera/Camera_Config.asset";
        public const string ModelPath = "Assets/_Project/Art/Characters/Horse/Models/Horse_DarkWarrior.obj";
        public const string MaterialPath = "Assets/_Project/Art/Characters/Horse/Materials/M_Horse_DarkCharcoal.mat";

        [MenuItem("RED MOON/Horse/Create Horse Prefab")]
        public static GameObject CreateHorsePrefab()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets/_Project", "Prefabs");
            }
            if (!AssetDatabase.IsValidFolder(PrefabFolder))
            {
                AssetDatabase.CreateFolder("Assets/_Project/Prefabs", "Horse");
            }

            // 1. Ensure Textures & Material exist
            var horseMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (horseMaterial == null)
            {
                horseMaterial = HorseAssetGenerator.GenerateHorseAssets();
            }

            var config = AssetDatabase.LoadAssetAtPath<HorseConfig>(ConfigPath);
            var actions = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(InputActionsPath);

            var horseRoot = new GameObject("Horse");
            horseRoot.layer = LayerMask.NameToLayer("Horse");

            // 2. CharacterController
            var cc = horseRoot.AddComponent<CharacterController>();
            cc.height = config != null ? config.height : 2.0f;
            cc.radius = config != null ? config.radius : 0.65f;
            cc.center = new Vector3(0f, config != null ? config.centerY : 1.0f, 0f);
            cc.skinWidth = config != null ? config.skinWidth : 0.08f;
            cc.stepOffset = config != null ? config.stepOffset : 0.5f;
            cc.slopeLimit = config != null ? config.slopeLimit : 45f;

            // 3. Horse Components
            var groundDetection = horseRoot.AddComponent<HorseGroundDetection>();
            var animatorBridge = horseRoot.AddComponent<HorseAnimator>();
            var inputReader = horseRoot.AddComponent<HorseInputReader>();
            var mountController = horseRoot.AddComponent<HorseMountController>();
            var interaction = horseRoot.AddComponent<HorseInteraction>();
            var controller = horseRoot.AddComponent<HorseController>();
            var debugUI = horseRoot.AddComponent<HorseDebugUI>();

            // 4. Sockets & Anchor Points
            var groundCheck = new GameObject("GroundCheck");
            groundCheck.transform.SetParent(horseRoot.transform, false);
            groundCheck.transform.localPosition = new Vector3(0f, 0.6f, 0f);

            var riderSocket = new GameObject("RiderSocket");
            riderSocket.transform.SetParent(horseRoot.transform, false);
            riderSocket.transform.localPosition = config != null ? config.riderPositionOffset : new Vector3(0f, 1.25f, -0.1f);

            var riderFocus = new GameObject("RiderFocusPoint");
            riderFocus.transform.SetParent(horseRoot.transform, false);
            riderFocus.transform.localPosition = config != null ? config.mountedCameraFocusOffset : new Vector3(0f, 1.9f, 0f);

            var dismountLeft = new GameObject("DismountPoint_Left");
            dismountLeft.transform.SetParent(horseRoot.transform, false);
            dismountLeft.transform.localPosition = config != null ? config.dismountOffsetLeft : new Vector3(-1.3f, 0f, -0.1f);

            var dismountRight = new GameObject("DismountPoint_Right");
            dismountRight.transform.SetParent(horseRoot.transform, false);
            dismountRight.transform.localPosition = config != null ? config.dismountOffsetRight : new Vector3(1.3f, 0f, -0.1f);

            // 5. Visual 3D Model: Horse_DarkWarrior
            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            GameObject visualInstance = null;
            if (modelAsset != null)
            {
                visualInstance = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset, horseRoot.transform);
                visualInstance.name = "Horse_VisualModel";
                visualInstance.transform.localPosition = Vector3.zero;
                visualInstance.transform.localRotation = Quaternion.identity;
                visualInstance.transform.localScale = Vector3.one;

                // Apply URP Dark Charcoal Material to all mesh renderers
                var renderers = visualInstance.GetComponentsInChildren<MeshRenderer>(true);
                foreach (var r in renderers)
                {
                    r.sharedMaterial = horseMaterial;
                    r.gameObject.layer = LayerMask.NameToLayer("Horse");
                    // Ensure visual meshes do not have mesh colliders that fight with CharacterController
                    var mc = r.GetComponent<Collider>();
                    if (mc != null) Object.DestroyImmediate(mc);
                }
            }
            else
            {
                Debug.LogError($"[RED MOON] Horse 3D model not found at {ModelPath}!");
            }

            // 6. Wire Serialized Properties
            var soController = new SerializedObject(controller);
            soController.FindProperty("config").objectReferenceValue = config;
            soController.FindProperty("groundDetection").objectReferenceValue = groundDetection;
            soController.FindProperty("horseAnimator").objectReferenceValue = animatorBridge;
            soController.FindProperty("mountController").objectReferenceValue = mountController;
            soController.FindProperty("inputReader").objectReferenceValue = inputReader;
            soController.ApplyModifiedProperties();

            var soMount = new SerializedObject(mountController);
            soMount.FindProperty("config").objectReferenceValue = config;
            soMount.FindProperty("horseController").objectReferenceValue = controller;
            soMount.FindProperty("horseInputReader").objectReferenceValue = inputReader;
            soMount.FindProperty("riderSocket").objectReferenceValue = riderSocket.transform;
            soMount.FindProperty("riderFocusPoint").objectReferenceValue = riderFocus.transform;
            soMount.FindProperty("dismountPointLeft").objectReferenceValue = dismountLeft.transform;
            soMount.FindProperty("dismountPointRight").objectReferenceValue = dismountRight.transform;
            soMount.ApplyModifiedProperties();

            var soGround = new SerializedObject(groundDetection);
            soGround.FindProperty("config").objectReferenceValue = config;
            soGround.FindProperty("groundCheckPoint").objectReferenceValue = groundCheck.transform;
            soGround.ApplyModifiedProperties();

            var soInput = new SerializedObject(inputReader);
            soInput.FindProperty("inputActions").objectReferenceValue = actions;
            soInput.ApplyModifiedProperties();

            var soInteract = new SerializedObject(interaction);
            soInteract.FindProperty("config").objectReferenceValue = config;
            soInteract.FindProperty("mountController").objectReferenceValue = mountController;
            soInteract.ApplyModifiedProperties();

            // 7. Save Prefab
            var prefab = PrefabUtility.SaveAsPrefabAsset(horseRoot, PrefabPath);
            Object.DestroyImmediate(horseRoot);

            Debug.Log($"[RED MOON] Horse prefab successfully created at: {PrefabPath}");
            return prefab;
        }

        [MenuItem("RED MOON/Horse/Setup Horse Test Scene")]
        public static void SetupHorseTestScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            int groundLayer = LayerMask.NameToLayer("Ground");
            int envLayer = LayerMask.NameToLayer("Environment");

            // 1. Lighting & Environment
            var lightObj = new GameObject("Directional Light");
            var light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.0f;
            light.color = new Color(0.95f, 0.92f, 0.88f);
            lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // 2. Ground Plane
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground_Plane";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(10f, 1f, 10f);
            ground.layer = groundLayer;
            ground.tag = "Ground";

            // 3. Sloped Test Ramp (15 deg slope)
            var ramp1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ramp1.name = "Slope_Test_Ramp_15Deg";
            ramp1.transform.position = new Vector3(0f, 1.25f, 25f);
            ramp1.transform.rotation = Quaternion.Euler(15f, 0f, 0f);
            ramp1.transform.localScale = new Vector3(12f, 0.4f, 12f);
            ramp1.layer = groundLayer;
            ramp1.tag = "Ground";

            // 4. Steeper Ramp (30 deg slope)
            var ramp2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ramp2.name = "Slope_Test_Ramp_30Deg";
            ramp2.transform.position = new Vector3(0f, 2.75f, 38f);
            ramp2.transform.rotation = Quaternion.Euler(30f, 0f, 0f);
            ramp2.transform.localScale = new Vector3(12f, 0.4f, 12f);
            ramp2.layer = groundLayer;
            ramp2.tag = "Ground";

            // 5. Test Obstacles (Walls on Environment layer 12)
            var obstaclesRoot = new GameObject("Test_Obstacles");
            var wallNorth = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallNorth.name = "Obstacle_Wall_North";
            wallNorth.transform.SetParent(obstaclesRoot.transform, false);
            wallNorth.transform.position = new Vector3(0f, 2f, 50f);
            wallNorth.transform.localScale = new Vector3(30f, 4f, 1f);
            wallNorth.layer = envLayer;
            wallNorth.tag = "Environment";

            var wallEast = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallEast.name = "Obstacle_Wall_East";
            wallEast.transform.SetParent(obstaclesRoot.transform, false);
            wallEast.transform.position = new Vector3(20f, 2f, 10f);
            wallEast.transform.localScale = new Vector3(1f, 4f, 40f);
            wallEast.layer = envLayer;
            wallEast.tag = "Environment";

            // 6. Instantiate Horse Prefab
            var horsePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (horsePrefab == null)
            {
                CreateHorsePrefab();
                horsePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            }

            var horseInstance = (GameObject)PrefabUtility.InstantiatePrefab(horsePrefab);
            horseInstance.name = "Horse";
            horseInstance.transform.position = new Vector3(0f, 0f, 0f);

            // 7. Instantiate Player Prefab
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            GameObject playerInstance = null;
            if (playerPrefab != null)
            {
                playerInstance = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
                playerInstance.name = "Player";
                playerInstance.transform.position = new Vector3(1.5f, 0f, 0f);
            }
            else
            {
                Debug.LogError("[RED MOON] Player prefab not found at " + PlayerPrefabPath);
            }

            // 8. Main Camera with CameraController
            var cameraObj = new GameObject("Main Camera");
            cameraObj.tag = "MainCamera";
            cameraObj.transform.position = new Vector3(1.5f, 1.8f, -3.2f);
            var cam = cameraObj.AddComponent<UnityEngine.Camera>();
            cam.fieldOfView = 60f;
            cam.nearClipPlane = 0.1f;
            cameraObj.AddComponent<AudioListener>();

            var cameraConfig = AssetDatabase.LoadAssetAtPath<CameraConfig>(CameraConfigPath);
            var actions = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(InputActionsPath);

            var camController = cameraObj.AddComponent<CameraController>();
            var soCam = new SerializedObject(camController);
            soCam.FindProperty("config").objectReferenceValue = cameraConfig;
            soCam.FindProperty("target").objectReferenceValue = playerInstance != null ? playerInstance.transform : horseInstance.transform;
            if (playerInstance != null)
            {
                soCam.FindProperty("playerController").objectReferenceValue = playerInstance.GetComponent<PlayerController>();
            }
            soCam.FindProperty("inputActions").objectReferenceValue = actions;
            soCam.FindProperty("lockCursor").boolValue = true;
            soCam.ApplyModifiedProperties();

            // 9. Attach HorseTestHarness with autoRunOnStart = false (Manual gameplay first)
            var harness = cameraObj.AddComponent<HorseTestHarness>();
            var soHarness = new SerializedObject(harness);
            soHarness.FindProperty("autoRunOnStart").boolValue = false;
            soHarness.FindProperty("horseController").objectReferenceValue = horseInstance.GetComponent<HorseController>();
            if (playerInstance != null)
            {
                soHarness.FindProperty("playerController").objectReferenceValue = playerInstance.GetComponent<PlayerController>();
            }
            soHarness.FindProperty("cameraController").objectReferenceValue = camController;
            soHarness.ApplyModifiedProperties();

            // Save Scene
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[RED MOON] HorseTest scene successfully generated and saved at: {ScenePath}");
        }
    }
}
