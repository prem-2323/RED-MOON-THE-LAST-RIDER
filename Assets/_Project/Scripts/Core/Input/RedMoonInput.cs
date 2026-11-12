using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RedMoon.Core.Input
{
    /// <summary>
    /// Central input access point for RED MOON: THE LAST RIDER.
    ///
    /// This class exposes the project's Input Actions by name and owns the
    /// enabling/disabling of action maps. It deliberately contains no gameplay
    /// logic: movement, combat, horse control and UI behaviour belong to their
    /// respective gameplay systems.
    ///
    /// Gameplay code should depend on this class instead of reading devices
    /// directly. This keeps bindings, rebinding and control schemes in one place.
    /// </summary>
    public sealed class RedMoonInput : IDisposable
    {
        public const string PlayerMapName = "Player";
        public const string HorseMapName = "Horse";
        public const string CombatMapName = "Combat";
        public const string CameraMapName = "Camera";
        public const string UIMapName = "UI";

        private readonly InputActionAsset _asset;
        private readonly InputActionMap _player;
        private readonly InputActionMap _horse;
        private readonly InputActionMap _combat;
        private readonly InputActionMap _camera;
        private readonly InputActionMap _ui;

        public RedMoonInput(InputActionAsset asset)
        {
            _asset = asset != null ? asset : throw new ArgumentNullException(nameof(asset));
            _player = RequireMap(PlayerMapName);
            _horse = RequireMap(HorseMapName);
            _combat = RequireMap(CombatMapName);
            _camera = RequireMap(CameraMapName);
            _ui = RequireMap(UIMapName);
        }

        public InputActionAsset Asset => _asset;

        public InputActionMap PlayerMap => _player;
        public InputActionMap HorseMap => _horse;
        public InputActionMap CombatMap => _combat;
        public InputActionMap CameraMap => _camera;
        public InputActionMap UIMap => _ui;

        // Player
        public InputAction Move => Get(_player, "Move");
        public InputAction Look => Get(_player, "Look");
        public InputAction Jump => Get(_player, "Jump");
        public InputAction Sprint => Get(_player, "Sprint");
        public InputAction MountHorse => Get(_player, "Mount");
        public InputAction Interact => Get(_player, "Interact");
        public InputAction Dodge => Get(_player, "Dodge");
        public InputAction Attack => Get(_player, "Attack");
        public InputAction Block => Get(_player, "Block");
        public InputAction Magic => Get(_player, "Magic");
        public InputAction ToggleInventory => Get(_player, "ToggleInventory");
        public InputAction ToggleMap => Get(_player, "ToggleMap");
        public InputAction Whistle => Get(_player, "Whistle");

        // Horse
        public InputAction HorseMove => Get(_horse, "Move");
        public InputAction HorseLook => Get(_horse, "Look");
        public InputAction HorseJump => Get(_horse, "Jump");
        public InputAction HorseSprint => Get(_horse, "Sprint");
        public InputAction Dismount => Get(_horse, "Dismount");

        // Combat
        public InputAction CombatAttack => Get(_combat, "Attack");
        public InputAction CombatBlock => Get(_combat, "Block");
        public InputAction CombatDodge => Get(_combat, "Dodge");
        public InputAction CombatMagic => Get(_combat, "Magic");

        // Camera
        public InputAction CameraLook => Get(_camera, "Look");
        public InputAction CameraZoom => Get(_camera, "Zoom");
        public InputAction SwitchShoulder => Get(_camera, "SwitchShoulder");

        // UI
        public InputAction Navigate => Get(_ui, "Navigate");
        public InputAction Submit => Get(_ui, "Submit");
        public InputAction Cancel => Get(_ui, "Cancel");
        public InputAction UIPause => Get(_ui, "Pause");

        /// <summary>Reads raw movement input. Gameplay decides how to apply it.</summary>
        public Vector2 ReadMove() => Move.ReadValue<Vector2>();

        /// <summary>Reads raw mouse/stick look input. Camera code applies sensitivity.</summary>
        public Vector2 ReadLook() => Look.ReadValue<Vector2>();

        /// <summary>Reads raw horse movement input.</summary>
        public Vector2 ReadHorseMove() => HorseMove.ReadValue<Vector2>();

        public void EnableGameplay()
        {
            _player.Enable();
            _horse.Enable();
            _combat.Enable();
            _camera.Enable();
        }

        public void DisableGameplay()
        {
            _player.Disable();
            _horse.Disable();
            _combat.Disable();
            _camera.Disable();
        }

        public void EnableUI() => _ui.Enable();

        public void DisableUI() => _ui.Disable();

        public void Dispose()
        {
            DisableGameplay();
            DisableUI();
        }

        private InputActionMap RequireMap(string name)
        {
            return _asset.FindActionMap(name, throwIfNotFound: true);
        }

        private static InputAction Get(InputActionMap map, string name)
        {
            return map.FindAction(name, throwIfNotFound: true);
        }
    }
}
