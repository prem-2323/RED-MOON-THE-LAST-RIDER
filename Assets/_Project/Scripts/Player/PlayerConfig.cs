using UnityEngine;

namespace RedMoon.Player
{
    /// <summary>
    /// Tunable player settings for the RED MOON character controller.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerConfig", menuName = "RED MOON/Player/Player Config", order = 10)]
    public sealed class PlayerConfig : ScriptableObject
    {
        [Header("Movement")]
        [Tooltip("Speed used for small analog movement and light stick input.")]
        [Range(0.1f, 10f)]
        public float walkSpeed = 2.2f;

        [Tooltip("Default movement speed for full keyboard/stick input.")]
        [Range(0.1f, 15f)]
        public float runSpeed = 4.8f;

        [Tooltip("Speed used while the Sprint action is held.")]
        [Range(0.1f, 20f)]
        public float sprintSpeed = 7.5f;

        [Tooltip("Horizontal acceleration in metres per second squared.")]
        [Range(1f, 30f)]
        public float acceleration = 12f;

        [Tooltip("Horizontal deceleration in metres per second squared.")]
        [Range(1f, 40f)]
        public float deceleration = 18f;

        [Tooltip("Multiplier applied to acceleration and deceleration while airborne.")]
        [Range(0f, 1f)]
        public float airControl = 0.65f;

        [Tooltip("How quickly the character turns toward the movement direction, in degrees per second.")]
        [Range(10f, 1080f)]
        public float rotationSpeed = 360f;

        [Header("Jump and Gravity")]
        [Tooltip("Peak jump height in metres.")]
        [Range(0.1f, 5f)]
        public float jumpHeight = 1.25f;

        [Tooltip("Vertical acceleration applied while airborne.")]
        [Range(-40f, -1f)]
        public float gravity = -22f;

        [Tooltip("Small downward velocity kept while grounded to preserve contact.")]
        [Range(-10f, 0f)]
        public float groundedStickForce = -2.5f;

        [Tooltip("Duration of the landing state after a significant fall.")]
        [Range(0f, 0.5f)]
        public float landingDuration = 0.18f;

        [Tooltip("Vertical impact speed that triggers the landing state.")]
        [Range(-20f, 0f)]
        public float landingImpactThreshold = -2.5f;

        [Header("Ground Check")]
        [Tooltip("Radius of the sphere used to verify ground contact.")]
        [Range(0.01f, 0.5f)]
        public float groundCheckRadius = 0.22f;

        [Tooltip("Vertical offset of the ground-check point above the controller base.")]
        [Range(0f, 0.5f)]
        public float groundCheckOffset = 0.08f;

        [Header("Character Controller")]
        [Range(0.2f, 1.2f)]
        public float height = 1.8f;

        [Range(0.15f, 0.6f)]
        public float radius = 0.32f;

        [Range(0f, 0.2f)]
        public float skinWidth = 0.08f;

        [Range(0f, 0.5f)]
        public float minMoveDistance = 0.001f;

        [Range(0f, 89f)]
        public float slopeLimit = 50f;

        [Range(0f, 1f)]
        public float stepOffset = 0.35f;

        [Tooltip("Vertical position of the CharacterController centre relative to the root.")]
        [Range(0.5f, 1.5f)]
        public float centerY = 0.9f;

        [Header("State Thresholds")]
        [Range(0.01f, 0.5f)]
        public float walkThreshold = 0.15f;

        [Range(0.5f, 3f)]
        public float runThreshold = 2.5f;

        [Range(3f, 10f)]
        public float sprintThreshold = 5.5f;

        [Header("Layers")]
        [Tooltip("Layers considered ground by the player ground check.")]
        public LayerMask groundMask = new LayerMask { value = (1 << 0) | (1 << 12) | (1 << 13) };
    }
}
