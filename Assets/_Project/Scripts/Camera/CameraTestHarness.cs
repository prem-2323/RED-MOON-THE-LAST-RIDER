using System.Collections;
using UnityEngine;
using RedMoon.Player;

namespace RedMoon.Camera
{
    /// <summary>
    /// Automated test harness for Phase 3.3 third-person orbital camera verification.
    /// Runs tests in Play Mode to verify mouse input, gamepad input, pitch clamping,
    /// target following and PlayerController binding independently.
    /// </summary>
    public sealed class CameraTestHarness : MonoBehaviour
    {
        [SerializeField] private CameraController cameraController;
        [SerializeField] private PlayerController playerController;

        public bool TestsCompleted { get; private set; }
        public bool AllTestsPassed { get; private set; }
        public string TestSummary { get; private set; } = "Not Run";

        private void Start()
        {
            if (cameraController == null)
            {
                cameraController = GetComponent<CameraController>();
            }

            if (playerController == null)
            {
                playerController = FindFirstObjectByType<PlayerController>();
            }

            StartCoroutine(RunVerificationRoutine());
        }

        private IEnumerator RunVerificationRoutine()
        {
            yield return new WaitForSeconds(0.1f);

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

            bool mousePass = false;
            bool gamepadPass = false;
            bool pitchClampPass = false;
            bool followPass = false;
            bool bindingPass = false;

            // 1. Verify PlayerController camera binding
            if (playerController != null && playerController.CameraTransform == cameraController.transform && playerController.HasCamera)
            {
                bindingPass = true;
                Debug.Log("[RED MOON QA] PASS: PlayerController.CameraTransform correctly bound to CameraController.");
            }
            else
            {
                Debug.LogError($"[RED MOON QA] FAIL: PlayerController binding mismatch. HasCamera={playerController?.HasCamera}");
            }

            // 2. Test Mouse Look Input independently
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
                mousePass = true;
                Debug.Log($"[RED MOON QA] PASS: Mouse orbital look verified. ΔYaw={deltaYawMouse:F2}°, ΔPitch={deltaPitchMouse:F2}°, ΔPos={deltaPosMouse:F2}m.");
            }
            else
            {
                Debug.LogError($"[RED MOON QA] FAIL: Mouse look produced insufficient change. ΔYaw={deltaYawMouse}, ΔPitch={deltaPitchMouse}");
            }

            // 3. Test Gamepad Look Input independently
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
                gamepadPass = true;
                Debug.Log($"[RED MOON QA] PASS: Gamepad orbital look verified. ΔYaw={deltaYawGamepad:F2}°, ΔPitch={deltaPitchGamepad:F2}°, ΔPos={deltaPosGamepad:F2}m.");
            }
            else
            {
                Debug.LogError($"[RED MOON QA] FAIL: Gamepad look produced insufficient change. ΔYaw={deltaYawGamepad}, ΔPitch={deltaPitchGamepad}");
            }

            // 4. Test Pitch Limits Clamping
            cameraController.ApplyLook(new Vector2(0f, 1000f), isMouse: true, deltaTime: 0.016f);
            bool maxClampPass = cameraController.TargetPitch <= config.maxPitch + 0.01f;

            cameraController.ApplyLook(new Vector2(0f, -2000f), isMouse: true, deltaTime: 0.016f);
            bool minClampPass = cameraController.TargetPitch >= config.minPitch - 0.01f;

            if (maxClampPass && minClampPass)
            {
                pitchClampPass = true;
                Debug.Log($"[RED MOON QA] PASS: Pitch clamping verified within [{config.minPitch}°, {config.maxPitch}°].");
            }
            else
            {
                Debug.LogError($"[RED MOON QA] FAIL: Pitch clamping violated. TargetPitch={cameraController.TargetPitch}");
            }

            // 5. Test World-Space Focus Point Following
            if (cameraController.Target != null)
            {
                Vector3 originalTargetPos = cameraController.Target.position;
                Vector3 preFollowFocus = cameraController.CurrentFocusPosition;

                // Move target temporarily by 2m
                cameraController.Target.position += new Vector3(0f, 0f, 2f);

                yield return new WaitForSeconds(0.25f);

                float focusShift = Vector3.Distance(cameraController.CurrentFocusPosition, preFollowFocus);

                // Restore position
                cameraController.Target.position = originalTargetPos;
                yield return new WaitForSeconds(0.2f);

                if (focusShift > 0.5f)
                {
                    followPass = true;
                    Debug.Log($"[RED MOON QA] PASS: Target focus follow verified in world space. Focus shift={focusShift:F2}m.");
                }
                else
                {
                    Debug.LogError($"[RED MOON QA] FAIL: Target focus follow failed. Shift={focusShift}");
                }
            }

            TestsCompleted = true;
            AllTestsPassed = mousePass && gamepadPass && pitchClampPass && followPass && bindingPass;

            TestSummary = AllTestsPassed ? "ALL PHASE 3.3 TESTS PASSED" : "SOME TESTS FAILED";
            Debug.Log($"[RED MOON QA] Phase 3.3 Camera Verification Result: {TestSummary}");
        }
    }
}
