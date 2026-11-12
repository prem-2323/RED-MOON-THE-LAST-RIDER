using System;
using UnityEngine;
using RedMoon.Player;
using RedMoon.Camera;

namespace RedMoon.Horse
{
    /// <summary>
    /// Coordinates player mounting, rider attachment, dismounting, and camera focus redirection.
    /// Ensures player and horse controllers never conflict or duplicate.
    /// </summary>
    public sealed class HorseMountController : MonoBehaviour
    {
        [SerializeField] private HorseConfig config;
        [SerializeField] private HorseController horseController;
        [SerializeField] private HorseInputReader horseInputReader;
        [SerializeField] private Transform riderSocket;
        [SerializeField] private Transform riderFocusPoint;
        [SerializeField] private Transform dismountPointLeft;
        [SerializeField] private Transform dismountPointRight;

        private PlayerController _currentRider;
        private bool _isMounted;
        private bool _isTransitioning;
        private CameraController _cameraController;

        public bool IsMounted => _isMounted;
        public bool IsTransitioning => _isTransitioning;
        public PlayerController CurrentRider => _currentRider;
        public Transform RiderSocket => riderSocket;
        public Transform RiderFocusPoint => riderFocusPoint;

        public event Action OnMounted;
        public event Action OnDismounted;

        private void Awake()
        {
            if (horseController == null)
            {
                horseController = GetComponent<HorseController>();
            }

            if (horseInputReader == null)
            {
                horseInputReader = GetComponent<HorseInputReader>();
            }

            EnsureSockets();
        }

        private void EnsureSockets()
        {
            if (riderSocket == null)
            {
                var socketObj = transform.Find("RiderSocket");
                if (socketObj == null)
                {
                    socketObj = new GameObject("RiderSocket").transform;
                    socketObj.SetParent(transform, false);
                    socketObj.localPosition = config != null ? config.riderPositionOffset : new Vector3(0f, 1.25f, -0.1f);
                }
                riderSocket = socketObj;
            }

            if (riderFocusPoint == null)
            {
                var focusObj = transform.Find("RiderFocusPoint");
                if (focusObj == null)
                {
                    focusObj = new GameObject("RiderFocusPoint").transform;
                    focusObj.SetParent(transform, false);
                    focusObj.localPosition = config != null ? config.mountedCameraFocusOffset : new Vector3(0f, 1.9f, 0f);
                }
                riderFocusPoint = focusObj;
            }

            if (dismountPointLeft == null)
            {
                var leftObj = transform.Find("DismountPoint_Left");
                if (leftObj == null)
                {
                    leftObj = new GameObject("DismountPoint_Left").transform;
                    leftObj.SetParent(transform, false);
                    leftObj.localPosition = config != null ? config.dismountOffsetLeft : new Vector3(-1.3f, 0f, -0.1f);
                }
                dismountPointLeft = leftObj;
            }

            if (dismountPointRight == null)
            {
                var rightObj = transform.Find("DismountPoint_Right");
                if (rightObj == null)
                {
                    rightObj = new GameObject("DismountPoint_Right").transform;
                    rightObj.SetParent(transform, false);
                    rightObj.localPosition = config != null ? config.dismountOffsetRight : new Vector3(1.3f, 0f, -0.1f);
                }
                dismountPointRight = rightObj;
            }
        }

        public bool CanMount(PlayerController player)
        {
            if (player == null || _isMounted || _isTransitioning)
            {
                return false;
            }

            if (horseController != null && horseController.HorizontalSpeed > 2.0f)
            {
                return false;
            }

            float maxRange = config != null ? config.mountInteractionRadius : 2.5f;
            float dist = Vector3.Distance(transform.position, player.transform.position);
            return dist <= maxRange;
        }

        public bool Mount(PlayerController player)
        {
            if (!CanMount(player))
            {
                return false;
            }

            _isTransitioning = true;
            _currentRider = player;

            if (horseController != null)
            {
                horseController.StateMachine.ChangeState(HorseState.Mounting);
            }

            // 1. Suspend Player CharacterController and input cleanly
            var playerCharController = player.GetComponent<CharacterController>();
            if (playerCharController != null)
            {
                playerCharController.enabled = false;
            }

            var playerInput = player.GetComponent<PlayerInputReader>();
            if (playerInput != null)
            {
                playerInput.enabled = false;
            }

            player.enabled = false;

            // 2. Attach player to rider socket
            player.transform.SetParent(riderSocket, false);
            player.transform.localPosition = Vector3.zero;
            player.transform.localRotation = Quaternion.identity;

            // 3. Activate horse input reader
            if (horseInputReader != null)
            {
                // Share player input actions if needed
                if (playerInput != null && playerInput.Input != null)
                {
                    horseInputReader.Initialize(playerInput.Input);
                }
                horseInputReader.SetInputActive(true);
            }

            // 4. Switch camera focus smoothly to rider focus point
            ResolveCamera();
            if (_cameraController != null)
            {
                _cameraController.SetTarget(riderFocusPoint);
            }

            // 5. Complete transition
            _isMounted = true;
            _isTransitioning = false;

            if (horseController != null)
            {
                horseController.StateMachine.ChangeState(HorseState.Mounted);
            }

            OnMounted?.Invoke();
            return true;
        }

        public bool Dismount()
        {
            if (!_isMounted || _isTransitioning || _currentRider == null)
            {
                return false;
            }

            _isTransitioning = true;

            if (horseController != null)
            {
                horseController.StateMachine.ChangeState(HorseState.Dismounting);
                horseController.StopMovement();
            }

            // 1. Determine safe dismount position
            Vector3 safePosition = FindSafeDismountPosition();

            // 2. Unparent player and reposition
            _currentRider.transform.SetParent(null, true);
            _currentRider.transform.position = safePosition;
            _currentRider.transform.rotation = Quaternion.LookRotation(transform.forward, Vector3.up);

            // 3. Restore Player Controller and CharacterController
            var playerCharController = _currentRider.GetComponent<CharacterController>();
            if (playerCharController != null)
            {
                playerCharController.enabled = true;
            }

            var playerInput = _currentRider.GetComponent<PlayerInputReader>();
            if (playerInput != null)
            {
                playerInput.enabled = true;
            }

            _currentRider.enabled = true;

            // 4. Deactivate horse input
            if (horseInputReader != null)
            {
                horseInputReader.SetInputActive(false);
            }

            // 5. Restore camera focus back to player
            ResolveCamera();
            if (_cameraController != null)
            {
                _cameraController.SetTarget(_currentRider.transform);
            }

            // 6. Complete dismount
            PlayerController previousRider = _currentRider;
            _currentRider = null;
            _isMounted = false;
            _isTransitioning = false;

            if (horseController != null)
            {
                horseController.StateMachine.ChangeState(HorseState.Idle);
            }

            OnDismounted?.Invoke();
            return true;
        }

        public Vector3 FindSafeDismountPosition()
        {
            Vector3 leftWorld = dismountPointLeft != null ? dismountPointLeft.position : transform.TransformPoint(config != null ? config.dismountOffsetLeft : new Vector3(-1.3f, 0f, 0f));
            Vector3 rightWorld = dismountPointRight != null ? dismountPointRight.position : transform.TransformPoint(config != null ? config.dismountOffsetRight : new Vector3(1.3f, 0f, 0f));

            if (IsDismountPositionSafe(leftWorld, out Vector3 safeLeft))
            {
                return safeLeft;
            }

            if (IsDismountPositionSafe(rightWorld, out Vector3 safeRight))
            {
                return safeRight;
            }

            // Fallback safe position behind horse
            Vector3 backPos = transform.position - transform.forward * 2.0f;
            if (IsDismountPositionSafe(backPos, out Vector3 safeBack))
            {
                return safeBack;
            }

            return leftWorld;
        }

        private bool IsDismountPositionSafe(Vector3 candidatePos, out Vector3 groundAdjustedPos)
        {
            groundAdjustedPos = candidatePos;
            int envLayerMask = (1 << 12); // Environment
            int groundLayerMask = (1 << 0) | (1 << 13); // Default and Ground

            // Check if obstructed by wall / environment collider
            if (Physics.CheckSphere(candidatePos + Vector3.up * 1.0f, 0.35f, envLayerMask, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            // Check downward for solid ground surface
            if (Physics.Raycast(candidatePos + Vector3.up * 1.5f, Vector3.down, out RaycastHit hit, 3.5f, groundLayerMask, QueryTriggerInteraction.Ignore))
            {
                groundAdjustedPos = hit.point;
                return true;
            }

            return false;
        }

        private void ResolveCamera()
        {
            if (_cameraController == null)
            {
                _cameraController = FindAnyObjectByType<CameraController>();
            }
        }
    }
}

