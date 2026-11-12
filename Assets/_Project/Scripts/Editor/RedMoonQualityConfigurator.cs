using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using ShadowQuality = UnityEngine.ShadowQuality;
using ShadowResolution = UnityEngine.ShadowResolution;

namespace RedMoon.Editor
{
    /// <summary>
    /// Creates and assigns the RED MOON quality-tier render pipeline assets.
    /// The existing REDMOON_URP asset is treated as the High tier and is never
    /// recreated or overwritten. Low, Medium and Ultra assets share the existing
    /// REDMOON_Renderer and differ only in measured, documented rendering costs.
    /// </summary>
    public static class RedMoonQualityConfigurator
    {
        private const string UrpFolder = "Assets/_Project/Settings/URP";
        private const string RendererPath = UrpFolder + "/REDMOON_Renderer.asset";
        private const string HighPath = UrpFolder + "/REDMOON_URP.asset";
        private const string LowPath = UrpFolder + "/REDMOON_URP_Low.asset";
        private const string MediumPath = UrpFolder + "/REDMOON_URP_Medium.asset";
        private const string UltraPath = UrpFolder + "/REDMOON_URP_Ultra.asset";

        [MenuItem("RED MOON/Rendering/Configure Quality Tiers")]
        public static void Configure()
        {
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (renderer == null)
            {
                Debug.LogError("[RED MOON] Missing renderer data: " + RendererPath);
                return;
            }

            var high = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(HighPath);
            if (high == null)
            {
                Debug.LogError("[RED MOON] Missing High pipeline asset: " + HighPath);
                return;
            }

            var low = LoadOrCreate(LowPath, "REDMOON_URP_Low", renderer);
            var medium = LoadOrCreate(MediumPath, "REDMOON_URP_Medium", renderer);
            var ultra = LoadOrCreate(UltraPath, "REDMOON_URP_Ultra", renderer);
            if (low == null || medium == null || ultra == null)
            {
                Debug.LogError("[RED MOON] Could not create quality pipeline assets.");
                return;
            }

            ConfigurePipeline(low, 0.80f, 1024, 1, 30f, 0, false, 1);
            ConfigurePipeline(medium, 0.90f, 2048, 2, 40f, 2, true, 1);
            ConfigurePipeline(ultra, 1.00f, 4096, 4, 70f, 8, true, 4);

            // High is intentionally not written: REDMOON_URP is the preserved baseline.
            ConfigureQualityLevel("Very Low", low, ShadowQuality.Disable, ShadowResolution.Low, 1, 20f, 0, 0.5f, AnisotropicFiltering.Disable, 32, false, false);
            ConfigureQualityLevel("Low", low, ShadowQuality.All, ShadowResolution.Low, 1, 30f, 0, 0.7f, AnisotropicFiltering.Disable, 64, false, true);
            ConfigureQualityLevel("Medium", medium, ShadowQuality.All, ShadowResolution.Medium, 2, 40f, 2, 1.0f, AnisotropicFiltering.Enable, 256, false, true);
            ConfigureQualityLevel("High", high, ShadowQuality.All, ShadowResolution.High, 2, 55f, 4, 1.5f, AnisotropicFiltering.ForceEnable, 512, true, true);
            ConfigureQualityLevel("Very High", ultra, ShadowQuality.All, ShadowResolution.VeryHigh, 4, 70f, 6, 2.0f, AnisotropicFiltering.ForceEnable, 1024, true, true);
            ConfigureQualityLevel("Ultra", ultra, ShadowQuality.All, ShadowResolution.VeryHigh, 4, 70f, 8, 2.0f, AnisotropicFiltering.ForceEnable, 2048, true, true);

            int highIndex = System.Array.IndexOf(QualitySettings.names, "High");
            if (highIndex >= 0)
            {
                QualitySettings.SetQualityLevel(highIndex, false);
                SetStandaloneDefaultQuality(highIndex);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[RED MOON] Quality tiers configured. High remains the default for Standalone.");
        }

        private static UniversalRenderPipelineAsset LoadOrCreate(string path, string assetName, UniversalRendererData renderer)
        {
            var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
            if (asset != null)
                return asset;

            asset = UniversalRenderPipelineAsset.Create(renderer);
            asset.name = assetName;
            AssetDatabase.CreateAsset(asset, path);
            Debug.Log("[RED MOON] Created quality pipeline: " + path);
            return asset;
        }

        private static void ConfigurePipeline(
            UniversalRenderPipelineAsset pipeline,
            float renderScale,
            int shadowMapResolution,
            int cascadeCount,
            float shadowDistance,
            int maxAdditionalLights,
            bool softShadows,
            int msaaSampleCount)
        {
            pipeline.renderScale = renderScale;
            pipeline.supportsHDR = true;
            pipeline.msaaSampleCount = msaaSampleCount;
            pipeline.mainLightShadowmapResolution = shadowMapResolution;
            pipeline.shadowCascadeCount = cascadeCount;
            pipeline.shadowDistance = shadowDistance;
            pipeline.maxAdditionalLightsCount = maxAdditionalLights;
            pipeline.supportsCameraDepthTexture = true;
            pipeline.supportsCameraOpaqueTexture = false;
            pipeline.colorGradingMode = ColorGradingMode.HighDynamicRange;
            pipeline.useSRPBatcher = true;

            // Soft/extra-light shadow support and light rendering modes are
            // read-only in the public API; set their serialized backing fields.
            var serialized = new SerializedObject(pipeline);
            serialized.FindProperty("m_SoftShadowsSupported").boolValue = softShadows;
            serialized.FindProperty("m_MainLightShadowsSupported").boolValue = true;
            serialized.FindProperty("m_AdditionalLightShadowsSupported").boolValue = false;
            serialized.ApplyModifiedProperties();

            EditorUtility.SetDirty(pipeline);
        }

        private static void ConfigureQualityLevel(
            string levelName,
            UniversalRenderPipelineAsset pipeline,
            ShadowQuality shadows,
            ShadowResolution shadowResolution,
            int shadowCascades,
            float shadowDistance,
            int pixelLightCount,
            float lodBias,
            AnisotropicFiltering anisotropicFiltering,
            int particleRaycastBudget,
            bool softParticles,
            bool softVegetation)
        {
            int index = System.Array.IndexOf(QualitySettings.names, levelName);
            if (index < 0)
            {
                Debug.LogWarning("[RED MOON] Quality level not found: " + levelName);
                return;
            }

            QualitySettings.SetQualityLevel(index, false);
            QualitySettings.renderPipeline = pipeline;
            QualitySettings.shadows = shadows;
            QualitySettings.shadowResolution = shadowResolution;
            QualitySettings.shadowCascades = shadowCascades;
            QualitySettings.shadowDistance = shadowDistance;
            QualitySettings.pixelLightCount = pixelLightCount;
            QualitySettings.antiAliasing = 0;
            QualitySettings.vSyncCount = 1;
            QualitySettings.lodBias = lodBias;
            QualitySettings.anisotropicFiltering = anisotropicFiltering;
            QualitySettings.particleRaycastBudget = particleRaycastBudget;
            QualitySettings.softParticles = softParticles;
            QualitySettings.softVegetation = softVegetation;
            QualitySettings.realtimeReflectionProbes = index >= 2;
            QualitySettings.billboardsFaceCameraPosition = true;
        }

        private static void SetStandaloneDefaultQuality(int index)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset");
            if (assets == null || assets.Length == 0)
            {
                Debug.LogWarning("[RED MOON] Could not load QualitySettings.asset.");
                return;
            }

            var serialized = new SerializedObject(assets[0]);
            var perPlatform = serialized.FindProperty("m_PerPlatformDefaultQuality");
            if (perPlatform == null || !perPlatform.isArray)
            {
                Debug.LogWarning("[RED MOON] Could not find per-platform quality defaults.");
                return;
            }

            for (int i = 0; i < perPlatform.arraySize; i++)
            {
                var entry = perPlatform.GetArrayElementAtIndex(i);
                var platform = entry.FindPropertyRelative("first");
                if (platform != null && platform.stringValue == "Standalone")
                {
                    entry.FindPropertyRelative("second").intValue = index;
                    serialized.ApplyModifiedProperties();
                    return;
                }
            }

            Debug.LogWarning("[RED MOON] Could not find Standalone default quality.");
        }
    }
}
