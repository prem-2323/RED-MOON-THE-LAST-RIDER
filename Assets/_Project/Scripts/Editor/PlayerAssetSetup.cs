using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RedMoon.Editor
{
    /// <summary>
    /// Configures the imported interim player model, animation library and
    /// textures for the RED MOON player.
    /// </summary>
    public static class PlayerAssetSetup
    {
        private const string NinjaPath = "Assets/_Project/Art/Characters/Player/Models/Player_Interim_Ninja.fbx";
        private const string AnimationLibraryPath = "Assets/_Project/Art/Characters/Player/Animations/UniversalAnimationLibrary_Standard.fbx";
        private const string ArtFolder = "Assets/_Project/Art/Characters/Player";
        private const int MaxTextureSize = 2048;

        [MenuItem("RED MOON/Player/Configure Imported Assets")]
        public static void ConfigureImportedAssets()
        {
            ConfigureNinja();
            ConfigureAnimationLibrary();
            ConfigureTextures();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void ConfigureNinja()
        {
            var importer = AssetImporter.GetAtPath(NinjaPath) as ModelImporter;
            if (importer == null)
            {
                Debug.LogError("[RED MOON] Ninja model importer not found at " + NinjaPath);
                return;
            }

            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.optimizeMeshPolygons = true;
            importer.optimizeMeshVertices = true;
            importer.importBlendShapes = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.isReadable = false;
            importer.importAnimation = true;
            importer.animationCompression = ModelImporterAnimationCompression.Optimal;
            importer.animationRotationError = 0.5f;
            importer.animationPositionError = 0.5f;
            importer.animationScaleError = 0.5f;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
            importer.materialSearch = ModelImporterMaterialSearch.Everywhere;
            importer.importNormals = ModelImporterNormals.Calculate;
            importer.importTangents = ModelImporterTangents.CalculateMikk;

            AssetDatabase.ImportAsset(NinjaPath, ImportAssetOptions.ForceUpdate);
            Debug.Log("[RED MOON] Configured Ninja model import: Humanoid, CreateFromThisModel.");
        }

        private static void ConfigureAnimationLibrary()
        {
            var importer = AssetImporter.GetAtPath(AnimationLibraryPath) as ModelImporter;
            if (importer == null)
            {
                Debug.LogError("[RED MOON] Animation library importer not found at " + AnimationLibraryPath);
                return;
            }

            var ninjaAvatar = AssetDatabase.LoadAllAssetsAtPath(NinjaPath)
                .OfType<Avatar>()
                .FirstOrDefault();

            if (ninjaAvatar == null)
            {
                Debug.LogError("[RED MOON] Ninja Avatar was not created. Configure the Ninja model first.");
                return;
            }

            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.optimizeMeshPolygons = true;
            importer.optimizeMeshVertices = true;
            importer.importBlendShapes = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.isReadable = false;
            importer.importAnimation = true;
            importer.importConstraints = false;
            importer.importVisibility = false;
            importer.importAnimatedCustomProperties = false;
            importer.animationCompression = ModelImporterAnimationCompression.Optimal;
            importer.animationRotationError = 0.5f;
            importer.animationPositionError = 0.5f;
            importer.animationScaleError = 0.5f;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;

            AssetDatabase.ImportAsset(AnimationLibraryPath, ImportAssetOptions.ForceUpdate);
            Debug.Log("[RED MOON] Configured animation library: Humanoid, CopyFromOtherAvatar.");
        }

        private static void ConfigureTextures()
        {
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { ArtFolder });

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    continue;
                }

                importer.maxTextureSize = MaxTextureSize;
                importer.textureCompression = TextureImporterCompression.Compressed;
                importer.crunchedCompression = true;
                importer.isReadable = false;
                importer.streamingMipmaps = true;
                importer.streamingMipmapsPriority = 0;
                importer.filterMode = FilterMode.Bilinear;
                importer.anisoLevel = 4;

                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }

            Debug.Log("[RED MOON] Configured " + guids.Length + " character textures (max " + MaxTextureSize + "px).");
        }
    }
}
