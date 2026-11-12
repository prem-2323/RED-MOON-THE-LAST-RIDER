using UnityEngine;
using RedMoon.Camera;

namespace RedMoon.Horse
{
    /// <summary>
    /// Debug telemetry overlay for Phase 4 Horse testing.
    /// Displays Horse State, Speed, Grounded status, IsMounted status, Camera Target, and Movement Mode.
    /// </summary>
    public sealed class HorseDebugUI : MonoBehaviour
    {
        [SerializeField] private HorseController horseController;
        [SerializeField] private HorseMountController mountController;
        [SerializeField] private CameraController cameraController;
        [SerializeField] private bool showUI = true;

        private GUIStyle _boxStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _labelStyle;

        private void Awake()
        {
            if (horseController == null) horseController = GetComponent<HorseController>();
            if (mountController == null) mountController = GetComponent<HorseMountController>();
            if (cameraController == null) cameraController = FindAnyObjectByType<CameraController>();
        }

        private void OnGUI()
        {
            if (!showUI)
            {
                return;
            }

            InitStyles();

            GUILayout.BeginArea(new Rect(15, 15, 340, 260), _boxStyle);
            GUILayout.Label("RED MOON — HORSE TELEMETRY (PHASE 4)", _headerStyle);
            GUILayout.Space(6);

            string stateStr = horseController != null ? horseController.CurrentState.ToString() : "N/A";
            float speed = horseController != null ? horseController.HorizontalSpeed : 0f;
            bool isGrounded = horseController != null && horseController.IsGrounded;
            bool isMounted = mountController != null && mountController.IsMounted;
            string camTarget = cameraController != null && cameraController.Target != null ? cameraController.Target.name : "None";

            string movementMode = "Idle";
            if (speed > 10.0f) movementMode = "Sprint";
            else if (speed > 6.0f) movementMode = "Gallop";
            else if (speed > 3.0f) movementMode = "Trot";
            else if (speed > 0.2f) movementMode = "Walk";

            GUILayout.Label($"Horse State: {stateStr}", _labelStyle);
            GUILayout.Label($"Movement Mode: {movementMode}", _labelStyle);
            GUILayout.Label($"Speed: {speed:F2} m/s", _labelStyle);
            GUILayout.Label($"Grounded: {isGrounded} (Slope: {(horseController?.GroundDetection != null ? horseController.GroundDetection.SlopeAngle.ToString("F1") : "0")}°)", _labelStyle);
            GUILayout.Label($"IsMounted: {isMounted}", _labelStyle);
            GUILayout.Label($"Camera Target: {camTarget}", _labelStyle);
            GUILayout.Label($"Camera Distance: {(cameraController != null ? cameraController.CurrentDistance.ToString("F2") : "0")} m", _labelStyle);

            GUILayout.Space(6);
            GUILayout.Label("Controls: WASD=Move | Shift=Sprint | Space=Jump | E=Mount/Dismount", _labelStyle);

            GUILayout.EndArea();
        }

        private void InitStyles()
        {
            if (_boxStyle == null)
            {
                _boxStyle = new GUIStyle(GUI.skin.box);
                _boxStyle.padding = new RectOffset(12, 12, 10, 10);
            }

            if (_headerStyle == null)
            {
                _headerStyle = new GUIStyle(GUI.skin.label);
                _headerStyle.fontStyle = FontStyle.Bold;
                _headerStyle.fontSize = 13;
                _headerStyle.normal.textColor = new Color(0.9f, 0.25f, 0.25f);
            }

            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(GUI.skin.label);
                _labelStyle.fontSize = 11;
                _labelStyle.normal.textColor = Color.white;
            }
        }
    }
}

