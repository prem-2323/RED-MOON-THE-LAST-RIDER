using UnityEngine;
using UnityEngine.InputSystem;
using RedMoon.Core.Input;

namespace RedMoon.Player
{
    /// <summary>
    /// Reads RED MOON input through RedMoonInput and converts it into a
    /// PlayerInputFrame. Keeps device access out of gameplay code.
    /// </summary>
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private bool enableOnStart = true;

        private RedMoonInput _input;
        private bool _disposed;

        public RedMoonInput Input => _input;

        public PlayerInputFrame ReadFrame()
        {
            if (_input == null)
            {
                return PlayerInputFrame.Empty;
            }

            return new PlayerInputFrame(
                _input.ReadMove(),
                _input.ReadLook(),
                _input.Sprint.IsPressed(),
                _input.Jump.WasPressedThisFrame(),
                _input.Jump.IsPressed());
        }

        private void Awake()
        {
            if (inputActions == null)
            {
                Debug.LogError("[RED MOON] PlayerInputReader is missing REDMOON_InputActions.", this);
                enabled = false;
                return;
            }

            _input = new RedMoonInput(inputActions);

            if (enableOnStart)
            {
                _input.EnableGameplay();
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _input?.Dispose();
            _disposed = true;
        }

        private void OnDestroy()
        {
            Dispose();
        }
    }
}
