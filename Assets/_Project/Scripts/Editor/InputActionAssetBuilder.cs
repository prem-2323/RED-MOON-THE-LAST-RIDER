using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RedMoon.Editor
{
    /// <summary>
    /// Creates the canonical RED MOON Input Actions asset if it does not exist.
    /// This is a non-destructive builder: an existing asset is never overwritten.
    /// Use the Input System editor to change bindings after the initial creation.
    /// </summary>
    public static class InputActionAssetBuilder
    {
        private const string AssetPath = "Assets/_Project/Settings/Input/REDMOON_InputActions.inputactions";
        private const string KeyboardMouse = "KeyboardMouse";
        private const string Gamepad = "Gamepad";

        [MenuItem("RED MOON/Input/Create Input Actions Asset (if missing)")]
        public static void CreateIfMissing()
        {
            var existing = AssetDatabase.LoadAssetAtPath<InputActionAsset>(AssetPath);
            if (existing != null && existing.actionMaps.Count > 0)
            {
                Selection.activeObject = existing;
                Debug.Log("[RED MOON] Input actions already exist: " + AssetPath);
                return;
            }

            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            asset.name = "REDMOON_InputActions";

            asset.AddControlScheme(KeyboardMouse)
                .WithRequiredDevice("<Keyboard>")
                .WithRequiredDevice("<Mouse>");
            asset.AddControlScheme(Gamepad)
                .WithRequiredDevice("<Gamepad>");

            BuildPlayer(asset);
            BuildHorse(asset);
            BuildCombat(asset);
            BuildCamera(asset);
            BuildUI(asset);

            // .inputactions files must be JSON text assets, not Unity-serialized
            // ScriptableObjects. Creating one with AssetDatabase.CreateAsset
            // produces an unreadable file, so write JSON and import it instead.
            string json = asset.ToJson();
            File.WriteAllText(AssetPath, json, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(AssetPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var imported = AssetDatabase.LoadAssetAtPath<InputActionAsset>(AssetPath);
            if (imported == null)
            {
                Debug.LogError("[RED MOON] Input actions import failed: " + AssetPath);
                return;
            }

            Selection.activeObject = imported;
            Debug.Log("[RED MOON] Created input actions: " + AssetPath);
        }

        private static void BuildPlayer(InputActionAsset asset)
        {
            var map = asset.AddActionMap("Player");
            AddMovement(map);
            AddLook(map);
            AddButton(map, "Jump", "<Keyboard>/space", "<Gamepad>/buttonSouth");
            AddButton(map, "Sprint", "<Keyboard>/leftShift", "<Gamepad>/leftStickPress");
            AddButton(map, "Mount", "<Keyboard>/e", "<Gamepad>/buttonWest");
            AddButton(map, "Interact", "<Keyboard>/f", "<Gamepad>/buttonNorth");
            AddButton(map, "Dodge", "<Keyboard>/leftCtrl", "<Gamepad>/buttonEast");
            AddButton(map, "Attack", "<Mouse>/leftButton", "<Gamepad>/rightTrigger");
            AddButton(map, "Block", "<Mouse>/rightButton", "<Gamepad>/leftTrigger");
            AddButton(map, "Magic", "<Keyboard>/q", "<Gamepad>/rightShoulder");
            AddButton(map, "ToggleInventory", "<Keyboard>/tab", "<Gamepad>/select");
            AddButton(map, "ToggleMap", "<Keyboard>/m", "<Gamepad>/start");
            AddButton(map, "Whistle", "<Keyboard>/h", "<Gamepad>/leftShoulder");
        }

        private static void BuildHorse(InputActionAsset asset)
        {
            var map = asset.AddActionMap("Horse");
            AddMovement(map);
            AddLook(map);
            AddButton(map, "Jump", "<Keyboard>/space", "<Gamepad>/buttonSouth");
            AddButton(map, "Sprint", "<Keyboard>/leftShift", "<Gamepad>/leftStickPress");
            AddButton(map, "Dismount", "<Keyboard>/e", "<Gamepad>/buttonWest");
        }

        private static void BuildCombat(InputActionAsset asset)
        {
            var map = asset.AddActionMap("Combat");
            AddButton(map, "Attack", "<Mouse>/leftButton", "<Gamepad>/rightTrigger");
            AddButton(map, "Block", "<Mouse>/rightButton", "<Gamepad>/leftTrigger");
            AddButton(map, "Dodge", "<Keyboard>/leftCtrl", "<Gamepad>/buttonEast");
            AddButton(map, "Magic", "<Keyboard>/q", "<Gamepad>/rightShoulder");
        }

        private static void BuildCamera(InputActionAsset asset)
        {
            var map = asset.AddActionMap("Camera");
            AddLook(map);

            var zoom = map.AddAction("Zoom", InputActionType.Value, expectedControlLayout: "Axis");
            zoom.AddBinding("<Mouse>/scroll/y", groups: KeyboardMouse);
            zoom.AddBinding("<Gamepad>/dpad/y", groups: Gamepad);

            AddButton(map, "SwitchShoulder", "<Keyboard>/c", "<Gamepad>/rightStickPress");
        }

        private static void BuildUI(InputActionAsset asset)
        {
            var map = asset.AddActionMap("UI");

            var navigate = map.AddAction("Navigate", InputActionType.Value, expectedControlLayout: "Vector2");
            navigate.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w", KeyboardMouse)
                .With("Down", "<Keyboard>/s", KeyboardMouse)
                .With("Left", "<Keyboard>/a", KeyboardMouse)
                .With("Right", "<Keyboard>/d", KeyboardMouse);
            navigate.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow", KeyboardMouse)
                .With("Down", "<Keyboard>/downArrow", KeyboardMouse)
                .With("Left", "<Keyboard>/leftArrow", KeyboardMouse)
                .With("Right", "<Keyboard>/rightArrow", KeyboardMouse);
            navigate.AddBinding("<Gamepad>/leftStick", groups: Gamepad);
            navigate.AddBinding("<Gamepad>/dpad", groups: Gamepad);

            AddButton(map, "Submit", "<Keyboard>/enter", "<Gamepad>/buttonSouth");
            AddButton(map, "Cancel", "<Keyboard>/escape", "<Gamepad>/buttonEast");
            AddButton(map, "ToggleInventory", "<Keyboard>/tab", "<Gamepad>/select");
            AddButton(map, "ToggleMap", "<Keyboard>/m", "<Gamepad>/buttonNorth");
            AddButton(map, "Pause", "<Keyboard>/p", "<Gamepad>/start");
        }

        private static void AddMovement(InputActionMap map)
        {
            var move = map.AddAction("Move", InputActionType.Value, expectedControlLayout: "Vector2");
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w", KeyboardMouse)
                .With("Down", "<Keyboard>/s", KeyboardMouse)
                .With("Left", "<Keyboard>/a", KeyboardMouse)
                .With("Right", "<Keyboard>/d", KeyboardMouse);
            move.AddBinding("<Gamepad>/leftStick", groups: Gamepad);
        }

        private static void AddLook(InputActionMap map)
        {
            var look = map.AddAction("Look", InputActionType.Value, expectedControlLayout: "Vector2");
            look.AddBinding("<Mouse>/delta", groups: KeyboardMouse);
            look.AddBinding("<Gamepad>/rightStick", groups: Gamepad);
        }

        private static void AddButton(InputActionMap map, string name, string keyboardPath, string gamepadPath)
        {
            var action = map.AddAction(name, InputActionType.Button);
            if (!string.IsNullOrEmpty(keyboardPath))
                action.AddBinding(keyboardPath, groups: KeyboardMouse);
            if (!string.IsNullOrEmpty(gamepadPath))
                action.AddBinding(gamepadPath, groups: Gamepad);
        }
    }
}
