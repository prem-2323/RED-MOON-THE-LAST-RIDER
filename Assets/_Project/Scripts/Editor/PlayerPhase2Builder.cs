using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.InputSystem;
using RedMoon.Player;

namespace RedMoon.Editor
{
    /// <summary>
    /// Builds the Phase 2 player config, animator controller, prefab and
    /// development test scene.
    /// </summary>
    public static class PlayerPhase2Builder
    {
        private const string NinjaPath = "Assets/_Project/Art/Characters/Player/Models/Player_Interim_Ninja.fbx";
        private const string AnimationLibraryPath = "Assets/_Project/Art/Characters/Player/Animations/UniversalAnimationLibrary_Standard.fbx";
        private const string InputActionsPath = "Assets/_Project/Settings/Input/REDMOON_InputActions.inputactions";
        private const string ConfigPath = "Assets/_Project/Settings/Player/Player_Config.asset";
        private const string ControllerPath = "Assets/_Project/Settings/Player/Player_AnimatorController.controller";
        private const string PrefabPath = "Assets/_Project/Prefabs/Characters/Player_Interim.prefab";
        private const string ScenePath = "Assets/_Project/Scenes/Development/PlayerTest.unity";

        [MenuItem("RED MOON/Player/Create Player Config")]
        public static void CreatePlayerConfig()
        {
            EnsureFolder("Assets/_Project/Settings/Player");

            if (AssetDatabase.LoadAssetAtPath<PlayerConfig>(ConfigPath) != null)
            {
                Debug.Log("[RED MOON] PlayerConfig already exists.");
                return;
            }

            var config = ScriptableObject.CreateInstance<PlayerConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            AssetDatabase.SaveAssets();
            Debug.Log("[RED MOON] Created PlayerConfig at " + ConfigPath);
        }

        [MenuItem("RED MOON/Player/Create Animator Controller")]
        public static void CreateAnimatorController()
        {
            EnsureFolder("Assets/_Project/Settings/Player");

            if (AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath) != null)
            {
                AssetDatabase.DeleteAsset(ControllerPath);
            }

            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            AddParameters(controller);
            BuildStateMachine(controller);
            AssetDatabase.SaveAssets();
            Debug.Log("[RED MOON] Created Player Animator Controller at " + ControllerPath);
        }

        [MenuItem("RED MOON/Player/Build Player Prefab")]
        public static void BuildPlayerPrefab()
        {
            EnsureFolder("Assets/_Project/Prefabs/Characters");

            var config = AssetDatabase.LoadAssetAtPath<PlayerConfig>(ConfigPath);
            if (config == null)
            {
                Debug.LogError("[RED MOON] PlayerConfig is missing. Create it first.");
                return;
            }

            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
            if (controller == null)
            {
                Debug.LogError("[RED MOON] Player Animator Controller is missing. Create it first.");
                return;
            }

            var ninjaModel = AssetDatabase.LoadAssetAtPath<GameObject>(NinjaPath);
            if (ninjaModel == null)
            {
                Debug.LogError("[RED MOON] Ninja model is missing at " + NinjaPath);
                return;
            }

            var root = new GameObject("Player_Interim");
            var characterController = root.AddComponent<CharacterController>();
            var playerController = root.AddComponent<PlayerController>();
            var inputReader = root.AddComponent<PlayerInputReader>();
            var animatorBridge = root.AddComponent<PlayerAnimator>();
            var modelSocket = root.AddComponent<PlayerModelSocket>();

            root.tag = "Player";
            root.layer = LayerMask.NameToLayer("Player");

            var modelInstance = PrefabUtility.InstantiatePrefab(ninjaModel) as GameObject;
            modelInstance.name = "Ninja_Interim_Visual";
            modelInstance.transform.SetParent(root.transform, false);
            modelInstance.transform.localPosition = Vector3.zero;
            modelInstance.transform.localRotation = Quaternion.identity;
            modelInstance.transform.localScale = Vector3.one;

            foreach (var collider in modelInstance.GetComponentsInChildren<Collider>(true))
            {
                Object.DestroyImmediate(collider);
            }

            foreach (var childTransform in modelInstance.GetComponentsInChildren<Transform>(true))
            {
                childTransform.gameObject.layer = LayerMask.NameToLayer("Player");
            }

            var modelAnimator = modelInstance.GetComponentInChildren<Animator>();
            if (modelAnimator == null)
            {
                modelAnimator = modelInstance.AddComponent<Animator>();
            }

            var ninjaAvatar = AssetDatabase.LoadAllAssetsAtPath(NinjaPath)
                .OfType<Avatar>()
                .FirstOrDefault();

            if (modelAnimator.avatar == null && ninjaAvatar != null)
            {
                modelAnimator.avatar = ninjaAvatar;
            }

            modelAnimator.runtimeAnimatorController = controller;
            modelAnimator.applyRootMotion = false;
            modelAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            var renderer = modelInstance.GetComponentInChildren<SkinnedMeshRenderer>();
            if (renderer != null)
            {
                float feetOffset = renderer.bounds.min.y;
                modelInstance.transform.localPosition = new Vector3(0f, -feetOffset, 0f);
            }

            var swordSocket = new GameObject("SwordSocket").transform;
            swordSocket.SetParent(modelInstance.transform, false);

            Transform handBone = null;
            if (modelAnimator != null && modelAnimator.avatar != null)
            {
                handBone = modelAnimator.GetBoneTransform(HumanBodyBones.RightHand);
            }
            if (handBone == null)
            {
                handBone = modelInstance.GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(t => t.name.Equals("RightHand", System.StringComparison.OrdinalIgnoreCase));
            }

            if (handBone != null)
            {
                swordSocket.SetParent(handBone, false);
                swordSocket.localPosition = Vector3.zero;
                swordSocket.localRotation = Quaternion.identity;
            }
            else
            {
                swordSocket.localPosition = new Vector3(0.25f, 1.15f, 0.2f);
            }

            var cloakRoot = new GameObject("CloakRoot").transform;
            cloakRoot.SetParent(modelInstance.transform, false);

            Transform chestBone = modelInstance.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t.name.Equals("Spine02", System.StringComparison.OrdinalIgnoreCase) ||
                                     t.name.Equals("Spine01", System.StringComparison.OrdinalIgnoreCase));
            if (chestBone != null)
            {
                cloakRoot.SetParent(chestBone, false);
                cloakRoot.localPosition = Vector3.zero;
                cloakRoot.localRotation = Quaternion.identity;
            }
            else
            {
                cloakRoot.localPosition = new Vector3(0f, 1.3f, -0.1f);
            }

            var groundCheck = new GameObject("GroundCheckPoint");
            groundCheck.transform.SetParent(root.transform, false);
            groundCheck.transform.localPosition = new Vector3(0f, config.centerY - config.height * 0.5f + config.groundCheckOffset, 0f);

            var controllerSerialized = new SerializedObject(playerController);
            controllerSerialized.FindProperty("config").objectReferenceValue = config;
            controllerSerialized.FindProperty("inputReader").objectReferenceValue = inputReader;
            controllerSerialized.FindProperty("groundCheckPoint").objectReferenceValue = groundCheck.transform;
            controllerSerialized.ApplyModifiedProperties();

            var inputSerialized = new SerializedObject(inputReader);
            inputSerialized.FindProperty("inputActions").objectReferenceValue = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            inputSerialized.ApplyModifiedProperties();

            var animatorSerialized = new SerializedObject(animatorBridge);
            animatorSerialized.FindProperty("animator").objectReferenceValue = modelAnimator;
            animatorSerialized.FindProperty("controller").objectReferenceValue = playerController;
            animatorSerialized.ApplyModifiedProperties();

            var socketSerialized = new SerializedObject(modelSocket);
            socketSerialized.FindProperty("visualRoot").objectReferenceValue = modelInstance.transform;
            socketSerialized.FindProperty("swordSocket").objectReferenceValue = swordSocket;
            socketSerialized.FindProperty("cloakRoot").objectReferenceValue = cloakRoot;
            socketSerialized.ApplyModifiedProperties();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);

            Debug.Log("[RED MOON] Built Player_Interim prefab at " + PrefabPath);
        }

        [MenuItem("RED MOON/Player/Build Player Test Scene")]
        public static void BuildPlayerTestScene()
        {
            EnsureFolder("Assets/_Project/Scenes/Development");

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                Debug.LogError("[RED MOON] Player prefab is missing at " + PrefabPath);
                return;
            }

            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
                UnityEditor.SceneManagement.NewSceneMode.Single);

            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Test_Ground";
            ground.transform.localScale = new Vector3(20f, 1f, 20f);
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            ground.layer = LayerMask.NameToLayer("Ground");
            ground.tag = "Environment";

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.layer = LayerMask.NameToLayer("Default");
            var camera = cameraObject.AddComponent<UnityEngine.Camera>();
            camera.fieldOfView = 60f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            cameraObject.transform.position = new Vector3(0f, 1.8f, -4.5f);
            cameraObject.transform.rotation = Quaternion.Euler(12f, 0f, 0f);
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();

            var lightObject = new GameObject("Directional Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            lightObject.transform.position = new Vector3(2f, 5f, -3f);
            lightObject.transform.rotation = Quaternion.Euler(45f, -30f, 0f);

            var player = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            player.transform.position = new Vector3(0f, 0.05f, 0f);

            var testDriverObject = new GameObject("PlayerTestDriver");
            var testDriver = testDriverObject.AddComponent<PlayerTestDriver>();
            testDriver.player = player.GetComponent<PlayerController>();

            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, ScenePath);
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ScenePath);

            Debug.Log("[RED MOON] Built PlayerTest scene at " + ScenePath);
        }

        [MenuItem("RED MOON/Player/Build All Phase 2 Assets")]
        public static void BuildAllPhase2Assets()
        {
            PlayerAssetSetup.ConfigureImportedAssets();
            CreatePlayerConfig();
            CreateAnimatorController();
            BuildPlayerPrefab();
            BuildPlayerTestScene();
        }

        private static void AddParameters(AnimatorController controller)
        {
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("VerticalSpeed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Turn", AnimatorControllerParameterType.Float);
            controller.AddParameter("State", AnimatorControllerParameterType.Int);
            controller.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Land", AnimatorControllerParameterType.Trigger);
        }

        private static void BuildStateMachine(AnimatorController controller)
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(AnimationLibraryPath)
                .OfType<AnimationClip>()
                .ToList();

            AnimationClip FindClip(string name)
            {
                return clips.FirstOrDefault(clip => clip.name == name)
                       ?? clips.FirstOrDefault(clip => clip.name.EndsWith("|" + name, System.StringComparison.Ordinal))
                       ?? clips.FirstOrDefault(clip => clip.name.IndexOf(name, System.StringComparison.Ordinal) >= 0);
            }

            var stateMachine = controller.layers[0].stateMachine;

            var idle = stateMachine.AddState("Idle");
            idle.motion = FindClip("Idle_Loop");

            var walk = stateMachine.AddState("Walk");
            walk.motion = FindClip("Walk_Loop");

            var run = stateMachine.AddState("Run");
            run.motion = FindClip("Jog_Fwd_Loop");

            var sprint = stateMachine.AddState("Sprint");
            sprint.motion = FindClip("Sprint_Loop");

            var jump = stateMachine.AddState("Jump");
            jump.motion = FindClip("Jump_Start");

            var fall = stateMachine.AddState("Fall");
            fall.motion = FindClip("Jump_Loop");

            var land = stateMachine.AddState("Land");
            land.motion = FindClip("Jump_Land");

            stateMachine.defaultState = idle;

            var jumpTransition = stateMachine.AddAnyStateTransition(jump);
            jumpTransition.AddCondition(AnimatorConditionMode.If, 0f, "Jump");

            var fallTransition = stateMachine.AddAnyStateTransition(fall);
            fallTransition.AddCondition(AnimatorConditionMode.IfNot, 0f, "Grounded");
            fallTransition.AddCondition(AnimatorConditionMode.Less, -0.1f, "VerticalSpeed");

            var landTransition = stateMachine.AddAnyStateTransition(land);
            landTransition.AddCondition(AnimatorConditionMode.If, 0f, "Land");

            AddTransition(idle, walk, AnimatorConditionMode.Greater, 0.1f, "Speed");
            AddTransition(idle, run, AnimatorConditionMode.Greater, 2.5f, "Speed");
            AddTransition(idle, sprint, AnimatorConditionMode.Greater, 5.5f, "Speed");
            AddTransition(walk, run, AnimatorConditionMode.Greater, 2.5f, "Speed");
            AddTransition(walk, sprint, AnimatorConditionMode.Greater, 5.5f, "Speed");
            AddTransition(walk, idle, AnimatorConditionMode.Less, 0.1f, "Speed");
            AddTransition(run, walk, AnimatorConditionMode.Less, 2.0f, "Speed");
            AddTransition(run, sprint, AnimatorConditionMode.Greater, 5.5f, "Speed");
            AddTransition(run, idle, AnimatorConditionMode.Less, 0.1f, "Speed");
            AddTransition(sprint, run, AnimatorConditionMode.Less, 5.0f, "Speed");
            AddTransition(sprint, idle, AnimatorConditionMode.Less, 0.1f, "Speed");
            AddTransition(jump, fall, AnimatorConditionMode.Less, -0.1f, "VerticalSpeed");
            AddTransition(fall, land, AnimatorConditionMode.If, 0f, "Land");
            AddTransition(land, idle, AnimatorConditionMode.Less, 0.1f, "Speed");
            AddTransition(land, walk, AnimatorConditionMode.Greater, 0.1f, "Speed");
            AddTransition(land, run, AnimatorConditionMode.Greater, 2.5f, "Speed");
            AddTransition(land, sprint, AnimatorConditionMode.Greater, 5.5f, "Speed");
        }

        private static void AddTransition(
            AnimatorState from,
            AnimatorState to,
            AnimatorConditionMode mode,
            float threshold,
            string parameter)
        {
            var transition = from.AddTransition(to);
            transition.AddCondition(mode, threshold, parameter);
            transition.hasExitTime = false;
            transition.exitTime = 0f;
            transition.duration = 0.15f;
            transition.interruptionSource = TransitionInterruptionSource.None;
            transition.orderedInterruption = false;
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            var parent = System.IO.Path.GetDirectoryName(folder)?.Replace('\\', '/');
            var name = System.IO.Path.GetFileName(folder);

            if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(name))
            {
                EnsureFolder(parent);
                AssetDatabase.CreateFolder(parent, name);
            }
        }
    }
}
