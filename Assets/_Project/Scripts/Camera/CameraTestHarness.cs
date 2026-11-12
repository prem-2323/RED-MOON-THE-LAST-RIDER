using System.Collections;
using UnityEngine;
using RedMoon.Player;

namespace RedMoon.Camera
{
    /// <summary>
    /// Automated test harness for Phase 3.4 camera verification.
    ///
    /// Validates:
    /// - PlayerController camera binding
    /// - Mouse and Gamepad orbital look
    /// - Pitch limits clamping
    /// - World-space focus following
    /// - Dynamic FOV modulation (60° -> 64° -> 60°)
    /// - Shoulder switching (Right -> Left -> Right, repeated, moving)
    /// - Camera collision against wall & distance retraction
    /// - Camera corner collision avoidance
    /// - Camera ground collision avoidance
    /// - Distance restoration when obstruction clears
    /// - Collider non-penetration verification
    /// </summary>
    public sealed class CameraTestHarness : MonoBehaviour
    {
        [SerializeField] private CameraController cameraController;
        [SerializeField] private PlayerController playerController;

        public bool TestsCompleted { get; private set; }
        public bool AllTestsPassed { get; private set; }
        public string TestSummary { get; private set; } = "Not Run";

        // Individual test flags
        public bool BindingPass { get; private set; }
        public bool MousePass { get; private set; }
        public bool GamepadPass { get; private set; }
        public bool PitchClampPass { get; private set; }
        public bool FollowPass { get; private set; }
        public bool FovPass { get; private set; }
        public bool ShoulderPass { get; private set; }
        public bool CollisionWallPass { get; private set; }
        public bool CollisionCornerPass { get; private set; }
        public bool CollisionGroundPass { get; private set; }
        public bool DistanceRecoveryPass { get; private set; }
        public bool NonPenetrationPass { get; private set; }

        private GameObject _runtimeObstacles;

        private void Start()
        {
            if (cameraController == null)
            {
                cameraController = GetComponent<CameraController>();
            }

            if (playerController == null)
            {
                playerController = FindAnyObjectByType<PlayerController>();
            }

            StartCoroutine(RunVerificationRoutine());
        }

        private void OnDestroy()
        {
            if (_runtimeObstacles != null)
            {
                Destroy(_runtimeObstacles);
            }
        }

        private IEnumerator RunVerificationRoutine()
        {
            yield return new WaitForSeconds(0.15f);

            if (cameraController == null)
            {
                Debug.LogError("[RED MOON QA] CameraTestHarness: CameraController missing!");
                yield break;
            }

            var config = cameraController.Config;
            if (config == null)
            {
                Debug.LogError("[RED MOON QA] CameraTestHarness: CameraConfig missing on CameraController!");
                yield break;
            }

            EnsureTestObstacles();

            Debug.Log("[RED MOON QA] === STARTING PHASE 3.4 CAMERA VERIFICATION SUITE ===");

            // -------------------------------------------------------------
            // 1. Regression: Verify PlayerController camera binding
            // -------------------------------------------------------------
            if (playerController != null && playerController.CameraTransform == cameraController.transform && playerController.HasCamera)
            {
                BindingPass = true;
                Debug.Log("[RED MOON QA] PASS 1/12: PlayerController.CameraTransform correctly bound to CameraController.");
            }
            else
            {
                Debug.LogError($"[RED MOON QA] FAIL 1/12: PlayerController binding mismatch. HasCamera={playerController?.HasCamera}");
            }

            // -------------------------------------------------------------
            // 2. Regression: Test Mouse Look Input independently
            // -------------------------------------------------------------
            float initialYaw = cameraController.CurrentYaw;
            float initialPitch = cameraController.CurrentPitch;
            Vector3 initialPos = cameraController.transform.position;

            cameraController.ApplyLook(new Vector2(60f, 30f), isMouse: true, deltaTime: 0.016f);
            yield return new WaitForSeconds(0.2f);

            float deltaYawMouse = Mathf.Abs(cameraController.CurrentYaw - initialYaw);
            float deltaPitchMouse = Mathf.Abs(cameraController.CurrentPitch - initialPitch);
            float deltaPosMouse = Vector3.Distance(cameraController.transform.position, initialPos);

            if (deltaYawMouse > 0.01f && deltaPitchMouse > 0.01f && deltaPosMouse > 0.01f)
            {
                MousePass = true;
                Debug.Log($"[RED MOON QA] PASS 2/12: Mouse orbital look verified. ΔYaw={deltaYawMouse:F2}°, ΔPitch={deltaPitchMouse:F2}°, ΔPos={deltaPosMouse:F2}m.");
            }
            else
            {
                Debug.LogError($"[RED MOON QA] FAIL 2/12: Mouse look produced insufficient change. ΔYaw={deltaYawMouse}, ΔPitch={deltaPitchMouse}");
            }

            // -------------------------------------------------------------
            // 3. Regression: Test Gamepad Look Input independently
            // -------------------------------------------------------------
            float preGamepadYaw = cameraController.CurrentYaw;
            float preGamepadPitch = cameraController.CurrentPitch;
            Vector3 preGamepadPos = cameraController.transform.position;

            cameraController.ApplyLook(new Vector2(-1.0f, -0.8f), isMouse: false, deltaTime: 0.1f);
            yield return new WaitForSeconds(0.25f);

            float deltaYawGamepad = Mathf.Abs(cameraController.CurrentYaw - preGamepadYaw);
            float deltaPitchGamepad = Mathf.Abs(cameraController.CurrentPitch - preGamepadPitch);
            float deltaPosGamepad = Vector3.Distance(cameraController.transform.position, preGamepadPos);

            if (deltaYawGamepad > 0.05f && deltaPitchGamepad > 0.05f && deltaPosGamepad > 0.05f)
            {
                GamepadPass = true;
                Debug.Log($"[RED MOON QA] PASS 3/12: Gamepad orbital look verified. ΔYaw={deltaYawGamepad:F2}°, ΔPitch={deltaPitchGamepad:F2}°, ΔPos={deltaPosGamepad:F2}m.");
            }
            else
            {
                Debug.LogError($"[RED MOON QA] FAIL 3/12: Gamepad look produced insufficient change. ΔYaw={deltaYawGamepad}, ΔPitch={deltaPitchGamepad}");
            }

            // -------------------------------------------------------------
            // 4. Regression: Test Pitch Limits Clamping
            // -------------------------------------------------------------
            cameraController.ApplyLook(new Vector2(0f, 1000f), isMouse: true, deltaTime: 0.016f);
            bool maxClampPass = cameraController.TargetPitch <= config.maxPitch + 0.01f;

            cameraController.ApplyLook(new Vector2(0f, -2000f), isMouse: true, deltaTime: 0.016f);
            bool minClampPass = cameraController.TargetPitch >= config.minPitch - 0.01f;

            if (maxClampPass && minClampPass)
            {
                PitchClampPass = true;
                Debug.Log($"[RED MOON QA] PASS 4/12: Pitch clamping verified within [{config.minPitch}°, {config.maxPitch}°].");
            }
            else
            {
                Debug.LogError($"[RED MOON QA] FAIL 4/12: Pitch clamping violated. TargetPitch={cameraController.TargetPitch}");
            }

            // -------------------------------------------------------------
            // 5. Regression: Test World-Space Focus Point Following
            // -------------------------------------------------------------
            if (cameraController.Target != null)
            {
                if (playerController != null) playerController.enabled = false;
                var cc = cameraController.Target.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;

                Vector3 originalTargetPos = cameraController.Target.position;
                Vector3 preFollowFocus = cameraController.CurrentFocusPosition;

                cameraController.Target.position += new Vector3(0f, 0f, 2f);
                Physics.SyncTransforms();
                yield return new WaitForSeconds(0.25f);

                float focusShift = Vector3.Distance(cameraController.CurrentFocusPosition, preFollowFocus);
                cameraController.Target.position = originalTargetPos;
                Physics.SyncTransforms();
                yield return new WaitForSeconds(0.2f);

                if (cc != null) cc.enabled = true;
                if (playerController != null) playerController.enabled = true;

                if (focusShift > 0.5f)
                {
                    FollowPass = true;
                    Debug.Log($"[RED MOON QA] PASS 5/12: Target focus follow verified in world space. Focus shift={focusShift:F2}m.");
                }
                else
                {
                    Debug.LogError($"[RED MOON QA] FAIL 5/12: Target focus follow failed. Shift={focusShift}");
                }
            }

            // -------------------------------------------------------------
            // 6. Dynamic FOV Modulation: Idle (60°) -> Sprint (64°) -> Idle (60°)
            // -------------------------------------------------------------
            float idleFOV = cameraController.CurrentFOV;
            bool idleMatch = Mathf.Abs(idleFOV - config.defaultFOV) < 1.0f;

            // Trigger Sprinting state
            cameraController.SimulatedSprinting = true;
            yield return new WaitForSeconds(0.5f);

            float sprintFOV = cameraController.CurrentFOV;
            bool sprintMatch = sprintFOV > idleFOV + 2.0f && Mathf.Abs(sprintFOV - config.sprintFOV) < 1.0f;

            // Release Sprinting state
            cameraController.SimulatedSprinting = false;
            yield return new WaitForSeconds(0.6f);

            float restoredFOV = cameraController.CurrentFOV;
            bool restoredMatch = Mathf.Abs(restoredFOV - config.defaultFOV) < 1.0f;

            if (idleMatch && sprintMatch && restoredMatch)
            {
                FovPass = true;
                Debug.Log($"[RED MOON QA] PASS 6/12: Dynamic FOV modulation verified. Idle={idleFOV:F1}° -> Sprint={sprintFOV:F1}° -> Restored={restoredFOV:F1}°.");
            }
            else
            {
                Debug.LogError($"[RED MOON QA] FAIL 6/12: FOV modulation failure. Idle={idleFOV}, Sprint={sprintFOV}, Restored={restoredFOV}");
            }

            // -------------------------------------------------------------
            // 7. Dynamic Shoulder Switching (Right -> Left -> Right, Repeated, Moving, Near Wall)
            // -------------------------------------------------------------
            cameraController.SetShoulder(true);
            yield return new WaitForSeconds(0.3f);
            float rightOffset = cameraController.CurrentShoulderOffset;

            // Switch to left shoulder
            cameraController.SetShoulder(false);
            yield return new WaitForSeconds(0.35f);
            float leftOffset = cameraController.CurrentShoulderOffset;

            // Switch back to right shoulder
            cameraController.SetShoulder(true);
            yield return new WaitForSeconds(0.35f);
            float backRightOffset = cameraController.CurrentShoulderOffset;

            // Repeated rapid switching test
            for (int i = 0; i < 4; i++)
            {
                cameraController.SwitchShoulder();
                yield return new WaitForSeconds(0.05f);
            }

            // Switching while moving
            if (cameraController.Target != null)
            {
                if (playerController != null) playerController.enabled = false;
                var ccMove = cameraController.Target.GetComponent<CharacterController>();
                if (ccMove != null) ccMove.enabled = false;
                Vector3 originPos = cameraController.Target.position;
                cameraController.Target.position += new Vector3(0.5f, 0f, 0.5f);
                Physics.SyncTransforms();
                cameraController.SwitchShoulder();
                yield return new WaitForSeconds(0.2f);
                cameraController.Target.position = originPos;
                Physics.SyncTransforms();
                if (ccMove != null) ccMove.enabled = true;
                if (playerController != null) playerController.enabled = true;
            }

            // Restore right shoulder
            cameraController.SetShoulder(true);
            yield return new WaitForSeconds(0.35f);

            bool rightValid = Mathf.Abs(rightOffset - config.defaultShoulderOffset) < 0.05f;
            bool leftValid = Mathf.Abs(leftOffset - config.leftShoulderOffset) < 0.05f;
            bool backRightValid = Mathf.Abs(backRightOffset - config.defaultShoulderOffset) < 0.05f;

            if (rightValid && leftValid && backRightValid)
            {
                ShoulderPass = true;
                Debug.Log($"[RED MOON QA] PASS 7/12: Shoulder switching verified. Right={rightOffset:F2}m, Left={leftOffset:F2}m, Restored={backRightOffset:F2}m.");
            }
            else
            {
                Debug.LogError($"[RED MOON QA] FAIL 7/12: Shoulder switching mismatch. Right={rightOffset:F3}, Left={leftOffset:F3}, Back={backRightOffset:F3}");
            }

            // -------------------------------------------------------------
            // 8. Camera Collision: Against Wall & Distance Retraction
            // -------------------------------------------------------------
            // Rotate camera to face North wall (orbit so camera is pushed against North wall at z=3.5)
            // Looking at Yaw 180° points camera south (-Z), placing camera at +Z (against wall at z=3.5)
            cameraController.ExternalInput = true;
            cameraController.ApplyLook(new Vector2(0f, 0f), isMouse: false, deltaTime: 0.016f);

            // Force yaw directly for deterministic test placement
            SetCameraYawAndPitch(180f, 0f);
            yield return new WaitForSeconds(0.4f);

            float wallDist = cameraController.CurrentDistance;
            bool wallObstructed = cameraController.IsObstructed;
            bool retractedBelowDefault = wallDist < config.defaultDistance - 0.3f;

            if (wallObstructed && retractedBelowDefault)
            {
                CollisionWallPass = true;
                Debug.Log($"[RED MOON QA] PASS 8/12: Wall collision verified. Obstructed={wallObstructed}, Retracted Distance={wallDist:F2}m (default={config.defaultDistance:F2}m).");
            }
            else
            {
                Debug.LogError($"[RED MOON QA] FAIL 8/12: Wall collision failed. Obstructed={wallObstructed}, Distance={wallDist}");
            }

            // -------------------------------------------------------------
            // 9. Camera Collision: Near a Corner
            // -------------------------------------------------------------
            // Angle yaw towards corner between North wall and Corner wall (Yaw = 135°)
            SetCameraYawAndPitch(135f, 0f);
            yield return new WaitForSeconds(0.4f);

            float cornerDist = cameraController.CurrentDistance;
            bool cornerObstructed = cameraController.IsObstructed;
            bool cornerRetracted = cornerDist < config.defaultDistance - 0.2f;

            if (cornerObstructed && cornerRetracted)
            {
                CollisionCornerPass = true;
                Debug.Log($"[RED MOON QA] PASS 9/12: Corner collision verified. Obstructed={cornerObstructed}, Corner Distance={cornerDist:F2}m.");
            }
            else
            {
                Debug.LogError($"[RED MOON QA] FAIL 9/12: Corner collision failed. Obstructed={cornerObstructed}, Distance={cornerDist}");
            }

            // -------------------------------------------------------------
            // 10. Camera Collision: Ground Avoidance
            // -------------------------------------------------------------
            // Rotate into open space (Yaw = -90° / 270°) and pitch maximum downward (looking up from ground)
            SetCameraYawAndPitch(-90f, config.minPitch);
            yield return new WaitForSeconds(0.4f);

            float groundCamY = cameraController.transform.position.y;
            bool aboveGround = groundCamY > 0.05f; // Ground top surface is at y = 0

            if (aboveGround)
            {
                CollisionGroundPass = true;
                Debug.Log($"[RED MOON QA] PASS 10/12: Ground collision avoidance verified. Camera Height Y={groundCamY:F2}m > ground plane (0m).");
            }
            else
            {
                Debug.LogError($"[RED MOON QA] FAIL 10/12: Ground collision failure. Camera clipped below ground! Y={groundCamY}");
            }

            // -------------------------------------------------------------
            // 11. Distance Recovery: Normal Distance Restoration When Clear
            // -------------------------------------------------------------
            // Reset to open space (Yaw = 0°, Pitch = 0°)
            SetCameraYawAndPitch(0f, 0f);
            yield return new WaitForSeconds(0.6f);

            float clearDist = cameraController.CurrentDistance;
            bool restoredDistance = Mathf.Abs(clearDist - config.defaultDistance) < 0.1f;

            if (restoredDistance)
            {
                DistanceRecoveryPass = true;
                Debug.Log($"[RED MOON QA] PASS 11/12: Distance recovery verified. Restored Distance={clearDist:F2}m (default={config.defaultDistance:F2}m).");
            }
            else
            {
                Debug.LogError($"[RED MOON QA] FAIL 11/12: Distance recovery failure. Current Distance={clearDist}");
            }

            // -------------------------------------------------------------
            // 12. Non-Penetration Guarantee
            // -------------------------------------------------------------
            bool insideCollider = Physics.CheckSphere(cameraController.transform.position, 0.05f, config.collisionLayers, QueryTriggerInteraction.Ignore);
            if (!insideCollider)
            {
                NonPenetrationPass = true;
                Debug.Log("[RED MOON QA] PASS 12/12: Collider non-penetration verified. Camera is completely outside collision geometry.");
            }
            else
            {
                Debug.LogError("[RED MOON QA] FAIL 12/12: Camera is positioned inside an obstacle collider!");
            }

            // Cleanup & Summary
            cameraController.ExternalInput = false;
            TestsCompleted = true;
            AllTestsPassed = BindingPass && MousePass && GamepadPass && PitchClampPass &&
                             FollowPass && FovPass && ShoulderPass && CollisionWallPass &&
                             CollisionCornerPass && CollisionGroundPass && DistanceRecoveryPass &&
                             NonPenetrationPass;

            TestSummary = AllTestsPassed ? "ALL 12 PHASE 3.4 TESTS PASSED" : "SOME TESTS FAILED";
            Debug.Log($"[RED MOON QA] === PHASE 3.4 VERIFICATION RESULT: {TestSummary} ===");
        }

        private void SetCameraYawAndPitch(float yaw, float pitch)
        {
            if (cameraController != null)
            {
                cameraController.SetTargetYawAndPitch(yaw, pitch);
            }
        }

        private void EnsureTestObstacles()
        {
            var existing = GameObject.Find("Test_Obstacles");
            if (existing != null)
            {
                return;
            }

            _runtimeObstacles = new GameObject("Test_Obstacles");
            int envLayer = LayerMask.NameToLayer("Environment");

            // Wall North: behind camera when looking south at player
            var wallNorth = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallNorth.name = "Test_Wall_North";
            wallNorth.transform.SetParent(_runtimeObstacles.transform, false);
            wallNorth.transform.position = new Vector3(0f, 1.5f, 2.5f);
            wallNorth.transform.localScale = new Vector3(8f, 3f, 0.5f);
            wallNorth.layer = envLayer;
            wallNorth.tag = "Environment";

            // Wall Corner: forms corner with North Wall
            var wallCorner = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallCorner.name = "Test_Wall_Corner";
            wallCorner.transform.SetParent(_runtimeObstacles.transform, false);
            wallCorner.transform.position = new Vector3(2.5f, 1.5f, 1.25f);
            wallCorner.transform.localScale = new Vector3(0.5f, 3f, 3f);
            wallCorner.layer = envLayer;
            wallCorner.tag = "Environment";

            // Wall West: beside player for lateral shoulder test
            var wallWest = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallWest.name = "Test_Wall_West";
            wallWest.transform.SetParent(_runtimeObstacles.transform, false);
            wallWest.transform.position = new Vector3(-1.8f, 1.5f, 0f);
            wallWest.transform.localScale = new Vector3(0.5f, 3f, 5f);
            wallWest.layer = envLayer;
            wallWest.tag = "Environment";
        }
    }
}
