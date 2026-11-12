using UnityEngine;

namespace RedMoon.Player
{
    /// <summary>
    /// Test harness driver for Phase 2 player character verification.
    /// Provides on-screen telemetry, state monitoring and test routines.
    /// </summary>
    public sealed class PlayerTestDriver : MonoBehaviour
    {
        [Tooltip("Target player controller to monitor and test.")]
        public PlayerController player;

        [Tooltip("Whether to draw the on-screen telemetry GUI.")]
        [SerializeField] private bool showTelemetry = true;

        [Tooltip("Whether to log player state transitions to the console.")]
        [SerializeField] private bool logStateChanges = true;

        private void Start()
        {
            if (player == null)
            {
                player = FindAnyObjectByType<PlayerController>();
            }

            if (player != null && logStateChanges)
            {
                player.StateChanged += OnPlayerStateChanged;
            }
        }

        private void OnDestroy()
        {
            if (player != null && logStateChanges)
            {
                player.StateChanged -= OnPlayerStateChanged;
            }
        }

        private void OnPlayerStateChanged(PlayerState newState)
        {
            Debug.Log($"[RED MOON QA] Player transitioned to state: {newState}");
        }

        private void OnGUI()
        {
            if (!showTelemetry || player == null)
            {
                return;
            }

            const float width = 280f;
            const float height = 190f;
            var rect = new Rect(16f, 16f, width, height);

            GUI.Box(rect, "RED MOON — Player Telemetry");

            var layoutRect = new Rect(26f, 42f, width - 20f, height - 30f);
            GUILayout.BeginArea(layoutRect);

            GUILayout.Label($"<b>State:</b> {player.CurrentState}");
            GUILayout.Label($"<b>Speed:</b> {player.HorizontalSpeed:F2} m/s");
            GUILayout.Label($"<b>Vertical Velocity:</b> {player.VerticalVelocity:F2} m/s");
            GUILayout.Label($"<b>Grounded:</b> {player.IsGrounded}");
            GUILayout.Label($"<b>Turn Angle:</b> {player.TurnAngle:F2}");

            GUILayout.Space(6f);
            GUILayout.Label("<color=#AAAAAA>WASD: Move | Shift: Sprint | Space: Jump</color>");

            GUILayout.EndArea();
        }
    }
}

