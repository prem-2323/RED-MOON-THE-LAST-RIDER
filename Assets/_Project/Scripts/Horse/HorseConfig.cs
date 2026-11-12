using UnityEngine;

namespace RedMoon.Horse
{
    /// <summary>
    /// Tunable configuration for the RED MOON horse locomotion, physics and mounting systems.
    /// Encapsulates all movement speeds, acceleration curves, collision dimensions,
    /// ground detection parameters, mounting offsets and animation parameters.
    /// </summary>
    [CreateAssetMenu(fileName = "HorseConfig", menuName = "RED MOON/Horse/Horse Config", order = 12)]
    public sealed class HorseConfig : ScriptableObject
    {
        [Header("Movement Speeds (m/s)")]
        [Tooltip("Slow walk speed used for subtle navigation and careful pathing.")]
        [Range(0.5f, 6.0f)]
        public float walkSpeed = 2.5f;

        [Tooltip("Standard cruising trot speed.")]
        [Range(2.0f, 10.0f)]
        public float trotSpeed = 5.0f;

        [Tooltip("Full gallop speed for rapid open-world traverse.")]
        [Range(4.0f, 16.0f)]
        public float gallopSpeed = 9.0f;

        [Tooltip("Maximum sprint speed engaged when sprint modifier is held.")]
        [Range(8.0f, 22.0f)]
        public float sprintSpeed = 13.5f;

        [Header("Acceleration & Responsiveness")]
        [Tooltip("Rate of acceleration in m/s^2.")]
        [Range(1.0f, 30.0f)]
        public float acceleration = 8.0f;

        [Tooltip("Rate of deceleration in m/s^2 when movement input ceases.")]
        [Range(1.0f, 40.0f)]
        public float deceleration = 12.0f;

        [Tooltip("Turning rate in degrees per second.")]
        [Range(30.0f, 360.0f)]
        public float turnSpeed = 160.0f;

        [Tooltip("Smoothing time applied to horse rotation.")]
        [Range(0.01f, 0.5f)]
        public float rotationSmoothing = 0.1f;

        [Tooltip("Multiplier applied to acceleration and steering when airborne.")]
        [Range(0.0f, 1.0f)]
        public float airControl = 0.35f;

        [Header("Jump & Gravity")]
        [Tooltip("Peak jump height in metres.")]
        [Range(0.2f, 4.0f)]
        public float jumpHeight = 1.4f;

        [Tooltip("Vertical acceleration applied while airborne.")]
        [Range(-50.0f, -5.0f)]
        public float gravity = -22.0f;

        [Tooltip("Downward force applied while grounded to maintain slope contact.")]
        [Range(-15.0f, 0.0f)]
        public float groundStickForce = -3.0f;

        [Tooltip("Vertical impact speed threshold triggering landing state.")]
        [Range(-25.0f, 0.0f)]
        public float landingImpactThreshold = -3.0f;

        [Tooltip("Duration of the landing recovery state in seconds.")]
        [Range(0.05f, 0.5f)]
        public float landingDuration = 0.2f;

        [Header("Physical Dimensions & CharacterController")]
        [Tooltip("Total height of the horse character controller.")]
        [Range(1.0f, 3.0f)]
        public float height = 2.0f;

        [Tooltip("Radius of the horse character controller.")]
        [Range(0.3f, 1.5f)]
        public float radius = 0.65f;

        [Tooltip("Vertical center of the horse character controller.")]
        [Range(0.5f, 2.0f)]
        public float centerY = 1.0f;

        [Tooltip("Skin width for collision cushion.")]
        [Range(0.01f, 0.2f)]
        public float skinWidth = 0.08f;

        [Tooltip("Maximum step obstacle height the horse can traverse smoothly.")]
        [Range(0.1f, 1.0f)]
        public float stepOffset = 0.5f;

        [Tooltip("Maximum traversable slope angle in degrees.")]
        [Range(10.0f, 60.0f)]
        public float slopeLimit = 45.0f;

        [Header("Ground Detection")]
        [Tooltip("Radius of the ground detection sphere.")]
        [Range(0.05f, 0.8f)]
        public float groundCheckRadius = 0.35f;

        [Tooltip("Vertical offset of the ground check origin relative to base.")]
        [Range(0.0f, 0.5f)]
        public float groundCheckOffset = 0.15f;

        [Tooltip("Layers recognized as traversable ground (Default, Environment, Ground).")]
        public LayerMask groundLayers = new LayerMask { value = (1 << 0) | (1 << 12) | (1 << 13) };

        [Header("Mount & Dismount Dimensions")]
        [Tooltip("Maximum distance from horse at which the player can initiate mounting.")]
        [Range(1.0f, 5.0f)]
        public float mountInteractionRadius = 2.5f;

        [Tooltip("Local position of the rider saddle socket.")]
        public Vector3 riderPositionOffset = new Vector3(0f, 1.25f, -0.1f);

        [Tooltip("Local offset for dismounting to the left.")]
        public Vector3 dismountOffsetLeft = new Vector3(-1.3f, 0f, -0.1f);

        [Tooltip("Local offset for dismounting to the right.")]
        public Vector3 dismountOffsetRight = new Vector3(1.3f, 0f, -0.1f);

        [Tooltip("Duration of the mounting transition in seconds.")]
        [Range(0.1f, 2.0f)]
        public float mountDuration = 0.45f;

        [Tooltip("Duration of the dismounting transition in seconds.")]
        [Range(0.1f, 2.0f)]
        public float dismountDuration = 0.45f;

        [Tooltip("Camera focus offset relative to horse when mounted.")]
        public Vector3 mountedCameraFocusOffset = new Vector3(0f, 1.9f, 0f);

        [Header("Animation & Damping")]
        [Tooltip("Interpolation blend rate for animator floats.")]
        [Range(1.0f, 20.0f)]
        public float animationBlendSpeed = 8.0f;

        [Tooltip("Animator parameter name for forward movement speed.")]
        public string animParamSpeed = "Speed";

        [Tooltip("Animator parameter name for turning direction (-1 left, +1 right).")]
        public string animParamTurn = "Turn";

        [Tooltip("Animator parameter name for grounded state.")]
        public string animParamGrounded = "Grounded";

        [Tooltip("Animator parameter name for vertical velocity.")]
        public string animParamVerticalSpeed = "VerticalSpeed";

        [Tooltip("Animator parameter name for state integer.")]
        public string animParamState = "State";

        [Tooltip("Animator trigger name for jump.")]
        public string animTriggerJump = "Jump";

        [Tooltip("Animator trigger name for landing.")]
        public string animTriggerLand = "Land";

        [Tooltip("Animator trigger name for mount.")]
        public string animTriggerMount = "Mount";

        [Tooltip("Animator trigger name for dismount.")]
        public string animTriggerDismount = "Dismount";
    }
}

