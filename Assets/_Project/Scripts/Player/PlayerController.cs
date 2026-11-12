using System;
using UnityEngine;

namespace RedMoon.Player
{
    /// <summary>
    /// CharacterController-based player motor for RED MOON.
    ///
    /// The controller consumes PlayerInputFrame values instead of reading
    /// devices directly. Movement is camera-relative when a camera transform
    /// is assigned, and remains world-relative otherwise.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private PlayerConfig config;
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private Transform groundCheckPoint;
        [SerializeField] private LayerMask groundMask = new LayerMask { value = (1 << 0) | (1 << 12) | (1 << 13) };
        [SerializeField] private bool drawGizmos = true;

        private CharacterController _characterController;
        private readonly PlayerStateMachine _stateMachine = new PlayerStateMachine();
        private Vector3 _horizontalVelocity;
        private float _verticalVelocity;
        private bool _jumpRequested;
        private Vector3 _lastMoveDirection;
        private float _turnAngle;

        public PlayerConfig Config => config;
        public CharacterController CharacterController => _characterController;
        public PlayerState CurrentState => _stateMachine.CurrentState;
        public bool IsGrounded { get; private set; }
        public Vector3 HorizontalVelocity => _horizontalVelocity;
        public float VerticalVelocity => _verticalVelocity;
        public float HorizontalSpeed => _horizontalVelocity.magnitude;
        public Vector3 LastMoveDirection => _lastMoveDirection;
        public float TurnAngle => _turnAngle;
        public bool HasCamera => cameraTransform != null;

        /// <summary>
        /// When true, Update() does not apply input or gravity. Editor tests can
        /// drive ApplyInput() directly.
        /// </summary>
        public bool ExternalInput { get; set; }

        public event Action<PlayerState> StateChanged;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();

            if (config == null)
            {
                Debug.LogError("[RED MOON] PlayerController has no PlayerConfig assigned.", this);
                enabled = false;
                return;
            }

            ApplyCharacterControllerConfig();

            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }

            if (groundCheckPoint == null)
            {
                var groundCheck = new GameObject("GroundCheckPoint");
                groundCheck.transform.SetParent(transform, false);
                groundCheck.transform.localPosition = new Vector3(0f, config.centerY - config.height * 0.5f + config.groundCheckOffset, 0f);
                groundCheckPoint = groundCheck.transform;
            }

            IsGrounded = true;
        }

        private void OnValidate()
        {
            if (config == null)
            {
                return;
            }

            var controller = _characterController != null ? _characterController : GetComponent<CharacterController>();
            if (controller != null)
            {
                ApplyCharacterControllerConfig(controller);
            }
        }

        private void ApplyCharacterControllerConfig()
        {
            ApplyCharacterControllerConfig(_characterController);
        }

        private void ApplyCharacterControllerConfig(CharacterController controller)
        {
            if (controller == null)
            {
                return;
            }

            controller.height = config.height;
            controller.radius = config.radius;
            controller.center = new Vector3(0f, config.centerY, 0f);
            controller.slopeLimit = config.slopeLimit;
            controller.stepOffset = config.stepOffset;
            controller.skinWidth = config.skinWidth;
            controller.minMoveDistance = config.minMoveDistance;
        }

        private void Update()
        {
            if (ExternalInput)
            {
                return;
            }

            float deltaTime = Time.deltaTime;

            if (inputReader != null && inputReader.enabled)
            {
                ApplyInput(inputReader.ReadFrame(), deltaTime);
                return;
            }

            if (!IsGrounded)
            {
                _verticalVelocity += config.gravity * deltaTime;
            }

            bool wasGrounded = IsGrounded;
            _characterController.Move(Vector3.up * _verticalVelocity * deltaTime);
            UpdateGroundState();

            bool justLanded = !wasGrounded && IsGrounded;
            float impactVelocity = _verticalVelocity;

            if (IsGrounded && _verticalVelocity <= 0.1f)
            {
                _verticalVelocity = config.groundedStickForce;
            }

            UpdateState(deltaTime, justLanded && impactVelocity <= config.landingImpactThreshold, false);
        }

        /// <summary>
        /// Applies one input frame. Public so editor tests can drive the motor
        /// without simulating hardware input.
        /// </summary>
        public void ApplyInput(PlayerInputFrame frame, float deltaTime)
        {
            if (config == null || _characterController == null)
            {
                return;
            }

            Vector2 move = frame.Move;
            if (move.sqrMagnitude > 1f)
            {
                move.Normalize();
            }

            float moveMagnitude = move.magnitude;
            Vector3 desiredDirection = CalculateMoveDirection(move);
            float targetSpeed = CalculateTargetSpeed(moveMagnitude, frame.SprintPressed);
            Vector3 targetVelocity = desiredDirection * targetSpeed;

            float control = IsGrounded ? 1f : config.airControl;
            float acceleration = targetVelocity.sqrMagnitude > 0.001f ? config.acceleration : config.deceleration;
            _horizontalVelocity = Vector3.MoveTowards(_horizontalVelocity, targetVelocity, acceleration * control * deltaTime);

            if (frame.JumpPressed && IsGrounded)
            {
                _verticalVelocity = Mathf.Sqrt(-2f * config.gravity * config.jumpHeight);
                _jumpRequested = true;
            }

            if (!IsGrounded)
            {
                _verticalVelocity += config.gravity * deltaTime;
            }

            Vector3 motion = _horizontalVelocity * deltaTime + Vector3.up * _verticalVelocity * deltaTime;
            bool wasGrounded = IsGrounded;
            _characterController.Move(motion);
            UpdateGroundState();

            bool justLanded = !wasGrounded && IsGrounded;
            float impactVelocity = _verticalVelocity;

            if (IsGrounded && _verticalVelocity <= 0.1f)
            {
                _verticalVelocity = config.groundedStickForce;
            }

            UpdateState(deltaTime, justLanded && impactVelocity <= config.landingImpactThreshold, _jumpRequested);
            UpdateRotation(desiredDirection, deltaTime);

            _jumpRequested = false;
        }

        private Vector3 CalculateMoveDirection(Vector2 move)
        {
            Vector3 forward;
            Vector3 right;

            if (cameraTransform != null)
            {
                forward = cameraTransform.forward;
                right = cameraTransform.right;
            }
            else
            {
                forward = transform.forward;
                right = transform.right;
            }

            forward.y = 0f;
            right.y = 0f;

            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = transform.forward;
                forward.y = 0f;
            }

            if (right.sqrMagnitude < 0.0001f)
            {
                right = transform.right;
                right.y = 0f;
            }

            forward.Normalize();
            right.Normalize();

            Vector3 direction = forward * move.y + right * move.x;
            if (direction.sqrMagnitude > 1f)
            {
                direction.Normalize();
            }

            return direction;
        }

        private float CalculateTargetSpeed(float moveMagnitude, bool sprint)
        {
            if (moveMagnitude < config.walkThreshold)
            {
                return 0f;
            }

            if (sprint)
            {
                return config.sprintSpeed;
            }

            return moveMagnitude > 0.55f ? config.runSpeed : config.walkSpeed;
        }

        private void UpdateGroundState()
        {
            bool sphereGrounded = false;

            if (groundCheckPoint != null)
            {
                sphereGrounded = Physics.CheckSphere(
                    groundCheckPoint.position,
                    config.groundCheckRadius,
                    groundMask.value,
                    QueryTriggerInteraction.Ignore);
            }

            IsGrounded = _characterController.isGrounded || sphereGrounded;
        }

        private void UpdateState(float deltaTime, bool justLanded, bool jumpRequested)
        {
            PlayerState previous = _stateMachine.CurrentState;
            PlayerState current = _stateMachine.Update(
                IsGrounded,
                justLanded,
                _verticalVelocity,
                HorizontalSpeed,
                jumpRequested,
                deltaTime,
                config);

            if (current != previous)
            {
                StateChanged?.Invoke(current);
            }
        }

        private void UpdateRotation(Vector3 desiredDirection, float deltaTime)
        {
            if (desiredDirection.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(desiredDirection, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, config.rotationSpeed * deltaTime);
                _turnAngle = Vector3.SignedAngle(transform.forward, desiredDirection, Vector3.up) / 180f;
                _lastMoveDirection = desiredDirection;
            }
            else
            {
                _turnAngle = Mathf.MoveTowards(_turnAngle, 0f, config.rotationSpeed * deltaTime * 0.5f);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos || config == null)
            {
                return;
            }

            if (groundCheckPoint != null)
            {
                Gizmos.color = IsGrounded ? Color.green : Color.red;
                Gizmos.DrawWireSphere(groundCheckPoint.position, config.groundCheckRadius);
            }

            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position + Vector3.up * 0.2f, _lastMoveDirection * 1.5f);
        }
    }
}
