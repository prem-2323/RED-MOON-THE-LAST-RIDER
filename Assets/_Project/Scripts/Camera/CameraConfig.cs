using UnityEngine;

namespace RedMoon.Camera
{
    /// <summary>
    /// Tunable configuration for the RED MOON cinematic third-person camera.
    /// Encapsulates framing, orbit sensitivity, damping, collision and FOV modulation.
    /// </summary>
    [CreateAssetMenu(fileName = "CameraConfig", menuName = "RED MOON/Camera/Camera Config", order = 11)]
    public sealed class CameraConfig : ScriptableObject
    {
        [Header("Target & Framing")]
        [Tooltip("Focus point offset relative to target pivot (e.g., chest/head height).")]
        public Vector3 focusOffset = new Vector3(0f, 1.4f, 0f);

        [Tooltip("Default lateral offset for right-shoulder framing in metres.")]
        [Range(-2f, 2f)]
        public float defaultShoulderOffset = 0.45f;

        [Tooltip("Alternate lateral offset for left-shoulder framing in metres.")]
        [Range(-2f, 2f)]
        public float leftShoulderOffset = -0.45f;

        [Tooltip("Speed of shoulder transition interpolation in units per second.")]
        [Range(0.5f, 15f)]
        public float shoulderDamping = 4f;

        [Header("Distance & Zoom")]
        [Tooltip("Standard resting distance behind target in metres.")]
        [Range(1f, 10f)]
        public float defaultDistance = 3.2f;

        [Tooltip("Minimum allowable camera distance during zoom or collision.")]
        [Range(0.5f, 5f)]
        public float minDistance = 1.5f;

        [Tooltip("Maximum allowable camera distance during zoom.")]
        [Range(2f, 12f)]
        public float maxDistance = 5.0f;

        [Tooltip("Scroll zoom sensitivity in metres per second.")]
        [Range(0.1f, 10f)]
        public float zoomSpeed = 2f;

        [Header("Rotation & Orbit")]
        [Tooltip("Horizontal rotation sensitivity in degrees per second.")]
        [Range(10f, 360f)]
        public float yawSensitivity = 120f;

        [Tooltip("Vertical rotation sensitivity in degrees per second.")]
        [Range(10f, 360f)]
        public float pitchSensitivity = 100f;

        [Tooltip("Inverts vertical camera look input.")]
        public bool invertY = false;

        [Tooltip("Minimum vertical angle in degrees (looking upward).")]
        [Range(-80f, 0f)]
        public float minPitch = -35f;

        [Tooltip("Maximum vertical angle in degrees (looking downward).")]
        [Range(0f, 85f)]
        public float maxPitch = 65f;

        [Header("Damping & Smoothing")]
        [Tooltip("Position follow smoothing time in seconds (smaller = tighter, larger = looser).")]
        [Range(0.001f, 0.5f)]
        public float positionSmoothing = 0.08f;

        [Tooltip("Rotation smoothing time in seconds.")]
        [Range(0.001f, 0.5f)]
        public float rotationSmoothing = 0.04f;

        [Header("Collision & Obstruction")]
        [Tooltip("SphereCast radius used to detect obstruction geometry.")]
        [Range(0.05f, 0.5f)]
        public float collisionRadius = 0.2f;

        [Tooltip("Safety margin kept between camera and obstructing surface.")]
        [Range(0.01f, 0.5f)]
        public float collisionPadding = 0.15f;

        [Tooltip("Layer mask checked for camera line-of-sight obstruction (default: Environment & Ground).")]
        public LayerMask collisionLayers = new LayerMask { value = (1 << 12) | (1 << 13) };

        [Header("Field of View")]
        [Tooltip("Standard vertical field of view in degrees.")]
        [Range(40f, 90f)]
        public float defaultFOV = 60f;

        [Tooltip("Expanded field of view applied during full sprint.")]
        [Range(40f, 100f)]
        public float sprintFOV = 64f;

        [Tooltip("Interpolation speed for dynamic FOV blending.")]
        [Range(0.5f, 10f)]
        public float fovBlendSpeed = 3f;
    }
}
