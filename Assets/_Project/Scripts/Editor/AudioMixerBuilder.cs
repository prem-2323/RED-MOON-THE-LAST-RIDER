using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace RedMoon.Editor
{
    /// <summary>
    /// Creates the RED MOON audio mixer and its required routing groups.
    /// No audio clips are imported. The mixer is intentionally a foundation:
    /// Master -> Music, Ambience, SFX, Voice and UI.
    /// </summary>
    public static class AudioMixerBuilder
    {
        private const string AudioFolder = "Assets/_Project/Settings/Audio";
        private const string MixerPath = AudioFolder + "/REDMOON_AudioMixer.mixer";

        private static readonly string[] RequiredGroups =
        {
            "Music",
            "Ambience",
            "SFX",
            "Voice",
            "UI"
        };

        [MenuItem("RED MOON/Audio/Create or Validate Audio Mixer")]
        public static void CreateOrValidate()
        {
            EnsureFolder(AudioFolder);

            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            object controller = mixer != null ? FindController(MixerPath) : null;

            if (mixer == null)
            {
                controller = CreateMixer();
                if (controller == null)
                    return;

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            }

            if (mixer == null || controller == null)
            {
                Debug.LogError("[RED MOON] Could not load or create the audio mixer at " + MixerPath);
                return;
            }

            EnsureGroups(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            if (mixer == null)
            {
                Debug.LogError("[RED MOON] Audio mixer disappeared after creation.");
                return;
            }

            ValidateGroups(mixer);
        }

        private static object CreateMixer()
        {
            var controllerType = FindType("UnityEditor.Audio.AudioMixerController");
            if (controllerType == null)
            {
                Debug.LogError("[RED MOON] UnityEditor.Audio.AudioMixerController is unavailable.");
                return null;
            }

            var create = controllerType.GetMethod(
                "CreateMixerControllerAtPath",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (create == null)
            {
                Debug.LogError("[RED MOON] AudioMixerController.CreateMixerControllerAtPath is unavailable.");
                return null;
            }

            try
            {
                var result = create.Invoke(null, new object[] { MixerPath });
                if (result == null)
                    Debug.LogError("[RED MOON] Unity returned no audio mixer controller.");

                return result;
            }
            catch (Exception exception)
            {
                Debug.LogError("[RED MOON] Audio mixer creation failed: " + exception);
                return null;
            }
        }

        private static void EnsureGroups(object controller)
        {
            var controllerType = controller.GetType();
            var masterProperty = controllerType.GetProperty(
                "masterGroup",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var masterGroup = masterProperty != null ? masterProperty.GetValue(controller) : null;
            if (masterGroup == null)
            {
                Debug.LogError("[RED MOON] Audio mixer has no Master group.");
                return;
            }

            var groupType = masterGroup.GetType();
            var createGroup = controllerType.GetMethod(
                "CreateNewGroup",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var addChild = controllerType.GetMethod(
                "AddChildToParent",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var childrenProperty = groupType.GetProperty(
                "children",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var nameProperty = groupType.GetProperty(
                "name",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (createGroup == null || addChild == null || childrenProperty == null || nameProperty == null)
            {
                Debug.LogError("[RED MOON] Audio mixer group API is incomplete.");
                return;
            }

            foreach (var requestedName in RequiredGroups)
            {
                var group = FindGroup(controller, nameProperty, requestedName);
                if (group == null)
                {
                    group = createGroup.Invoke(controller, new object[] { requestedName, false });
                    Debug.Log("[RED MOON] Created audio mixer group: " + requestedName);
                }

                if (group == null)
                {
                    Debug.LogError("[RED MOON] Could not create audio mixer group: " + requestedName);
                    continue;
                }

                var children = childrenProperty.GetValue(masterGroup) as Array;
                var isChild = children != null && children.Cast<object>().Any(child => ReferenceEquals(child, group));
                if (!isChild)
                {
                    addChild.Invoke(controller, new object[] { group, masterGroup });
                    Debug.Log("[RED MOON] Routed audio mixer group under Master: " + requestedName);
                }
            }
        }

        private static object FindGroup(object controller, PropertyInfo nameProperty, string requestedName)
        {
            var getAll = controller.GetType().GetMethod(
                "GetAllAudioGroupsSlow",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (getAll == null)
                return null;

            var groups = getAll.Invoke(controller, null) as IEnumerable;
            if (groups == null)
                return null;

            foreach (var group in groups)
            {
                if (group == null)
                    continue;

                var name = nameProperty.GetValue(group) as string;
                if (string.Equals(name, requestedName, StringComparison.Ordinal))
                    return group;
            }

            return null;
        }

        private static object FindController(string path)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            if (assets == null)
                return null;

            return assets.FirstOrDefault(asset =>
                asset != null &&
                asset.GetType().FullName == "UnityEditor.Audio.AudioMixerController");
        }

        private static void ValidateGroups(AudioMixer mixer)
        {
            var missing = RequiredGroups
                .Where(group => mixer.FindMatchingGroups(group).Length == 0)
                .ToList();

            var master = mixer.FindMatchingGroups("Master");
            if (master.Length == 0)
                missing.Add("Master");

            if (missing.Count > 0)
            {
                Debug.LogError("[RED MOON] Audio mixer is missing groups: " + string.Join(", ", missing));
                return;
            }

            Debug.Log("[RED MOON] Audio mixer validated: Master, Music, Ambience, SFX, Voice, UI.");
        }

        private static Type FindType(string fullName)
        {
            foreach (var type in TypeCache.GetTypesDerivedFrom<AudioMixer>())
            {
                if (type.FullName == fullName)
                    return type;
            }

            return null;
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
                return;

            var parent = System.IO.Path.GetDirectoryName(folder)?.Replace('\\', '/');
            var name = System.IO.Path.GetFileName(folder);
            if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(name))
            {
                EnsureFolder(parent);
                AssetDatabase.CreateFolder(parent, name);
            }
        }
    }
}
