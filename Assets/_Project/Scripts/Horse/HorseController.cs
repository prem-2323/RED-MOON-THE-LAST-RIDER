using System;
using UnityEngine;
using RedMoon.Camera;

namespace RedMoon.Horse
{
    /// <summary>
    /// CharacterController-based locomotion motor for the RED MOON horse.
    /// Supports 4-tier movement (Walk, Trot, Gallop, Sprint), realistic acceleration curves,
    /// directional turning, slope handling, jumping, and seamless mounting integration.
    /// Eliminates sideways sliding by anchoring momentum to forward direction.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(HorseGroundDetection))]
    public sealed class HorseController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private HorseConfig config;

        [Header("Dependencies")]
        [SerializeField] private HorseInputReader inputReader;
        [SerializeField] private HorseGroundDetection groundDetection;
        [SerializeField] private HorseAnimator horseAnimator;
        [SerializeField] private HorseMountController mountController;

        private CharacterController _characterController;
        private readonly HorseStateMachine _stateMachine = new HorseStateMachine();

        private float _currentForwardSpeed;
        private Vector3 _horizontalVelocity;
        private float _verticalVelocity;
        private float _currentTurnRate;
        private float _turnVelocity;
        private bool _jumpRequested;
        private float _landingTimer;
        private bool _isLanding;

        public HorseConfig Config => config;
        public CharacterController CharacterController => _characterController;
        public HorseStateMachine StateMachine => _stateMachine;
        public HorseState CurrentState => _stateMachine.CurrentState;
        public HorseGroundDetection GroundDetection => groundDetection;
        public HorseMountController MountController => mountController;

        public bool IsGrounded => groundDetection != null && groundDetection.IsGrounded && _verticalVelocity <= 0.1f;
        public Vector3 HorizontalVelocity => _horizontalVelocity;
        public float HorizontalSpeed => Mathf.Abs(_currentForwardSpeed);
        public float ForwardSpeed => _currentForwardSpeed;
        public float VerticalVelocity => _verticalVelocity;
        public float TurnRate => _currentTurnRate;

        /// <summary>
        /// When true, automated test harnesses drive ApplyInput() directly without reading hardware.
        /// </summary>
        public bool ExternalInput { get; set; }

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            groundDetection = GetComponent<HorseGroundDetection>();
            mountController = GetComponent<HorseMountController>();
            horseAnimator = GetComponent<HorseAnimator>();
            inputReader = GetComponent<HorseInputReader>();

            if (config != null)
            {
                ApplyCharacterControllerConfig();
            }

            if (groundDetection != null && config != null)
            {
                groundDetection.Initialize(config, transform.Find("GroundCheck"));
            }
        }

        private void ApplyCharacterControllerConfig()
        {
            if (_characterController == null || config == null)
            {
                return;
            }

            _characterController.height = config.height;
            _characterController.radius = config.radius;
            _characterController.center = new Vector3(0f, config.centerY, 0f);
            _characterController.skinWidth = config.skinWidth;
            _characterController.stepOffset = config.stepOffset;
            _characterController.slopeLimit = config.slopeLimit;
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            _stateMachine.Update(deltaTime);

            if (ExternalInput)
            {
                return;
            }

            HorseInputFrame frame = inputReader != null && inputReader.IsActive
                ? inputReader.ReadFrame()
                : HorseInputFrame.Empty;

            ApplyInput(frame, deltaTime);
        }

        /// <summary>
        /// Applies one frame of input to the horse locomotion motor.
        /// Public so automated QA tests can test movement, turning and speeds deterministically.
        /// </summary>
        public void ApplyInput(HorseInputFrame frame, float deltaTime)
        {
            if (config == null || _characterController == null)
            {
                return;
            }

            // Handle Dismount request
            if (frame.DismountPressed && mountController != null && mountController.IsMounted)
            {
                mountController.Dismount();
                return;
            }

            // 1. Evaluate Ground Contact
            bool wasGrounded = IsGrounded;
            bool groundedNow = groundDetection.CheckGrounded(transform.position);

            if (_verticalVelocity > 0.1f)
            {
                groundedNow = false;
            }

            // Handle Landing transition
            if (!wasGrounded && groundedNow && _verticalVelocity <= config.landingImpactThreshold)
            {
                _isLanding = true;
                _landingTimer = config.landingDuration;
                if (horseAnimator != null)
                {
                    horseAnimator.TriggerLand();
                }
            }

            if (_isLanding)
            {
                _landingTimer -= deltaTime;
                if (_landingTimer <= 0f)
                {
                    _isLanding = false;
                }
            }

            // 2. Turning & Orientation
            Vector2 moveInput = frame.Move;
            float turnInput = moveInput.x;
            float targetTurnRate = turnInput * config.turnSpeed;

            float turnSmooth = Mathf.Max(0.01f, config.rotationSmoothing);
            _currentTurnRate = Mathf.SmoothDamp(_currentTurnRate, targetTurnRate, ref _turnVelocity, turnSmooth);

            if (Mathf.Abs(_currentTurnRate) > 0.01f)
            {
                transform.Rotate(Vector3.up, _currentTurnRate * deltaTime);
            }

            // 3. Forward Locomotion Speed Calculation
            float forwardInput = Mathf.Clamp(moveInput.y, -0.5f, 1.0f);
            float targetSpeed = 0f;

            if (forwardInput > 0.01f)
            {
                if (frame.Sprint)
                {
                    targetSpeed = config.sprintSpeed; // 13.5 m/s
                }
                else if (forwardInput > 0.85f)
                {
                    targetSpeed = config.gallopSpeed; // 9.0 m/s
                }
                else if (forwardInput > 0.35f)
                {
                    targetSpeed = config.trotSpeed; // 5.0 m/s
                }
                else
                {
                    targetSpeed = config.walkSpeed; // 2.5 m/s
                }
            }
            else if (forwardInput < -0.01f)
            {
                targetSpeed = -config.walkSpeed * 0.6f; // Backing speed
            }

            // 4. Direction Alignment & Slope Projecting
            Vector3 forwardDirection = transform.forward;
            if (groundedNow && groundDetection.SlopeAngle > 1.0f && groundDetection.SlopeAngle <= config.slopeLimit)
            {
                forwardDirection = Vector3.ProjectOnPlane(forwardDirection, groundDetection.GroundNormal).normalized;
            }

            // 5. Longitudinal Acceleration & Deceleration (No sideways sliding)
            float accelRate = Mathf.Abs(targetSpeed) > Mathf.Abs(_currentForwardSpeed)
                ? config.acceleration
                : config.deceleration;

            float controlMultiplier = groundedNow ? 1.0f : config.airControl;
            _currentForwardSpeed = Mathf.MoveTowards(_currentForwardSpeed, targetSpeed, accelRate * controlMultiplier * deltaTime);

            _horizontalVelocity = forwardDirection * _currentForwardSpeed;

            // 6. Jump & Gravity
            if (frame.JumpPressed && groundedNow && !_isLanding)
            {
                _verticalVelocity = Mathf.Sqrt(-2.0f * config.gravity * config.jumpHeight);
                _jumpRequested = true;
                groundedNow = false;
                if (horseAnimator != null)
                {
                    horseAnimator.TriggerJump();
                }
            }

            if (!groundedNow || _verticalVelocity > 0.01f)
            {
                _verticalVelocity += config.gravity * deltaTime;
            }
            else if (_verticalVelocity <= 0.1f)
            {
                _verticalVelocity = config.groundStickForce;
            }

            // 7. Execute CharacterController Move
            Vector3 motion = _horizontalVelocity * deltaTime + Vector3.up * _verticalVelocity * deltaTime;
            if (_characterController.enabled)
            {
                _characterController.Move(motion);
            }

            // Post-move ground state refresh
            groundDetection.CheckGrounded(transform.position);

            // 8. Update State Machine
            bool isJumping = _jumpRequested || (!groundedNow && _verticalVelocity > 0.1f);
            bool isMounting = mountController != null && mountController.IsTransitioning && !mountController.IsMounted;
            bool isDismounting = mountController != null && mountController.IsTransitioning && mountController.IsMounted;

            _stateMachine.EvaluateLocomotionState(
                groundedNow,
                HorizontalSpeed,
                _currentTurnRate,
                isJumping,
                _isLanding,
                isMounting,
                isDismounting,
                config
            );

            // 9. Update Animator
            if (horseAnimator != null)
            {
                horseAnimator.UpdateAnimation(HorizontalSpeed, _currentTurnRate, groundedNow, _verticalVelocity, CurrentState, deltaTime);
            }

            _jumpRequested = false;
        }

        public void StopMovement()
        {
            _currentForwardSpeed = 0f;
            _horizontalVelocity = Vector3.zero;
            _verticalVelocity = 0f;
            _currentTurnRate = 0f;
        }
    }
}
