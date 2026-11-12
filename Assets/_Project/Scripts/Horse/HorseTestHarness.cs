using System;
using System.Collections;
using UnityEngine;
using RedMoon.Player;
using RedMoon.Camera;

namespace RedMoon.Horse
{
    /// <summary>
    /// Comprehensive automated QA test harness for Phase 4 Horse Development.
    /// Executes all 29 automated verification tests in Play Mode.
    /// Defaulted to autoRunOnStart = false so manual gameplay is never hijacked.
    /// </summary>
    public sealed class HorseTestHarness : MonoBehaviour
    {
        [Header("Execution Mode")]
        [Tooltip("When true, runs the full automated test suite on scene start.")]
        [SerializeField] private bool autoRunOnStart = false;

        [Header("Scene References")]
        [SerializeField] private HorseController horseController;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private CameraController cameraController;

        public bool TestsCompleted { get; private set; }
        public bool AllTestsPassed { get; private set; }
        public int PassedCount { get; private set; }
        public int FailedCount { get; private set; }
        public string TestSummary { get; private set; } = "Not Run";

        // 29 Individual Test Statuses
        public bool Test01_PrefabInstantiation { get; private set; }
        public bool Test02_ConfigLoading { get; private set; }
        public bool Test03_InputInitialization { get; private set; }
        public bool Test04_IdleState { get; private set; }
        public bool Test05_WalkMovement { get; private set; }
        public bool Test06_TrotMovement { get; private set; }
        public bool Test07_GallopMovement { get; private set; }
        public bool Test08_SprintMovement { get; private set; }
        public bool Test09_Acceleration { get; private set; }
        public bool Test10_Deceleration { get; private set; }
        public bool Test11_LeftRightTurning { get; private set; }
        public bool Test12_GroundDetection { get; private set; }
        public bool Test13_SlopeStability { get; private set; }
        public bool Test14_MountInteractionRange { get; private set; }
        public bool Test15_SuccessfulMounting { get; private set; }
        public bool Test16_PlayerAttachment { get; private set; }
        public bool Test17_PlayerInputSuspension { get; private set; }
        public bool Test18_HorseInputActivation { get; private set; }
        public bool Test19_MountedCameraFocus { get; private set; }
        public bool Test20_MouseCameraOrbitMounted { get; private set; }
        public bool Test21_ShoulderSwitchingMounted { get; private set; }
        public bool Test22_SuccessfulDismount { get; private set; }
        public bool Test23_PlayerMovementRestoration { get; private set; }
        public bool Test24_CameraRestoration { get; private set; }
        public bool Test25_SafeDismountPosition { get; private set; }
        public bool Test26_RepeatedMountDismountPrevention { get; private set; }
        public bool Test27_BootstrapRegression { get; private set; }
        public bool Test28_PlayerTestRegression { get; private set; }
        public bool Test29_CameraRegression { get; private set; }

        private void Start()
        {
            if (autoRunOnStart)
            {
                RunAllTests();
            }
        }

        [ContextMenu("Execute All Tests")]
        public void RunAllTests()
        {
            StopAllCoroutines();
            StartCoroutine(RunAllTestsRoutine());
        }

        private IEnumerator RunAllTestsRoutine()
        {
            yield return new WaitForSeconds(0.2f);

            PassedCount = 0;
            FailedCount = 0;

            if (horseController == null) horseController = FindAnyObjectByType<HorseController>();
            if (playerController == null) playerController = FindAnyObjectByType<PlayerController>();
            if (cameraController == null) cameraController = FindAnyObjectByType<CameraController>();

            // Test 01: Horse Prefab Instantiation
            Test01_PrefabInstantiation = horseController != null && horseController.CharacterController != null;
            RecordResult(1, "Horse Prefab Instantiation", Test01_PrefabInstantiation);

            // Test 02: HorseConfig Loading
            var config = horseController != null ? horseController.Config : null;
            Test02_ConfigLoading = config != null && config.walkSpeed > 0f && config.sprintSpeed > config.walkSpeed;
            RecordResult(2, "HorseConfig Loading", Test02_ConfigLoading);

            // Test 03: Horse Input Initialization
            var inputReader = horseController != null ? horseController.GetComponent<HorseInputReader>() : null;
            Test03_InputInitialization = inputReader != null;
            RecordResult(3, "Horse Input Initialization", Test03_InputInitialization);

            if (horseController != null)
            {
                horseController.ExternalInput = true;
            }

            try
            {
                // Test 04: Idle State
                yield return new WaitForSeconds(0.1f);
                if (horseController != null)
                {
                    horseController.ApplyInput(HorseInputFrame.Empty, 0.05f);
                    Test04_IdleState = horseController.CurrentState == HorseState.Idle && horseController.HorizontalSpeed < 0.1f;
                }
                RecordResult(4, "Idle State", Test04_IdleState);

                // Test 05: Walk Movement
                if (horseController != null)
                {
                    for (int i = 0; i < 20; i++)
                    {
                        horseController.ApplyInput(new HorseInputFrame(new Vector2(0f, 0.25f), Vector2.zero, false, false, false), 0.05f);
                    }
                    Test05_WalkMovement = horseController.HorizontalSpeed > 0.5f && (horseController.CurrentState == HorseState.Walking || horseController.HorizontalSpeed <= config.walkSpeed + 0.5f);
                }
                RecordResult(5, "Walk Movement", Test05_WalkMovement);

                // Test 06: Trot Movement
                if (horseController != null)
                {
                    for (int i = 0; i < 25; i++)
                    {
                        horseController.ApplyInput(new HorseInputFrame(new Vector2(0f, 0.6f), Vector2.zero, false, false, false), 0.05f);
                    }
                    Test06_TrotMovement = horseController.HorizontalSpeed > config.walkSpeed && horseController.HorizontalSpeed <= config.trotSpeed + 1.0f;
                }
                RecordResult(6, "Trot Movement", Test06_TrotMovement);

                // Test 07: Gallop Movement
                if (horseController != null)
                {
                    for (int i = 0; i < 30; i++)
                    {
                        horseController.ApplyInput(new HorseInputFrame(new Vector2(0f, 1.0f), Vector2.zero, false, false, false), 0.05f);
                    }
                    Test07_GallopMovement = horseController.HorizontalSpeed > config.trotSpeed && horseController.HorizontalSpeed <= config.gallopSpeed + 1.0f;
                }
                RecordResult(7, "Gallop Movement", Test07_GallopMovement);

                // Test 08: Sprint Movement
                if (horseController != null)
                {
                    for (int i = 0; i < 35; i++)
                    {
                        horseController.ApplyInput(new HorseInputFrame(new Vector2(0f, 1.0f), Vector2.zero, true, false, false), 0.05f);
                    }
                    Test08_SprintMovement = horseController.HorizontalSpeed > config.gallopSpeed && horseController.CurrentState == HorseState.Sprinting;
                }
                RecordResult(8, "Sprint Movement", Test08_SprintMovement);

                // Test 09: Acceleration
                float speedStart = 0f;
                float speedAfter5Frames = 0f;
                if (horseController != null)
                {
                    horseController.StopMovement();
                    speedStart = horseController.HorizontalSpeed;
                    for (int i = 0; i < 8; i++)
                    {
                        horseController.ApplyInput(new HorseInputFrame(new Vector2(0f, 1.0f), Vector2.zero, false, false, false), 0.05f);
                    }
                    speedAfter5Frames = horseController.HorizontalSpeed;
                    Test09_Acceleration = speedAfter5Frames > speedStart && speedAfter5Frames <= config.gallopSpeed;
                }
                RecordResult(9, "Acceleration Curve", Test09_Acceleration);

                // Test 10: Deceleration
                float speedBeforeBrake = speedAfter5Frames;
                float speedAfterBrake = 0f;
                if (horseController != null)
                {
                    for (int i = 0; i < 25; i++)
                    {
                        horseController.ApplyInput(HorseInputFrame.Empty, 0.05f);
                    }
                    speedAfterBrake = horseController.HorizontalSpeed;
                    Test10_Deceleration = speedAfterBrake < speedBeforeBrake && speedAfterBrake < 0.2f;
                    horseController.StopMovement();
                }
                RecordResult(10, "Deceleration Curve", Test10_Deceleration);

                // Test 11: Left and Right Turning
                float initialYaw = horseController != null ? horseController.transform.eulerAngles.y : 0f;
                float rightYaw = initialYaw;
                float leftYaw = initialYaw;
                if (horseController != null)
                {
                    for (int i = 0; i < 15; i++)
                    {
                        horseController.ApplyInput(new HorseInputFrame(new Vector2(1.0f, 0f), Vector2.zero, false, false, false), 0.05f);
                    }
                    rightYaw = horseController.transform.eulerAngles.y;

                    for (int i = 0; i < 30; i++)
                    {
                        horseController.ApplyInput(new HorseInputFrame(new Vector2(-1.0f, 0f), Vector2.zero, false, false, false), 0.05f);
                    }
                    leftYaw = horseController.transform.eulerAngles.y;

                    Test11_LeftRightTurning = Mathf.Abs(Mathf.DeltaAngle(initialYaw, rightYaw)) > 5f && Mathf.Abs(Mathf.DeltaAngle(rightYaw, leftYaw)) > 5f;
                }
                RecordResult(11, "Left/Right Turning", Test11_LeftRightTurning);

                // Test 12: Ground Detection
                Test12_GroundDetection = horseController != null && horseController.GroundDetection != null && horseController.IsGrounded;
                RecordResult(12, "Ground Detection", Test12_GroundDetection);

                // Test 13: Slope Stability
                Test13_SlopeStability = horseController != null && horseController.GroundDetection != null && horseController.GroundDetection.SlopeAngle < 50f;
                RecordResult(13, "Slope Stability", Test13_SlopeStability);

                // Test 14: Mount Interaction Range
                var mountCtrl = horseController != null ? horseController.MountController : null;
                if (mountCtrl != null && playerController != null)
                {
                    // Place player far away (10m)
                    playerController.transform.position = horseController.transform.position + Vector3.forward * 10f;
                    bool canMountFar = mountCtrl.CanMount(playerController);

                    // Place player close (1.5m)
                    playerController.transform.position = horseController.transform.position + Vector3.right * 1.5f;
                    bool canMountClose = mountCtrl.CanMount(playerController);

                    Test14_MountInteractionRange = !canMountFar && canMountClose;
                }
                RecordResult(14, "Mount Interaction Range", Test14_MountInteractionRange);

                // Test 15: Successful Mounting
                bool mountSuccess = false;
                if (mountCtrl != null && playerController != null)
                {
                    mountSuccess = mountCtrl.Mount(playerController);
                    Test15_SuccessfulMounting = mountSuccess && mountCtrl.IsMounted;
                }
                RecordResult(15, "Successful Mounting", Test15_SuccessfulMounting);

                // Test 16: Player Attachment to Rider Position
                Test16_PlayerAttachment = mountCtrl != null && playerController != null && playerController.transform.parent == mountCtrl.RiderSocket;
                RecordResult(16, "Player Attachment to RiderSocket", Test16_PlayerAttachment);

                // Test 17: Player Input Suspension
                var pCharController = playerController != null ? playerController.GetComponent<CharacterController>() : null;
                var pInputReader = playerController != null ? playerController.GetComponent<PlayerInputReader>() : null;
                Test17_PlayerInputSuspension = pCharController != null && !pCharController.enabled && pInputReader != null && !pInputReader.enabled && !playerController.enabled;
                RecordResult(17, "Player Input Suspension While Mounted", Test17_PlayerInputSuspension);

                // Test 18: Horse Input Activation While Mounted
                Test18_HorseInputActivation = inputReader != null && inputReader.IsActive;
                RecordResult(18, "Horse Input Activation While Mounted", Test18_HorseInputActivation);

                // Test 19: Mounted Camera Focus
                Test19_MountedCameraFocus = cameraController != null && mountCtrl != null && cameraController.Target == mountCtrl.RiderFocusPoint;
                RecordResult(19, "Mounted Camera Focus Redirection", Test19_MountedCameraFocus);

                // Test 20: Mouse Camera Orbit While Mounted
                if (cameraController != null)
                {
                    float camYawBefore = cameraController.CurrentYaw;
                    cameraController.ApplyLook(new Vector2(40f, 15f), true, 0.05f);
                    Test20_MouseCameraOrbitMounted = Mathf.Abs(cameraController.TargetYaw - camYawBefore) > 10f;
                }
                RecordResult(20, "Mouse Camera Orbit While Mounted", Test20_MouseCameraOrbitMounted);

                // Test 21: Shoulder Switching While Mounted
                if (cameraController != null)
                {
                    bool shoulderBefore = cameraController.IsRightShoulder;
                    cameraController.SwitchShoulder();
                    Test21_ShoulderSwitchingMounted = cameraController.IsRightShoulder != shoulderBefore;
                    cameraController.SwitchShoulder(); // revert
                }
                RecordResult(21, "Shoulder Switching While Mounted", Test21_ShoulderSwitchingMounted);

                // Test 22: Successful Dismount
                bool dismountSuccess = false;
                if (mountCtrl != null)
                {
                    dismountSuccess = mountCtrl.Dismount();
                    Test22_SuccessfulDismount = dismountSuccess && !mountCtrl.IsMounted;
                }
                RecordResult(22, "Successful Dismount", Test22_SuccessfulDismount);

                // Test 23: Player Movement Restoration
                Test23_PlayerMovementRestoration = playerController != null && playerController.enabled && pCharController != null && pCharController.enabled && pInputReader != null && pInputReader.enabled;
                RecordResult(23, "Player Movement Restoration", Test23_PlayerMovementRestoration);

                // Test 24: Camera Restoration
                Test24_CameraRestoration = cameraController != null && playerController != null && cameraController.Target == playerController.transform;
                RecordResult(24, "Camera Focus Restoration to Player", Test24_CameraRestoration);

                // Test 25: Safe Dismount Position
                if (playerController != null && horseController != null)
                {
                    float dismountDist = Vector3.Distance(playerController.transform.position, horseController.transform.position);
                    bool notParented = playerController.transform.parent == null;
                    Test25_SafeDismountPosition = notParented && dismountDist > 0.8f && dismountDist < 3.5f;
                }
                RecordResult(25, "Safe Dismount Position Validation", Test25_SafeDismountPosition);

                // Test 26: Repeated Mount/Dismount Prevention
                if (mountCtrl != null && playerController != null)
                {
                    bool repeatDismount = mountCtrl.Dismount(); // Already dismounted
                    mountCtrl.Mount(playerController);
                    bool repeatMount = mountCtrl.Mount(playerController); // Already mounted
                    mountCtrl.Dismount(); // Reset to unmounted
                    Test26_RepeatedMountDismountPrevention = !repeatDismount && !repeatMount;
                }
                RecordResult(26, "Repeated Mount/Dismount Prevention", Test26_RepeatedMountDismountPrevention);

                // Test 27: Bootstrap Regression (scene build index 0 exists and valid)
                var scenes = UnityEditor.EditorBuildSettings.scenes;
                Test27_BootstrapRegression = scenes != null && scenes.Length > 0 && scenes[0].enabled && scenes[0].path.Contains("Bootstrap.unity");
                RecordResult(27, "Bootstrap Scene Regression (Index 0)", Test27_BootstrapRegression);

                // Test 28: PlayerTest Regression
                var playerTestAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.SceneAsset>("Assets/_Project/Scenes/Development/PlayerTest.unity");
                Test28_PlayerTestRegression = playerTestAsset != null;
                RecordResult(28, "PlayerTest Scene Regression", Test28_PlayerTestRegression);

                // Test 29: Camera Regression
                Test29_CameraRegression = cameraController != null && cameraController.Config != null && cameraController.CurrentDistance > 1.0f;
                RecordResult(29, "Phase 3 Camera System Regression", Test29_CameraRegression);
            }
            finally
            {
                // CRITICAL FIX: ALWAYS restore player control authority and release test lock
                if (horseController != null)
                {
                    horseController.ExternalInput = false;
                    horseController.StopMovement();
                }
            }

            // Final Summary
            TestsCompleted = true;
            AllTestsPassed = FailedCount == 0;
            TestSummary = AllTestsPassed
                ? $"ALL 29 PHASE 4 TESTS PASSED ({PassedCount}/29)"
                : $"PHASE 4 TESTS: {PassedCount} PASSED, {FailedCount} FAILED";

            Debug.Log($"[RED MOON QA] {TestSummary}");
        }

        private void RecordResult(int testIndex, string testName, bool passed)
        {
            if (passed)
            {
                PassedCount++;
                Debug.Log($"[RED MOON QA] Test {testIndex:D2} - {testName}: PASS");
            }
            else
            {
                FailedCount++;
                Debug.LogError($"[RED MOON QA] Test {testIndex:D2} - {testName}: FAIL");
            }
        }
    }
}
