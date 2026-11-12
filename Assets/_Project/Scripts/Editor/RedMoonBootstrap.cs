using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RedMoon.Editor
{
    /// <summary>
    /// One-shot project bootstrap for RED MOON: THE LAST RIDER.
    /// Creates the URP render pipeline assets, assigns them, creates the folder
    /// structure and an initial bootstrap scene.
    /// Safe to run multiple times (assets are reused if they already exist).
    /// </summary>
    public static class RedMoonBootstrap
    {
        private const string Root = "Assets/_Project";
        private const string SettingsFolder = Root + "/Settings/URP";
        private const string ScenesFolder = Root + "/Scenes";
        private const string BootstrapFolder = ScenesFolder + "/Bootstrap";
        private const string RendererPath = SettingsFolder + "/REDMOON_Renderer.asset";
        private const string PipelinePath = SettingsFolder + "/REDMOON_URP.asset";
        private const string ScenePath = BootstrapFolder + "/Bootstrap.unity";

        [MenuItem("RED MOON/Configure Project")]
        public static void Configure()
        {
            Debug.Log("[RED MOON] Starting project bootstrap...");

            EnsureFolder("Assets", "_Project");
            EnsureFolder(Root, "Art");
            EnsureFolder(Root + "/Art", "Materials");
            EnsureFolder(Root, "Audio");
            EnsureFolder(Root, "Prefabs");
            EnsureFolder(Root, "Scenes");
            EnsureFolder(ScenesFolder, "Bootstrap");
            EnsureFolder(Root, "Scripts");
            EnsureFolder(Root, "Settings");
            EnsureFolder(Root + "/Settings", "URP");
            EnsureFolder(Root, "UI");

            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (rendererData == null)
            {
                rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(rendererData, RendererPath);
                Debug.Log("[RED MOON] Created renderer data: " + RendererPath);
            }

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(rendererData);
                pipeline.name = "REDMOON_URP";
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
                Debug.Log("[RED MOON] Created URP asset: " + PipelinePath);
            }

            // Initial quality profile for the RTX 3050 4GB target.
            pipeline.supportsHDR = true;
            pipeline.msaaSampleCount = 1;
            pipeline.renderScale = 1.0f;
            pipeline.mainLightShadowmapResolution = 2048;
            pipeline.shadowDistance = 55f;
            pipeline.shadowCascadeCount = 2;
            pipeline.supportsCameraDepthTexture = true;
            pipeline.supportsCameraOpaqueTexture = false;
            pipeline.maxAdditionalLightsCount = 4;
            EditorUtility.SetDirty(pipeline);

            GraphicsSettings.defaultRenderPipeline = pipeline;
            int previousQuality = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipeline;
            }
            QualitySettings.SetQualityLevel(previousQuality, false);

            PlayerSettings.companyName = "RED MOON";
            PlayerSettings.productName = "RED MOON: THE LAST RIDER";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone, "com.redmoon.lastrider");
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.runInBackground = true;

            if (!File.Exists(ScenePath))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, ScenePath);
                EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
                Debug.Log("[RED MOON] Created bootstrap scene: " + ScenePath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[RED MOON] Project bootstrap complete. URP active: " + PipelinePath);
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }
    }
}
