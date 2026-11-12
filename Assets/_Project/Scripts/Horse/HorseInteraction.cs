using UnityEngine;
using UnityEngine.InputSystem;
using RedMoon.Player;

namespace RedMoon.Horse
{
    /// <summary>
    /// Proximity-based mount interaction detector for the RED MOON horse.
    /// Detects when the player enters mounting range and routes interaction inputs.
    /// </summary>
    public sealed class HorseInteraction : MonoBehaviour
    {
        [SerializeField] private HorseConfig config;
        [SerializeField] private HorseMountController mountController;

        private PlayerController _nearbyPlayer;
        private bool _playerInRange;

        public bool PlayerInRange => _playerInRange;
        public PlayerController NearbyPlayer => _nearbyPlayer;

        private void Awake()
        {
            if (mountController == null)
            {
                mountController = GetComponent<HorseMountController>();
            }
        }

        private void Update()
        {
            if (mountController == null)
            {
                return;
            }

            // If already mounted, Dismount is handled via HorseController / HorseInputReader
            if (mountController.IsMounted)
            {
                _playerInRange = false;
                _nearbyPlayer = null;
                return;
            }

            CheckProximity();
            CheckMountInput();
        }

        private void CheckProximity()
        {
            float radius = config != null ? config.mountInteractionRadius : 2.5f;
            int playerMask = 1 << 8; // Layer 8: Player

            Collider[] hits = Physics.OverlapSphere(transform.position, radius, playerMask, QueryTriggerInteraction.Ignore);
            if (hits != null && hits.Length > 0)
            {
                for (int i = 0; i < hits.Length; i++)
                {
                    var pc = hits[i].GetComponentInParent<PlayerController>();
                    if (pc != null && pc.enabled)
                    {
                        _nearbyPlayer = pc;
                        _playerInRange = true;
                        return;
                    }
                }
            }

            _playerInRange = false;
            _nearbyPlayer = null;
        }

        private void CheckMountInput()
        {
            if (!_playerInRange || _nearbyPlayer == null)
            {
                return;
            }

            bool mountPressed = false;

            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                mountPressed = true;
            }
            else if (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame)
            {
                mountPressed = true;
            }

            if (mountPressed)
            {
                mountController.Mount(_nearbyPlayer);
            }
        }

        private void OnDrawGizmosSelected()
        {
            float radius = config != null ? config.mountInteractionRadius : 2.5f;
            Gizmos.color = _playerInRange ? Color.yellow : Color.cyan;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}

