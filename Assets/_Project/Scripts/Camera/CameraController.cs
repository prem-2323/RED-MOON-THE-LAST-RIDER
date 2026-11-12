using System;
using UnityEngine;
using UnityEngine.InputSystem;
using RedMoon.Core.Input;
using RedMoon.Player;

namespace RedMoon.Camera
{
    /// <summary>
    /// Custom third-person orbital camera motor for RED MOON: THE LAST RIDER.
    ///
    /// Executes in LateUpdate() to smoothly orbit and follow the player focus point
    /// in world space. Operates independently from character rotation to prevent
    /// orientation feedback loops, and injects its Transform into PlayerController
    /// for camera-relative steering.
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

        private RedMoonInput _input;
        private bool _ownsInput;
        private float _targetYaw;
        private float _targetPitch;
        private float _currentYaw;
        private float _currentPitch;
        private float _yawVelocity;
        private float _pitchVelocity;
        private Vector3 _currentFocusPosition;
        private Vector3 _positionVelocity;
        private UnityEngine.Camera _cameraComponent;

        public CameraConfig Config => config;
        public Transform Target => target;
        public PlayerController PlayerController => playerController;
        public float CurrentYaw => _currentYaw;
        public float CurrentPitch => _currentPitch;
        public float TargetYaw => _targetYaw;
        public float TargetPitch => _targetPitch;
        public Vector3 CurrentFocusPosition => _currentFocusPosition;

        /// <summary>
        /// When true, automated tests can drive ApplyLook() directly without polling hardware.
        /// </summary>
        public bool ExternalInput { get; set; }

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
        }

        private void OnDestroy()
        {
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
                    _input = reader.Input;
                    _ownsInput = false;
                    return;
                }
            }

            if (inputActions != null)
            {
                _input = new RedMoonInput(inputActions);
                _input.EnableGameplay();
                _ownsInput = true;
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

        private void LateUpdate()
        {
            if (target == null || config == null)
            {
                return;
            }

            float deltaTime = Time.deltaTime;

            if (!ExternalInput)
            {
                ProcessLookInput(deltaTime);
            }

            UpdateOrbit(deltaTime);
        }

        private void ProcessLookInput(float deltaTime)
        {
            if (_input == null)
            {
                return;
            }

            Vector2 rawLook = _input.CameraLook.ReadValue<Vector2>();
            if (rawLook.sqrMagnitude < 0.00001f)
            {
                return;
            }

            bool isMouse = true;
            var control = _input.CameraLook.activeControl;
            if (control != null)
            {
                isMouse = control.device is Mouse;
            }

            ApplyLook(rawLook, isMouse, deltaTime);
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
                // Mouse delta is a per-frame pixel displacement; scale by sensitivity * 0.01
                yawDelta = rawLook.x * (config.yawSensitivity * 0.01f);
                pitchDelta = (config.invertY ? rawLook.y : -rawLook.y) * (config.pitchSensitivity * 0.01f);
            }
            else
            {
                // Gamepad stick is continuous rate [-1, 1]; multiply by deltaTime
                yawDelta = rawLook.x * config.yawSensitivity * deltaTime;
                pitchDelta = (config.invertY ? rawLook.y : -rawLook.y) * config.pitchSensitivity * deltaTime;
            }

            _targetYaw += yawDelta;
            _targetPitch = Mathf.Clamp(_targetPitch + pitchDelta, config.minPitch, config.maxPitch);
        }

        private void UpdateOrbit(float deltaTime)
        {
            float smoothing = Mathf.Max(0.001f, config.rotationSmoothing);
            _currentYaw = Mathf.SmoothDampAngle(_currentYaw, _targetYaw, ref _yawVelocity, smoothing);
            _currentPitch = Mathf.SmoothDampAngle(_currentPitch, _targetPitch, ref _pitchVelocity, smoothing);

            Quaternion orbitRotation = Quaternion.Euler(_currentPitch, _currentYaw, 0f);

            Vector3 targetFocus = target.position + config.focusOffset;
            float posSmoothing = Mathf.Max(0.001f, config.positionSmoothing);
            _currentFocusPosition = Vector3.SmoothDamp(_currentFocusPosition, targetFocus, ref _positionVelocity, posSmoothing);

            Vector3 desiredPosition = _currentFocusPosition - (orbitRotation * Vector3.forward * config.defaultDistance);

            transform.position = desiredPosition;
            transform.rotation = orbitRotation;
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
        /// Instantly snaps the camera focus position to the current target without damping.
        /// </summary>
        public void SnapToTarget()
        {
            if (target != null && config != null)
            {
                _currentFocusPosition = target.position + config.focusOffset;
                Quaternion orbitRotation = Quaternion.Euler(_currentPitch, _currentYaw, 0f);
                transform.position = _currentFocusPosition - (orbitRotation * Vector3.forward * config.defaultDistance);
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

