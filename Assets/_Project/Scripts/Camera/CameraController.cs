using System;
using UnityEngine;
using UnityEngine.InputSystem;
using RedMoon.Core.Input;
using RedMoon.Player;

namespace RedMoon.Camera
{
    /// <summary>
    /// Custom third-person cinematic camera motor for RED MOON: THE LAST RIDER.
    ///
    /// Phase 3.4 Implementation:
    /// - Orbital look with yaw and pitch damping.
    /// - World-space focus point follow.
    /// - Dynamic shoulder switching (right: +0.45m, left: -0.45m) with smooth damping.
    /// - SphereCast line-of-sight obstruction detection against Environment (12) and Ground (13).
    /// - Rapid retraction on obstruction, smooth distance recovery when clear.
    /// - Dynamic sprint FOV modulation (60° -> 64°).
    /// - Safe lateral wall avoidance for shoulder switching.
    /// - Automatic cursor locking with Escape/Click toggle.
    /// - Robust mouse and gamepad input handling with zero GC allocations.
    /// </summary>
    [RequireComponent(typeof(UnityEngine.Camera))]
    public sealed class CameraController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private CameraConfig config;

        [Header("Target")]
        [Tooltip("Target transform to follow (defaults to Player if unassigned).")]
        [SerializeField] private Transform target;

        [Tooltip("PlayerController to bind this camera to for camera-relative steering.")]
        [SerializeField] private PlayerController playerController;

        [Header("Input")]
        [Tooltip("Canonical Input Actions asset (optional if target has PlayerInputReader).")]
        [SerializeField] private InputActionAsset inputActions;

        [Header("Cursor Settings")]
        [Tooltip("Whether to lock and hide the mouse cursor for third-person mouse look.")]
        [SerializeField] private bool lockCursor = true;

        private RedMoonInput _input;
        private bool _ownsInput;
        private UnityEngine.Camera _cameraComponent;

        // Orbit state
        private float _targetYaw;
        private float _targetPitch;
        private float _currentYaw;
        private float _currentPitch;
        private float _yawVelocity;
        private float _pitchVelocity;
        private Vector3 _currentFocusPosition;
        private Vector3 _positionVelocity;

        // Distance & collision state
        private float _currentDistance;
        private float _targetDistance;
        private float _distanceVelocity;
        private bool _isObstructed;
        private RaycastHit _lastCollisionHit;

        // Shoulder state
        private float _currentShoulderOffset;
        private float _targetShoulderOffset;
        private bool _isRightShoulder = true;

        public CameraConfig Config => config;
        public Transform Target => target;
        public PlayerController PlayerController => playerController;
        public UnityEngine.Camera CameraComponent => _cameraComponent;

        public float CurrentYaw => _currentYaw;
        public float CurrentPitch => _currentPitch;
        public float TargetYaw => _targetYaw;
        public float TargetPitch => _targetPitch;
        public Vector3 CurrentFocusPosition => _currentFocusPosition;

        // Collision & Distance properties
        public float CurrentDistance => _currentDistance;
        public float TargetDistance => _targetDistance;
        public bool IsObstructed => _isObstructed;
        public RaycastHit LastCollisionHit => _lastCollisionHit;

        // Shoulder properties
        public float CurrentShoulderOffset => _currentShoulderOffset;
        public float TargetShoulderOffset => _targetShoulderOffset;
        public bool IsRightShoulder => _isRightShoulder;

        // FOV property
        public float CurrentFOV => _cameraComponent != null ? _cameraComponent.fieldOfView : 60f;

        /// <summary>
        /// When true, automated tests can drive ApplyLook(), SwitchShoulder() directly without hardware.
        /// </summary>
        public bool ExternalInput { get; set; }

        /// <summary>
        /// When true, simulates the player sprinting state for automated FOV testing.
        /// </summary>
        public bool SimulatedSprinting { get; set; }

        public bool LockCursorEnabled
        {
            get => lockCursor;
            set => lockCursor = value;
        }

        private void Awake()
        {
            _cameraComponent = GetComponent<UnityEngine.Camera>();

            ResolveTargetAndPlayer();
            InitializeInput();
            InitializeOrbitState();
        }

        private void Start()
        {
            BindToPlayer();

            if (_input == null)
            {
                InitializeInput();
            }

            if (lockCursor && !ExternalInput)
            {
                SetCursorLocked(true);
            }
        }

        private void Update()
        {
            if (_input == null)
            {
                InitializeInput();
            }

            HandleCursorLock();

            if (!ExternalInput)
            {
                ProcessLookInput(Time.deltaTime);
            }
        }

        private void HandleCursorLock()
        {
            if (ExternalInput || !lockCursor)
            {
                return;
            }

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                SetCursorLocked(false);
            }
            else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                SetCursorLocked(true);
            }
        }

        public void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        private void OnDestroy()
        {
            if (_input != null)
            {
                try
                {
                    _input.SwitchShoulder.performed -= OnSwitchShoulderPerformed;
                }
                catch
                {
                    // Action may have been disposed
                }
            }

            if (_ownsInput)
            {
                _input?.Dispose();
            }
        }

        private void ResolveTargetAndPlayer()
        {
            if (target == null)
            {
                var playerObject = GameObject.FindWithTag("Player");
                if (playerObject != null)
                {
                    target = playerObject.transform;
                }
            }

            if (playerController == null && target != null)
            {
                playerController = target.GetComponent<PlayerController>();
            }
        }

        private void InitializeInput()
        {
            if (target != null)
            {
                var reader = target.GetComponent<PlayerInputReader>();
                if (reader != null && reader.Input != null)
                {
                    if (_input != null && _input != reader.Input)
                    {
                        try { _input.SwitchShoulder.performed -= OnSwitchShoulderPerformed; } catch { }
                    }
                    _input = reader.Input;
                    _ownsInput = false;
                    _input.SwitchShoulder.performed += OnSwitchShoulderPerformed;
                    return;
                }
            }

            if (inputActions != null && _input == null)
            {
                _input = new RedMoonInput(inputActions);
                _input.EnableGameplay();
                _ownsInput = true;
                _input.SwitchShoulder.performed += OnSwitchShoulderPerformed;
            }
        }

        private void InitializeOrbitState()
        {
            Vector3 angles = transform.eulerAngles;
            _targetYaw = angles.y;
            _targetPitch = NormalizePitch(angles.x);

            if (config != null)
            {
                _targetPitch = Mathf.Clamp(_targetPitch, config.minPitch, config.maxPitch);
                _currentDistance = config.defaultDistance;
                _targetDistance = _currentDistance;
                _isRightShoulder = true;
                _currentShoulderOffset = config.defaultShoulderOffset;
                _targetShoulderOffset = _currentShoulderOffset;

                if (_cameraComponent != null)
                {
                    _cameraComponent.fieldOfView = config.defaultFOV;
                }
            }
            else
            {
                _currentDistance = 3.2f;
                _targetDistance = 3.2f;
                _isRightShoulder = true;
                _currentShoulderOffset = 0.45f;
                _targetShoulderOffset = 0.45f;
            }

            _currentYaw = _targetYaw;
            _currentPitch = _targetPitch;

            if (target != null && config != null)
            {
                _currentFocusPosition = target.position + config.focusOffset;
            }
            else if (target != null)
            {
                _currentFocusPosition = target.position;
            }
            else
            {
                _currentFocusPosition = transform.position;
            }
        }

        private void BindToPlayer()
        {
            if (playerController != null)
            {
                playerController.SetCamera(transform);
            }
        }

        private void OnSwitchShoulderPerformed(InputAction.CallbackContext context)
        {
            if (ExternalInput)
            {
                return;
            }
            SwitchShoulder();
        }

        /// <summary>
        /// Toggles between right and left shoulder framing.
        /// </summary>
        public void SwitchShoulder()
        {
            SetShoulder(!_isRightShoulder);
        }

        /// <summary>
        /// Explicitly sets right (true) or left (false) shoulder framing.
        /// </summary>
        public void SetShoulder(bool rightShoulder)
        {
            _isRightShoulder = rightShoulder;
            if (config != null)
            {
                _targetShoulderOffset = _isRightShoulder ? config.defaultShoulderOffset : config.leftShoulderOffset;
            }
        }

        private void LateUpdate()
        {
            if (target == null || config == null)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            UpdateCamera(deltaTime);
        }

        private void ProcessLookInput(float deltaTime)
        {
            Vector2 mouseDelta = Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
            Vector2 actionLook = _input != null ? _input.CameraLook.ReadValue<Vector2>() : Vector2.zero;

            Vector2 lookDelta = Vector2.zero;
            bool isMouse = false;

            if (mouseDelta.sqrMagnitude > 0.0001f)
            {
                lookDelta = mouseDelta;
                isMouse = true;
            }
            else if (actionLook.sqrMagnitude > 0.0001f)
            {
                lookDelta = actionLook;
                var control = _input != null ? _input.CameraLook.activeControl : null;
                isMouse = control != null ? control.device is Mouse : (Gamepad.current == null || Gamepad.current.rightStick.ReadValue().sqrMagnitude < 0.01f);
            }

            if (lookDelta.sqrMagnitude < 0.00001f)
            {
                return;
            }

            ApplyLook(lookDelta, isMouse, deltaTime);
        }

        /// <summary>
        /// Applies look delta to orbit yaw and pitch.
        /// Public so automated tests can test mouse and gamepad inputs independently.
        /// </summary>
        public void ApplyLook(Vector2 rawLook, bool isMouse, float deltaTime)
        {
            if (config == null)
            {
                return;
            }

            float yawDelta;
            float pitchDelta;

            if (isMouse)
            {
                yawDelta = rawLook.x * (config.yawSensitivity * 0.01f);
                pitchDelta = (config.invertY ? rawLook.y : -rawLook.y) * (config.pitchSensitivity * 0.01f);
            }
            else
            {
                yawDelta = rawLook.x * config.yawSensitivity * deltaTime;
                pitchDelta = (config.invertY ? rawLook.y : -rawLook.y) * config.pitchSensitivity * deltaTime;
            }

            _targetYaw += yawDelta;
            _targetPitch = Mathf.Clamp(_targetPitch + pitchDelta, config.minPitch, config.maxPitch);
        }

        private void UpdateCamera(float deltaTime)
        {
            // 1. Rotation smoothing
            float rotSmoothing = Mathf.Max(0.001f, config.rotationSmoothing);
            _currentYaw = Mathf.SmoothDampAngle(_currentYaw, _targetYaw, ref _yawVelocity, rotSmoothing);
            _currentPitch = Mathf.SmoothDampAngle(_currentPitch, _targetPitch, ref _pitchVelocity, rotSmoothing);
            Quaternion orbitRotation = Quaternion.Euler(_currentPitch, _currentYaw, 0f);

            // 2. Focus point smoothing
            Vector3 targetFocus = target.position + config.focusOffset;
            float posSmoothing = Mathf.Max(0.001f, config.positionSmoothing);
            _currentFocusPosition = Vector3.SmoothDamp(_currentFocusPosition, targetFocus, ref _positionVelocity, posSmoothing);

            // 3. Dynamic shoulder offset smoothing
            float shoulderSpeed = Mathf.Max(0.5f, config.shoulderDamping);
            _currentShoulderOffset = Mathf.MoveTowards(_currentShoulderOffset, _targetShoulderOffset, shoulderSpeed * deltaTime);

            // 4. Safe lateral wall check to prevent shoulder offset clipping into walls
            Vector3 lateralDir = orbitRotation * Vector3.right;
            float lateralDist = Mathf.Abs(_currentShoulderOffset);
            Vector3 safeShoulderVector = lateralDir * _currentShoulderOffset;

            if (lateralDist > 0.001f)
            {
                Vector3 stepDir = lateralDir * Mathf.Sign(_currentShoulderOffset);
                if (Physics.SphereCast(_currentFocusPosition, config.collisionRadius, stepDir, out RaycastHit lateralHit, lateralDist, config.collisionLayers, QueryTriggerInteraction.Ignore))
                {
                    float maxSafeLateral = Mathf.Max(0f, lateralHit.distance - config.collisionPadding);
                    safeShoulderVector = stepDir * Mathf.Min(lateralDist, maxSafeLateral);
                }
            }

            Vector3 cameraPivot = _currentFocusPosition + safeShoulderVector;

            // 5. Line-of-sight collision check using SphereCast against Environment (12) and Ground (13)
            Vector3 backDirection = -(orbitRotation * Vector3.forward);
            float desiredDistance = config.defaultDistance;

            bool hitDetected = false;
            float targetDist = desiredDistance;

            if (Physics.SphereCast(cameraPivot, config.collisionRadius, backDirection, out RaycastHit sphereHit, desiredDistance, config.collisionLayers, QueryTriggerInteraction.Ignore))
            {
                float safeDist = sphereHit.distance - config.collisionPadding;
                if (safeDist < desiredDistance)
                {
                    hitDetected = true;
                    _lastCollisionHit = sphereHit;
                    targetDist = Mathf.Clamp(safeDist, 0.2f, desiredDistance);
                }
            }
            else if (Physics.Raycast(cameraPivot, backDirection, out RaycastHit rayHit, desiredDistance, config.collisionLayers, QueryTriggerInteraction.Ignore))
            {
                float safeDist = rayHit.distance - (config.collisionRadius + config.collisionPadding);
                if (safeDist < desiredDistance)
                {
                    hitDetected = true;
                    _lastCollisionHit = rayHit;
                    targetDist = Mathf.Clamp(safeDist, 0.2f, desiredDistance);
                }
            }

            _isObstructed = hitDetected;
            _targetDistance = targetDist;

            // 6. Distance interpolation: rapid retraction on obstruction, smooth recovery when clear
            if (_targetDistance < _currentDistance)
            {
                // Reset recovery velocity on retraction to avoid retaining negative/contaminated momentum
                _distanceVelocity = 0f;
                // Rapidly retract to prevent any visible wall clipping
                _currentDistance = Mathf.MoveTowards(_currentDistance, _targetDistance, 25f * deltaTime);
            }
            else
            {
                // Smoothly ease back out when obstruction clears
                _currentDistance = Mathf.SmoothDamp(_currentDistance, _targetDistance, ref _distanceVelocity, 0.25f);
                if (Mathf.Abs(_targetDistance - _currentDistance) < 0.005f)
                {
                    _currentDistance = _targetDistance;
                    _distanceVelocity = 0f;
                }
            }

            // 7. Place camera and align rotation
            Vector3 finalPosition = cameraPivot + backDirection * _currentDistance;

            transform.position = finalPosition;
            transform.rotation = orbitRotation;

            // 8. Dynamic FOV modulation
            UpdateFieldOfView(deltaTime);
        }

        private void UpdateFieldOfView(float deltaTime)
        {
            if (_cameraComponent == null || config == null)
            {
                return;
            }

            bool isSprinting = SimulatedSprinting || (playerController != null && playerController.CurrentState == PlayerState.Sprinting);
            float targetFOV = isSprinting ? config.sprintFOV : config.defaultFOV;
            float blendRate = Mathf.Max(0.1f, config.fovBlendSpeed);

            _cameraComponent.fieldOfView = Mathf.Lerp(_cameraComponent.fieldOfView, targetFOV, 1f - Mathf.Exp(-blendRate * deltaTime));
        }

        /// <summary>
        /// Explicitly changes the target to follow and re-binds player steering.
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            playerController = target != null ? target.GetComponent<PlayerController>() : null;
            BindToPlayer();
            SnapToTarget();
        }

        /// <summary>
        /// Directly sets target yaw and pitch for deterministic automated testing.
        /// </summary>
        public void SetTargetYawAndPitch(float yaw, float pitch)
        {
            _targetYaw = yaw;
            if (config != null)
            {
                _targetPitch = Mathf.Clamp(pitch, config.minPitch, config.maxPitch);
            }
            else
            {
                _targetPitch = pitch;
            }
        }

        /// <summary>
        /// Instantly snaps the camera focus position and distance to target without damping.
        /// </summary>
        public void SnapToTarget()
        {
            if (target != null && config != null)
            {
                _currentFocusPosition = target.position + config.focusOffset;
                _targetDistance = config.defaultDistance;
                _currentDistance = _targetDistance;
                _distanceVelocity = 0f;
                _currentShoulderOffset = _targetShoulderOffset;
                Quaternion orbitRotation = Quaternion.Euler(_currentPitch, _currentYaw, 0f);
                Vector3 backDirection = -(orbitRotation * Vector3.forward);
                Vector3 shoulderVector = orbitRotation * (Vector3.right * _currentShoulderOffset);
                Vector3 cameraPivot = _currentFocusPosition + shoulderVector;
                transform.position = cameraPivot + backDirection * _currentDistance;
                transform.rotation = orbitRotation;
            }
        }

        private static float NormalizePitch(float angle)
        {
            while (angle > 180f) angle -= 360f;
            while (angle < -180f) angle += 360f;
            return angle;
        }
    }
}
