using System.IO;
using UnityEditor;
using UnityEngine;

namespace RedMoon.Editor
{
    /// <summary>
    /// Generates high-fidelity URP textures, materials, and TerrainLayers for the RED MOON environment.
    /// Calibrated for proper linear color space dynamic range, dark fantasy gothic aesthetics,
    /// and clear visual readability under moonlight:
    /// - Dark Earthen Soil & Withered Mountain Grass
    /// - Cold Jagged Slate & Fractured Basalt Cliff Rock
    /// - Worn Mountain Road with Packed Gravel, Rut Channels, and Cobblestones
    /// </summary>
    public static class EnvironmentAssetGenerator
    {
        public const string TexturesDir = "Assets/_Project/Art/Environment/Terrain/Textures";
        public const string LayersDir = "Assets/_Project/Art/Environment/Terrain/Layers";
        public const string MaterialsDir = "Assets/_Project/Art/Environment/Terrain/Materials";
        public const string BlockoutDir = "Assets/_Project/Art/Environment/Blockout";

        public const string SoilAlbedoPath = TexturesDir + "/T_DarkSoil_Albedo.png";
        public const string SoilNormalPath = TexturesDir + "/T_DarkSoil_Normal.png";
        public const string SoilLayerPath = LayersDir + "/TL_DarkSoil_DeadGrass.terrainlayer";

        public const string RockAlbedoPath = TexturesDir + "/T_CliffRock_Albedo.png";
        public const string RockNormalPath = TexturesDir + "/T_CliffRock_Normal.png";
        public const string RockLayerPath = LayersDir + "/TL_MountainCliff_Rock.terrainlayer";

        public const string RoadAlbedoPath = TexturesDir + "/T_MountainRoad_Albedo.png";
        public const string RoadNormalPath = TexturesDir + "/T_MountainRoad_Normal.png";
        public const string RoadLayerPath = LayersDir + "/TL_MountainRoad_Dirt.terrainlayer";

        public const string RoadMatPath = MaterialsDir + "/M_MountainRoad_Surface.mat";
        public const string RockMatPath = BlockoutDir + "/M_CliffRock_Blockout.mat";

        [MenuItem("RED MOON/Environment/Generate Terrain Layers & Textures")]
        public static TerrainLayer[] GenerateTerrainAssets()
        {
            if (!Directory.Exists(TexturesDir)) Directory.CreateDirectory(TexturesDir);
            if (!Directory.Exists(LayersDir)) Directory.CreateDirectory(LayersDir);
            if (!Directory.Exists(MaterialsDir)) Directory.CreateDirectory(MaterialsDir);
            if (!Directory.Exists(BlockoutDir)) Directory.CreateDirectory(BlockoutDir);

            // 1. Generate Rich High-Contrast Textures (1024x1024)
            GenerateSoilTextures();
            GenerateRockTextures();
            GenerateRoadTextures();

            AssetDatabase.Refresh();

            // 2. Create TerrainLayers
            var soilLayer = CreateOrUpdateTerrainLayer(SoilLayerPath, SoilAlbedoPath, SoilNormalPath, new Vector2(10f, 10f), 0.12f);
            var rockLayer = CreateOrUpdateTerrainLayer(RockLayerPath, RockAlbedoPath, RockNormalPath, new Vector2(14f, 14f), 0.22f);
            var roadLayer = CreateOrUpdateTerrainLayer(RoadLayerPath, RoadAlbedoPath, RoadNormalPath, new Vector2(6f, 6f), 0.18f);

            // 3. Create Road Mesh Material & Rock Blockout Material
            CreateOrUpdateMaterial(RoadMatPath, RoadAlbedoPath, RoadNormalPath, 0.16f);
            CreateOrUpdateMaterial(RockMatPath, RockAlbedoPath, RockNormalPath, 0.22f);

            AssetDatabase.SaveAssets();
            Debug.Log("[RED MOON] High-contrast environment textures, layers, and materials successfully generated.");

            return new TerrainLayer[] { soilLayer, rockLayer, roadLayer };
        }

        private static void GenerateSoilTextures()
        {
            int res = 1024;
            var albedo = new Texture2D(res, res, TextureFormat.RGBA32, true);
            var normal = new Texture2D(res, res, TextureFormat.RGBA32, true);

            // Rich dark fantasy soil and withered moss/grass colors (proper linear brightness)
            Color soilBase = new Color(0.24f, 0.21f, 0.18f);       // Damp fertile dark earth
            Color deadGrass = new Color(0.32f, 0.30f, 0.22f);      // Withered yellow-olive highland grass
            Color mossGreen = new Color(0.20f, 0.24f, 0.18f);      // Cold mountain lichen/moss
            Color darkCrevice = new Color(0.14f, 0.12f, 0.10f);    // Deep earth shadows

            for (int y = 0; y < res; y++)
            {
                float v = (float)y / res;
                for (int x = 0; x < res; x++)
                {
                    float u = (float)x / res;

                    float n1 = Mathf.PerlinNoise(u * 8f, v * 8f);
                    float n2 = Mathf.PerlinNoise(u * 20f, v * 20f) * 0.5f;
                    float n3 = Mathf.PerlinNoise(u * 50f, v * 50f) * 0.25f;
                    float noise = n1 + n2 + n3;

                    Color c = Color.Lerp(soilBase, deadGrass, Mathf.Clamp01((noise - 0.45f) * 1.8f));
                    if (n2 > 0.32f)
                    {
                        c = Color.Lerp(c, mossGreen, (n2 - 0.32f) * 2.2f);
                    }
                    if (noise < 0.35f)
                    {
                        c = Color.Lerp(c, darkCrevice, (0.35f - noise) * 3f);
                    }

                    albedo.SetPixel(x, y, c);

                    // Normal map calculation with fine rocky soil grain
                    float nx = (Mathf.PerlinNoise(u * 32f, v * 32f) - 0.5f) * 0.5f + (Mathf.PerlinNoise(u * 80f, v * 80f) - 0.5f) * 0.25f;
                    float ny = (Mathf.PerlinNoise(u * 32f + 100f, v * 32f + 100f) - 0.5f) * 0.5f + (Mathf.PerlinNoise(u * 80f + 100f, v * 80f + 100f) - 0.5f) * 0.25f;
                    float nz = Mathf.Sqrt(Mathf.Max(0.01f, 1f - nx * nx - ny * ny));
                    normal.SetPixel(x, y, new Color(nx * 0.5f + 0.5f, ny * 0.5f + 0.5f, nz * 0.5f + 0.5f, 1f));
                }
            }

            albedo.Apply();
            normal.Apply();

            SaveAndImportTexture(albedo, SoilAlbedoPath, false);
            SaveAndImportTexture(normal, SoilNormalPath, true);
            Object.DestroyImmediate(albedo);
            Object.DestroyImmediate(normal);
        }

        private static void GenerateRockTextures()
        {
            int res = 1024;
            var albedo = new Texture2D(res, res, TextureFormat.RGBA32, true);
            var normal = new Texture2D(res, res, TextureFormat.RGBA32, true);

            // Cold mountain slate and fractured rock strata (clear contrast under directional light)
            Color slateDark = new Color(0.20f, 0.21f, 0.23f);      // Shadowed slate base
            Color slateMid = new Color(0.32f, 0.34f, 0.38f);       // Exposed weathered rock face
            Color slateHighlight = new Color(0.44f, 0.46f, 0.50f); // Sharp ridge edge highlight
            Color rockFissure = new Color(0.10f, 0.11f, 0.13f);    // Deep fractured crevice shadow

            for (int y = 0; y < res; y++)
            {
                float v = (float)y / res;
                for (int x = 0; x < res; x++)
                {
                    float u = (float)x / res;

                    // Directional strata: horizontal and diagonal rock layering
                    float strata = Mathf.PerlinNoise(u * 6f + v * 2f, v * 24f);
                    float detail = Mathf.PerlinNoise(u * 36f, v * 36f) * 0.35f;
                    float micro = Mathf.PerlinNoise(u * 90f, v * 90f) * 0.15f;
                    float rockNoise = strata + detail + micro;

                    Color c = Color.Lerp(slateDark, slateMid, rockNoise);
                    if (strata > 0.65f)
                    {
                        c = Color.Lerp(c, slateHighlight, (strata - 0.65f) * 2.5f);
                    }
                    if (strata < 0.22f)
                    {
                        c = Color.Lerp(c, rockFissure, (0.22f - strata) * 4f);
                    }

                    albedo.SetPixel(x, y, c);

                    // High contrast rock normal map with sharp ledge dropoffs
                    float nx = (Mathf.PerlinNoise(u * 12f + v * 4f, v * 48f) - 0.5f) * 0.75f;
                    float ny = (Mathf.PerlinNoise(u * 12f + 150f, v * 48f + 150f) - 0.5f) * 0.75f;
                    float nz = Mathf.Sqrt(Mathf.Max(0.01f, 1f - nx * nx - ny * ny));
                    normal.SetPixel(x, y, new Color(nx * 0.5f + 0.5f, ny * 0.5f + 0.5f, nz * 0.5f + 0.5f, 1f));
                }
            }

            albedo.Apply();
            normal.Apply();

            SaveAndImportTexture(albedo, RockAlbedoPath, false);
            SaveAndImportTexture(normal, RockNormalPath, true);
            Object.DestroyImmediate(albedo);
            Object.DestroyImmediate(normal);
        }

        private static void GenerateRoadTextures()
        {
            int res = 1024;
            var albedo = new Texture2D(res, res, TextureFormat.RGBA32, true);
            var normal = new Texture2D(res, res, TextureFormat.RGBA32, true);

            // Worn mountain road with cobblestone gravel, crushed stone, and packed wheel ruts
            Color roadBase = new Color(0.28f, 0.26f, 0.23f);       // Packed earth & dust
            Color crushedGravel = new Color(0.42f, 0.40f, 0.36f);  // Lighter crushed stone/pebbles
            Color wheelRut = new Color(0.18f, 0.16f, 0.14f);       // Compacted dark muddy rut channels
            Color stoneCobble = new Color(0.35f, 0.33f, 0.30f);    // Embedded cobblestones

            for (int y = 0; y < res; y++)
            {
                float v = (float)y / res;
                for (int x = 0; x < res; x++)
                {
                    float u = (float)x / res;

                    // Wheel ruts along vertical/longitudinal direction
                    float rut1 = Mathf.Abs(u - 0.32f);
                    float rut2 = Mathf.Abs(u - 0.68f);
                    float rutDist = Mathf.Min(rut1, rut2);

                    float pebbles = Mathf.PerlinNoise(u * 40f, v * 40f);
                    float cobbles = Mathf.PerlinNoise(u * 16f, v * 16f);
                    float fineGrain = Mathf.PerlinNoise(u * 80f, v * 80f) * 0.2f;

                    Color c = Color.Lerp(roadBase, crushedGravel, pebbles * 0.7f + fineGrain);
                    if (cobbles > 0.55f)
                    {
                        c = Color.Lerp(c, stoneCobble, (cobbles - 0.55f) * 2f);
                    }

                    // Darker compacted depression in wheel ruts
                    if (rutDist < 0.10f)
                    {
                        float rutWeight = (1f - (rutDist / 0.10f));
                        c = Color.Lerp(c, wheelRut, rutWeight * 0.65f);
                    }

                    albedo.SetPixel(x, y, c);

                    // Cobblestones and rut normal variations
                    float nx = (Mathf.PerlinNoise(u * 48f, v * 48f) - 0.5f) * 0.55f;
                    float ny = (Mathf.PerlinNoise(u * 48f + 120f, v * 48f + 120f) - 0.5f) * 0.55f;
                    if (rutDist < 0.10f)
                    {
                        nx += (u > (rut1 < rut2 ? 0.32f : 0.68f) ? -0.25f : 0.25f);
                    }
                    float nz = Mathf.Sqrt(Mathf.Max(0.01f, 1f - nx * nx - ny * ny));
                    normal.SetPixel(x, y, new Color(nx * 0.5f + 0.5f, ny * 0.5f + 0.5f, nz * 0.5f + 0.5f, 1f));
                }
            }

            albedo.Apply();
            normal.Apply();

            SaveAndImportTexture(albedo, RoadAlbedoPath, false);
            SaveAndImportTexture(normal, RoadNormalPath, true);
            Object.DestroyImmediate(albedo);
            Object.DestroyImmediate(normal);
        }

        private static void SaveAndImportTexture(Texture2D tex, string path, bool isNormalMap)
        {
            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                if (isNormalMap)
                {
                    importer.textureType = TextureImporterType.NormalMap;
                    importer.sRGBTexture = false;
                }
                else
                {
                    importer.textureType = TextureImporterType.Default;
                    importer.sRGBTexture = true;
                }
                importer.mipmapEnabled = true;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.SaveAndReimport();
            }
        }

        private static TerrainLayer CreateOrUpdateTerrainLayer(string assetPath, string albedoPath, string normalPath, Vector2 tileSize, float smoothness)
        {
            var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(assetPath);
            if (layer == null)
            {
                layer = new TerrainLayer();
                AssetDatabase.CreateAsset(layer, assetPath);
            }

            var albedoTex = AssetDatabase.LoadAssetAtPath<Texture2D>(albedoPath);
            var normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);

            layer.diffuseTexture = albedoTex;
            layer.normalMapTexture = normalTex;
            layer.tileSize = tileSize;
            layer.tileOffset = Vector2.zero;
            layer.smoothness = smoothness;
            layer.metallic = 0f;

            EditorUtility.SetDirty(layer);
            return layer;
        }

        private static void CreateOrUpdateMaterial(string matPath, string albedoPath, string normalPath, float smoothness)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, matPath);
            }

            var albedoTex = AssetDatabase.LoadAssetAtPath<Texture2D>(albedoPath);
            var normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);

            mat.SetTexture("_BaseMap", albedoTex);
            mat.SetTexture("_BumpMap", normalTex);
            mat.EnableKeyword("_NORMALMAP");
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetColor("_BaseColor", Color.white);

            EditorUtility.SetDirty(mat);
        }
    }
}
