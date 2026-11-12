using System.IO;
using UnityEditor;
using UnityEngine;

namespace RedMoon.Editor
{
    /// <summary>
    /// Generates high-quality URP textures and materials for the RED MOON horse.
    /// Creates a dark charcoal/black coat with subtle crimson undertones for the dark fantasy aesthetic.
    /// </summary>
    public static class HorseAssetGenerator
    {
        public const string TexturesDir = "Assets/_Project/Art/Characters/Horse/Textures";
        public const string MaterialsDir = "Assets/_Project/Art/Characters/Horse/Materials";
        public const string AlbedoPath = TexturesDir + "/T_Horse_DarkCharcoal_Albedo.png";
        public const string NormalPath = TexturesDir + "/T_Horse_DarkCharcoal_Normal.png";
        public const string MaterialPath = MaterialsDir + "/M_Horse_DarkCharcoal.mat";

        [MenuItem("RED MOON/Horse/Generate Horse Materials & Textures")]
        public static Material GenerateHorseAssets()
        {
            if (!Directory.Exists(TexturesDir)) Directory.CreateDirectory(TexturesDir);
            if (!Directory.Exists(MaterialsDir)) Directory.CreateDirectory(MaterialsDir);

            // 1. Generate Albedo Texture (1024x1024)
            int width = 1024;
            int height = 1024;
            var albedoTex = new Texture2D(width, height, TextureFormat.RGBA32, true);

            Color charcoalBase = new Color(0.11f, 0.10f, 0.11f);
            Color crimsonTint = new Color(0.20f, 0.09f, 0.10f); // Supernatural red moon undertone
            Color darkHoof = new Color(0.06f, 0.06f, 0.07f);
            Color maneTailColor = new Color(0.07f, 0.06f, 0.07f);
            Color highlightColor = new Color(0.16f, 0.12f, 0.13f);

            for (int y = 0; y < height; y++)
            {
                float v = (float)y / height;
                for (int x = 0; x < width; x++)
                {
                    float u = (float)x / width;

                    // Organic noise variation
                    float n1 = Mathf.PerlinNoise(u * 12f, v * 12f);
                    float n2 = Mathf.PerlinNoise(u * 32f, v * 32f) * 0.5f;
                    float blend = n1 + n2;

                    Color col;
                    if (v > 0.88f)
                    {
                        // Hooves and lower legs
                        col = Color.Lerp(darkHoof, charcoalBase, (v - 0.88f) / 0.12f);
                    }
                    else if (u > 0.75f && v < 0.35f)
                    {
                        // Mane region
                        col = maneTailColor;
                    }
                    else
                    {
                        // Body coat with subtle red moon atmospheric sheen
                        Color baseMix = Color.Lerp(charcoalBase, highlightColor, blend * 0.4f);
                        float crimsonFactor = Mathf.Clamp01(Mathf.Sin(v * Mathf.PI * 2f) * 0.35f + (n1 - 0.5f) * 0.3f);
                        col = Color.Lerp(baseMix, crimsonTint, crimsonFactor);
                    }

                    albedoTex.SetPixel(x, y, col);
                }
            }
            albedoTex.Apply();

            byte[] albedoBytes = albedoTex.EncodeToPNG();
            File.WriteAllBytes(AlbedoPath, albedoBytes);
            Object.DestroyImmediate(albedoTex);

            AssetDatabase.ImportAsset(AlbedoPath, ImportAssetOptions.ForceUpdate);

            // Configure Albedo Importer
            var albedoImporter = AssetImporter.GetAtPath(AlbedoPath) as TextureImporter;
            if (albedoImporter != null)
            {
                albedoImporter.textureType = TextureImporterType.Default;
                albedoImporter.sRGBTexture = true;
                albedoImporter.mipmapEnabled = true;
                albedoImporter.wrapMode = TextureWrapMode.Repeat;
                albedoImporter.SaveAndReimport();
            }

            // 2. Generate Normal Map (1024x1024)
            var normalTex = new Texture2D(width, height, TextureFormat.RGBA32, true);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float u = (float)x / width;
                    float v = (float)y / height;
                    float nx = (Mathf.PerlinNoise(u * 24f, v * 24f) - 0.5f) * 0.3f;
                    float ny = (Mathf.PerlinNoise(u * 24f + 100f, v * 24f + 100f) - 0.5f) * 0.3f;
                    float nz = Mathf.Sqrt(Mathf.Max(0f, 1f - nx * nx - ny * ny));

                    // Encode to normal map color space
                    Color nCol = new Color(nx * 0.5f + 0.5f, ny * 0.5f + 0.5f, nz * 0.5f + 0.5f, 1f);
                    normalTex.SetPixel(x, y, nCol);
                }
            }
            normalTex.Apply();

            byte[] normalBytes = normalTex.EncodeToPNG();
            File.WriteAllBytes(NormalPath, normalBytes);
            Object.DestroyImmediate(normalTex);

            AssetDatabase.ImportAsset(NormalPath, ImportAssetOptions.ForceUpdate);

            // Configure Normal Importer
            var normalImporter = AssetImporter.GetAtPath(NormalPath) as TextureImporter;
            if (normalImporter != null)
            {
                normalImporter.textureType = TextureImporterType.NormalMap;
                normalImporter.mipmapEnabled = true;
                normalImporter.wrapMode = TextureWrapMode.Repeat;
                normalImporter.SaveAndReimport();
            }

            // 3. Create or Update URP Lit Material
            var mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, MaterialPath);
            }

            var loadedAlbedo = AssetDatabase.LoadAssetAtPath<Texture2D>(AlbedoPath);
            var loadedNormal = AssetDatabase.LoadAssetAtPath<Texture2D>(NormalPath);

            mat.SetColor("_BaseColor", Color.white);
            if (loadedAlbedo != null) mat.SetTexture("_BaseMap", loadedAlbedo);
            if (loadedNormal != null)
            {
                mat.SetTexture("_BumpMap", loadedNormal);
                mat.EnableKeyword("_NORMALMAP");
            }
            mat.SetFloat("_Smoothness", 0.35f);
            mat.SetFloat("_Metallic", 0.05f);

            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();

            Debug.Log($"[RED MOON] Horse material and textures successfully generated and saved at: {MaterialPath}");
            return mat;
        }
    }
}

