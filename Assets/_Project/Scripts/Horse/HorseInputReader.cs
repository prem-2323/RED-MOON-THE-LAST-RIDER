using System;
using UnityEngine;
using UnityEngine.InputSystem;
using RedMoon.Core.Input;

namespace RedMoon.Horse
{
    /// <summary>
    /// Reads horse inputs through RedMoonInput with robust direct hardware polling fallback.
    /// Manages action enable/disable states to ensure input is active when mounted.
    /// </summary>
    public sealed class HorseInputReader : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private bool autoEnableOnMount = true;

        private RedMoonInput _input;
        private bool _ownsInput;
        private bool _isActive;

        public RedMoonInput Input => _input;
        public bool IsActive => _isActive;

        public void Initialize(RedMoonInput sharedInput)
        {
            if (sharedInput != null)
            {
                _input = sharedInput;
                _ownsInput = false;
            }
            else if (inputActions != null && _input == null)
            {
                _input = new RedMoonInput(inputActions);
                _ownsInput = true;
            }
        }

        public void SetInputActive(bool active)
        {
            _isActive = active;
            if (_input == null)
            {
                return;
            }

            if (_isActive)
            {
                _input.HorseMap.Enable();
            }
            else
            {
                _input.HorseMap.Disable();
            }
        }

        public HorseInputFrame ReadFrame()
        {
            if (!_isActive)
            {
                return HorseInputFrame.Empty;
            }

            Vector2 move = Vector2.zero;
            Vector2 look = Vector2.zero;
            bool sprint = false;
            bool jump = false;
            bool dismount = false;

            // 1. Try reading through canonical RedMoonInput
            if (_input != null)
            {
                try
                {
                    move = _input.ReadHorseMove();
                    look = _input.HorseLook.ReadValue<Vector2>();
                    sprint = _input.HorseSprint.IsPressed();
                    jump = _input.HorseJump.WasPressedThisFrame();
                    dismount = _input.Dismount.WasPressedThisFrame();
                }
                catch
                {
                    // Action may not be enabled or bound
                }
            }

            // 2. Hardware fallback: Direct Keyboard polling (ensures responsive WASD in Editor & Standalone)
            if (Keyboard.current != null)
            {
                Vector2 keyMove = Vector2.zero;
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) keyMove.y += 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) keyMove.y -= 1f;
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) keyMove.x -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) keyMove.x += 1f;

                if (keyMove.sqrMagnitude > 0.001f)
                {
                    move = keyMove.normalized;
                }

                if (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed)
                {
                    sprint = true;
                }

                if (Keyboard.current.spaceKey.wasPressedThisFrame)
                {
                    jump = true;
                }

                if (Keyboard.current.eKey.wasPressedThisFrame)
                {
                    dismount = true;
                }
            }

            // 3. Hardware fallback: Gamepad polling
            if (Gamepad.current != null)
            {
                Vector2 stickMove = Gamepad.current.leftStick.ReadValue();
                if (stickMove.sqrMagnitude > 0.01f)
                {
                    move = stickMove;
                }

                if (Gamepad.current.leftStickButton.isPressed)
                {
                    sprint = true;
                }

                if (Gamepad.current.buttonSouth.wasPressedThisFrame)
                {
                    jump = true;
                }

                if (Gamepad.current.buttonEast.wasPressedThisFrame)
                {
                    dismount = true;
                }
            }

            return new HorseInputFrame(move, look, sprint, jump, dismount);
        }

        private void Awake()
        {
            if (inputActions != null && _input == null)
            {
                _input = new RedMoonInput(inputActions);
                _ownsInput = true;
            }
            // By default horse input is inactive until player mounts
            _isActive = false;
        }

        private void OnDestroy()
        {
            if (_ownsInput)
            {
                _input?.Dispose();
            }
        }
    }
}
