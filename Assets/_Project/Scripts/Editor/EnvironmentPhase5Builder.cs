using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RedMoon.World;
using RedMoon.Player;
using RedMoon.Horse;
using RedMoon.Camera;

namespace RedMoon.Editor
{
    /// <summary>
    /// Professional Builder for RED MOON Phase 5.1 Environment.
    /// Sculptures an expansive 500m x 500m mountain valley landscape:
    /// - Broad, natural valley floor (y ~ 2-8m) with rolling foothills and open vistas
    /// - Towering mountain massifs (y ~ 50-75m) on North-West and South-East flanks
    /// - Flat outer terrain borders (y = 0m) eliminating the floating funnel/crater effect
    /// - Winding mountain pass road with smooth grading and dedicated visual road mesh
    /// - Organic 3D faceted rock spires and cliff crags (no primitive cubes)
    /// - Calibrated high-contrast dark fantasy moonlight and atmospheric fog
    /// </summary>
    public static class EnvironmentPhase5Builder
    {
        public const string ScenePath = "Assets/_Project/Scenes/Development/EnvironmentTest.unity";
        public const string TerrainDataPath = "Assets/_Project/Art/Environment/Terrain/TerrainData_MountainRegion.asset";
        public const string TerrainMatPath = "Assets/_Project/Art/Environment/Terrain/M_Terrain_MountainRegion.mat";
        public const string RoadMatPath = "Assets/_Project/Art/Environment/Terrain/Materials/M_MountainRoad_Surface.mat";
        public const string RockMatPath = "Assets/_Project/Art/Environment/Blockout/M_CliffRock_Blockout.mat";
        public const string ConfigPath = "Assets/_Project/Settings/Environment/Environment_Config.asset";
        public const string HorsePrefabPath = "Assets/_Project/Prefabs/Horse/Horse.prefab";
        public const string PlayerPrefabPath = "Assets/_Project/Prefabs/Characters/Player_Interim.prefab";
        public const string CameraConfigPath = "Assets/_Project/Settings/Camera/Camera_Config.asset";
        public const string InputActionsPath = "Assets/_Project/Settings/Input/REDMOON_InputActions.inputactions";

        public const string RockSpirePath = "Assets/_Project/Art/Environment/Blockout/Models/Rock_Spire_01.obj";
        public const string RockCragPath = "Assets/_Project/Art/Environment/Blockout/Models/Rock_Crag_02.obj";

        // 10 Road Spline Waypoints across the broad mountain valley floor
        public static readonly Vector3[] RoadWaypoints = new Vector3[]
        {
            new Vector3(80f,  3.5f,  70f),  // WP0: South-West valley overlook & entrance
            new Vector3(120f, 3.8f, 110f),  // WP1: Gentle curve through southern foothills
            new Vector3(160f, 4.2f, 160f),  // WP2: Skirting the western ridge base
            new Vector3(195f, 4.5f, 195f),  // WP3: Entering wide central meadow
            new Vector3(230f, 4.8f, 230f),  // WP4: Central Staging Area (Player & Horse spawn)
            new Vector3(265f, 5.2f, 265f),  // WP5: Passing between valley knolls
            new Vector3(305f, 5.8f, 305f),  // WP6: Turning toward northern valley pass
            new Vector3(345f, 6.5f, 350f),  // WP7: Gentle incline along eastern crags
            new Vector3(385f, 7.2f, 395f),  // WP8: Highland saddle approach
            new Vector3(420f, 8.0f, 430f)   // WP9: North-East plateau overlook
        };

        [MenuItem("RED MOON/Environment/Build Phase 5.1 Environment Scene")]
        public static void BuildPhase5EnvironmentScene()
        {
            Debug.Log("[RED MOON] Building improved Phase 5.1 Mountain Environment...");

            int groundLayer = LayerMask.NameToLayer("Ground");
            int envLayer = LayerMask.NameToLayer("Environment");

            // 1. Generate 3D Rock OBJ Models & Textures
            EnsureRockModelsExist();
            var layers = EnvironmentAssetGenerator.GenerateTerrainAssets();

            // 2. Ensure Environment Config
            var envConfig = AssetDatabase.LoadAssetAtPath<EnvironmentConfig>(ConfigPath);
            if (envConfig == null)
            {
                envConfig = ScriptableObject.CreateInstance<EnvironmentConfig>();
                AssetDatabase.CreateAsset(envConfig, ConfigPath);
            }

            // 3. Create or Update TerrainData
            var terrainData = GenerateTerrainData(layers);

            // 4. Create Material for URP Terrain
            var terrainMaterial = AssetDatabase.LoadAssetAtPath<Material>(TerrainMatPath);
            if (terrainMaterial == null)
            {
                var terrainShader = Shader.Find("Universal Render Pipeline/Terrain/Lit");
                terrainMaterial = new Material(terrainShader);
                AssetDatabase.CreateAsset(terrainMaterial, TerrainMatPath);
            }

            // 5. Create Fresh Scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 6. Directional Moonlight & Atmosphere
            SetupLightingAndAtmosphere();

            // 7. Instantiate Single Terrain GameObject
            var terrainObj = Terrain.CreateTerrainGameObject(terrainData);
            terrainObj.name = "Terrain_MountainRegion";
            terrainObj.layer = groundLayer;
            terrainObj.tag = "Environment";
            terrainObj.transform.position = Vector3.zero;

            var terrain = terrainObj.GetComponent<Terrain>();
            terrain.materialTemplate = terrainMaterial;
            terrain.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            terrain.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

            var terrainCollider = terrainObj.GetComponent<TerrainCollider>();
            terrainCollider.terrainData = terrainData;

            // Clean up any extraneous Terrain object Unity might create
            var duplicateTerrain = GameObject.Find("Terrain");
            if (duplicateTerrain != null && duplicateTerrain != terrainObj)
            {
                Object.DestroyImmediate(duplicateTerrain);
            }

            // 8. Create RoadSpline
            var roadObj = new GameObject("Road_MainMountainPass");
            roadObj.layer = groundLayer;
            roadObj.tag = "Environment";
            var roadSpline = roadObj.AddComponent<RoadSpline>();
            roadSpline.SetWaypoints(RoadWaypoints, 5.0f, 3.0f);

            // 9. Generate Visible Road Ribbon Mesh
            CreateRoadRibbonMesh(roadObj, roadSpline, terrain, groundLayer);

            // 10. Environment Blockouts (Organic 3D Rock Spires & Crags)
            CreateOrganicRockBlockouts(envLayer);

            // 11. Staging Position (WP4)
            Vector3 horseSpawnPos = RoadWaypoints[4]; // (230, 4.8, 230)
            float sampledHeight = terrain.SampleHeight(horseSpawnPos);
            horseSpawnPos.y = sampledHeight + 0.05f;

            Vector3 playerSpawnPos = horseSpawnPos + new Vector3(-2.0f, 0f, 0f);
            playerSpawnPos.y = terrain.SampleHeight(playerSpawnPos) + 0.05f;

            // 12. Instantiate Horse Prefab
            var horsePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HorsePrefabPath);
            GameObject horseInstance = null;
            if (horsePrefab != null)
            {
                horseInstance = (GameObject)PrefabUtility.InstantiatePrefab(horsePrefab);
                horseInstance.name = "Horse";
                horseInstance.transform.position = horseSpawnPos;
                horseInstance.transform.rotation = Quaternion.Euler(0f, 65f, 0f);
            }
            else
            {
                Debug.LogError("[RED MOON] Horse prefab missing at: " + HorsePrefabPath);
            }

            // 13. Instantiate Player Prefab
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            GameObject playerInstance = null;
            if (playerPrefab != null)
            {
                playerInstance = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
                playerInstance.name = "Player";
                playerInstance.transform.position = playerSpawnPos;
                playerInstance.transform.rotation = Quaternion.Euler(0f, 65f, 0f);
            }
            else
            {
                Debug.LogError("[RED MOON] Player prefab missing at: " + PlayerPrefabPath);
            }

            // 14. Setup Main Camera & CameraController
            var cameraObj = new GameObject("Main Camera");
            cameraObj.tag = "MainCamera";
            cameraObj.transform.position = playerSpawnPos + new Vector3(0f, 1.8f, -3.2f);
            cameraObj.transform.rotation = Quaternion.Euler(15f, 65f, 0f);

            var cam = cameraObj.AddComponent<UnityEngine.Camera>();
            cam.fieldOfView = 60f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 1000f;
            cameraObj.AddComponent<AudioListener>();

            var cameraConfig = AssetDatabase.LoadAssetAtPath<CameraConfig>(CameraConfigPath);
            var actions = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(InputActionsPath);

            var camController = cameraObj.AddComponent<CameraController>();
            var soCam = new SerializedObject(camController);
            soCam.FindProperty("config").objectReferenceValue = cameraConfig;
            soCam.FindProperty("target").objectReferenceValue = playerInstance != null ? playerInstance.transform : (horseInstance != null ? horseInstance.transform : null);
            if (playerInstance != null)
            {
                soCam.FindProperty("playerController").objectReferenceValue = playerInstance.GetComponent<PlayerController>();
            }
            soCam.FindProperty("inputActions").objectReferenceValue = actions;
            soCam.FindProperty("lockCursor").boolValue = true;
            soCam.ApplyModifiedProperties();

            // 15. Save Scene
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();

            Debug.Log($"[RED MOON] Mountain Environment successfully created at: {ScenePath}");
        }

        private static TerrainData GenerateTerrainData(TerrainLayer[] layers)
        {
            var terrainData = AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainDataPath);
            if (terrainData == null)
            {
                terrainData = new TerrainData();
                AssetDatabase.CreateAsset(terrainData, TerrainDataPath);
            }

            terrainData.size = new Vector3(500f, 80f, 500f);
            terrainData.heightmapResolution = 513;
            terrainData.alphamapResolution = 512;
            terrainData.terrainLayers = layers;

            int hRes = terrainData.heightmapResolution;
            float[,] heights = new float[hRes, hRes];

            float maxTerrainHeight = 80f;
            float roadWidth = 5.0f;
            float halfRoad = roadWidth * 0.5f;
            float shoulder = 3.5f;

            for (int z = 0; z < hRes; z++)
            {
                float wz = (float)z / (hRes - 1) * 500f;
                float v = wz / 500f;

                for (int x = 0; x < hRes; x++)
                {
                    float wx = (float)x / (hRes - 1) * 500f;
                    float u = wx / 500f;

                    // Distance to closest road segment and interpolated road elevation
                    float roadDist = GetDistanceToRoadSpline(wx, wz, out float roadHeight);

                    // 1. Natural Mountain Valley Topography
                    // Valley floor width: ~200m wide (roadDist < 100m stays low and gentle)
                    float valleyFactor = Mathf.Clamp01((roadDist - 40f) / 120f);
                    valleyFactor = valleyFactor * valleyFactor * (3f - 2f * valleyFactor); // smoothstep

                    // Mountain Massifs: Asymmetrically rising higher on North-West
                    float nwBias = Mathf.Clamp01((500f - wx + wz) / 500f);
                    float mountainHeightTarget = Mathf.Lerp(45f, 72f, nwBias);

                    // Multi-octave natural terrain noise
                    float macroMountains = Mathf.PerlinNoise(u * 2.2f, v * 2.2f) * 22f;
                    float ridges = Mathf.Abs(Mathf.PerlinNoise(u * 4.5f, v * 4.5f) - 0.5f) * 2f * 18f;
                    float rollingFoothills = Mathf.PerlinNoise(u * 10f, v * 10f) * 3.5f;
                    float valleyMeanders = (Mathf.PerlinNoise(u * 6f, v * 6f) - 0.5f) * 1.5f;

                    // Valley floor elevation sits naturally around 3-6m
                    float valleyFloorElevation = 3.5f + (u + v) * 2.5f + valleyMeanders;
                    float highMountainElevation = valleyFloorElevation + (mountainHeightTarget * valleyFactor) + macroMountains + ridges + rollingFoothills;

                    float naturalHeight = Mathf.Lerp(valleyFloorElevation, highMountainElevation, valleyFactor);

                    // 2. Smooth edge falloff to avoid floating box/funnel look
                    // Terrain edges blend gently down toward 0m so it rests naturally on ground grid
                    float borderDist = Mathf.Min(Mathf.Min(wx, 500f - wx), Mathf.Min(wz, 500f - wz));
                    if (borderDist < 25f)
                    {
                        float edgeT = borderDist / 25f;
                        naturalHeight *= (edgeT * edgeT);
                    }

                    // 3. Road Bed Grading (continuous, smooth, gentle crown)
                    float finalHeight;
                    if (roadDist <= halfRoad)
                    {
                        float crown = (1f - (roadDist / halfRoad)) * 0.04f;
                        finalHeight = roadHeight + crown;
                    }
                    else if (roadDist <= halfRoad + shoulder)
                    {
                        float t = (roadDist - halfRoad) / shoulder;
                        float smoothT = t * t * (3f - 2f * t);
                        finalHeight = Mathf.Lerp(roadHeight, naturalHeight, smoothT);
                    }
                    else
                    {
                        finalHeight = naturalHeight;
                    }

                    heights[z, x] = Mathf.Clamp01(finalHeight / maxTerrainHeight);
                }
            }
            terrainData.SetHeights(0, 0, heights);

            // 2. Procedural Splatmaps (Alphamaps)
            int aRes = terrainData.alphamapResolution;
            float[,,] alphamaps = new float[aRes, aRes, 3];

            for (int z = 0; z < aRes; z++)
            {
                float wz = (float)z / (aRes - 1) * 500f;
                int hz = Mathf.RoundToInt((float)z / (aRes - 1) * (hRes - 1));

                for (int x = 0; x < aRes; x++)
                {
                    float wx = (float)x / (aRes - 1) * 500f;
                    int hx = Mathf.RoundToInt((float)x / (aRes - 1) * (hRes - 1));

                    float roadDist = GetDistanceToRoadSpline(wx, wz, out _);

                    int hxPrev = Mathf.Max(0, hx - 1);
                    int hxNext = Mathf.Min(hRes - 1, hx + 1);
                    int hzPrev = Mathf.Max(0, hz - 1);
                    int hzNext = Mathf.Min(hRes - 1, hz + 1);

                    float dx = (heights[hz, hxNext] - heights[hz, hxPrev]) * maxTerrainHeight / (2f * 500f / (hRes - 1));
                    float dz = (heights[hzNext, hx] - heights[hzPrev, hx]) * maxTerrainHeight / (2f * 500f / (hRes - 1));
                    float slopeDeg = Mathf.Atan(Mathf.Sqrt(dx * dx + dz * dz)) * Mathf.Rad2Deg;

                    float currentHeight = heights[hz, hx] * maxTerrainHeight;

                    float wSoil = 0f;
                    float wRock = 0f;
                    float wRoad = 0f;

                    if (roadDist <= halfRoad)
                    {
                        wRoad = 1.0f;
                    }
                    else if (roadDist <= halfRoad + shoulder)
                    {
                        float t = (roadDist - halfRoad) / shoulder;
                        float smoothT = t * t * (3f - 2f * t);
                        wRoad = 1f - smoothT;

                        float rem = 1f - wRoad;
                        float rockRatio = Mathf.Clamp01((slopeDeg - 18f) / 10f);
                        wRock = rem * rockRatio;
                        wSoil = rem * (1f - rockRatio);
                    }
                    else
                    {
                        wRoad = 0f;
                        float rockRatio = Mathf.Clamp01((slopeDeg - 20f) / 10f);
                        if (currentHeight > 35f)
                        {
                            rockRatio = Mathf.Max(rockRatio, Mathf.Clamp01((currentHeight - 35f) / 25f));
                        }
                        wRock = rockRatio;
                        wSoil = 1f - wRock;
                    }

                    float sum = wSoil + wRock + wRoad;
                    if (sum > 0.0001f)
                    {
                        alphamaps[z, x, 0] = wSoil / sum; // Soil / Dead Grass
                        alphamaps[z, x, 1] = wRock / sum; // Mountain Cliff Rock
                        alphamaps[z, x, 2] = wRoad / sum; // Mountain Dirt Road
                    }
                    else
                    {
                        alphamaps[z, x, 0] = 1f;
                    }
                }
            }
            terrainData.SetAlphamaps(0, 0, alphamaps);

            EditorUtility.SetDirty(terrainData);
            return terrainData;
        }

        private static float GetDistanceToRoadSpline(float wx, float wz, out float roadHeight)
        {
            float minSqrDist = float.MaxValue;
            roadHeight = 4.8f;
            Vector2 pt = new Vector2(wx, wz);

            for (int i = 0; i < RoadWaypoints.Length - 1; i++)
            {
                Vector2 a = new Vector2(RoadWaypoints[i].x, RoadWaypoints[i].z);
                Vector2 b = new Vector2(RoadWaypoints[i + 1].x, RoadWaypoints[i + 1].z);
                Vector2 ab = b - a;
                float lenSq = ab.sqrMagnitude;

                float t = lenSq > 0.0001f ? Mathf.Clamp01(Vector2.Dot(pt - a, ab) / lenSq) : 0f;
                Vector2 proj = a + t * ab;
                float sqrDist = (pt - proj).sqrMagnitude;

                if (sqrDist < minSqrDist)
                {
                    minSqrDist = sqrDist;
                    roadHeight = Mathf.Lerp(RoadWaypoints[i].y, RoadWaypoints[i + 1].y, t);
                }
            }

            return Mathf.Sqrt(minSqrDist);
        }

        private static void CreateRoadRibbonMesh(GameObject roadParent, RoadSpline spline, Terrain terrain, int groundLayer)
        {
            var roadMeshObj = new GameObject("Road_VisualMesh");
            roadMeshObj.layer = groundLayer;
            roadMeshObj.tag = "Environment";
            roadMeshObj.transform.SetParent(roadParent.transform, false);

            var mf = roadMeshObj.AddComponent<MeshFilter>();
            var mr = roadMeshObj.AddComponent<MeshRenderer>();

            var roadMat = AssetDatabase.LoadAssetAtPath<Material>(RoadMatPath);
            if (roadMat != null)
            {
                mr.sharedMaterial = roadMat;
            }

            int segments = 160;
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();

            float halfW = spline.RoadWidth * 0.5f;
            float totalLen = spline.GetTotalLength(segments);
            float accumDist = 0f;
            Vector3 prevCenter = spline.GetPoint(0f);

            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                Vector3 center = spline.GetPoint(t);
                accumDist += Vector3.Distance(prevCenter, center);
                prevCenter = center;

                Vector3 normal = spline.GetNormal(t);

                Vector3 left = center - normal * halfW;
                Vector3 right = center + normal * halfW;

                // Conform height to terrain with subtle 0.03m offset to avoid z-fighting
                left.y = terrain.SampleHeight(left) + 0.03f;
                right.y = terrain.SampleHeight(right) + 0.03f;

                vertices.Add(left);
                vertices.Add(right);

                float v = accumDist * 0.15f; // Texture tiling along road
                uvs.Add(new Vector2(0f, v));
                uvs.Add(new Vector2(1f, v));

                if (i > 0)
                {
                    int currLeft = i * 2;
                    int currRight = i * 2 + 1;
                    int prevLeft = (i - 1) * 2;
                    int prevRight = (i - 1) * 2 + 1;

                    triangles.Add(prevLeft);
                    triangles.Add(currLeft);
                    triangles.Add(prevRight);

                    triangles.Add(prevRight);
                    triangles.Add(currLeft);
                    triangles.Add(currRight);
                }
            }

            var mesh = new Mesh();
            mesh.name = "MountainRoad_Mesh";
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            mf.sharedMesh = mesh;
        }

        private static void CreateOrganicRockBlockouts(int envLayer)
        {
            var root = new GameObject("Environment_Blockouts");
            root.layer = envLayer;
            root.tag = "Environment";

            var rockMat = AssetDatabase.LoadAssetAtPath<Material>(RockMatPath);
            var spirePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RockSpirePath);
            var cragPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RockCragPath);

            // 6 Prominent Rock Formations on mountain ridges & defile entrances
            Vector3[] rockPositions = new Vector3[]
            {
                new Vector3(180f, 48f, 410f), // North Ridge Summit Spire
                new Vector3(390f, 42f, 260f), // East Sentinel Crag
                new Vector3(90f,  18f,  55f), // South Valley Gate Left
                new Vector3(55f,  20f,  95f), // South Valley Gate Right
                new Vector3(65f,  54f, 280f), // West Lookout Massif
                new Vector3(415f, 52f, 420f)  // North-East Ridge Spire
            };

            Vector3[] rockScales = new Vector3[]
            {
                new Vector3(1.2f, 1.4f, 1.2f),
                new Vector3(1.5f, 1.2f, 1.5f),
                new Vector3(1.0f, 1.0f, 1.0f),
                new Vector3(1.0f, 1.1f, 1.0f),
                new Vector3(1.4f, 1.5f, 1.3f),
                new Vector3(1.3f, 1.3f, 1.2f)
            };

            for (int i = 0; i < rockPositions.Length; i++)
            {
                GameObject modelSource = (i % 2 == 0) ? spirePrefab : cragPrefab;
                GameObject rockInstance;

                if (modelSource != null)
                {
                    rockInstance = (GameObject)PrefabUtility.InstantiatePrefab(modelSource, root.transform);
                }
                else
                {
                    rockInstance = new GameObject($"Rock_Cluster_{i + 1}");
                    rockInstance.transform.SetParent(root.transform, false);
                }

                rockInstance.name = $"Rock_Formation_{i + 1}";
                rockInstance.layer = envLayer;
                rockInstance.tag = "Environment";
                rockInstance.transform.position = rockPositions[i];
                rockInstance.transform.localScale = rockScales[i];
                rockInstance.transform.rotation = Quaternion.Euler(0f, (i * 57f) % 360f, 0f);

                var renderers = rockInstance.GetComponentsInChildren<MeshRenderer>();
                foreach (var r in renderers)
                {
                    if (rockMat != null) r.sharedMaterial = rockMat;
                }

                // Ensure MeshColliders for physical presence
                var filters = rockInstance.GetComponentsInChildren<MeshFilter>();
                foreach (var f in filters)
                {
                    var col = f.gameObject.GetComponent<MeshCollider>();
                    if (col == null)
                    {
                        col = f.gameObject.AddComponent<MeshCollider>();
                    }
                    col.sharedMesh = f.sharedMesh;
                }
            }
        }

        private static void EnsureRockModelsExist()
        {
            string dir = Path.GetDirectoryName(RockSpirePath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            if (!File.Exists(RockSpirePath))
            {
                GenerateProceduralRockOBJ(RockSpirePath, 14f, 30f, 14f, 7, 5, 101);
                AssetDatabase.ImportAsset(RockSpirePath, ImportAssetOptions.ForceUpdate);
            }
            if (!File.Exists(RockCragPath))
            {
                GenerateProceduralRockOBJ(RockCragPath, 20f, 22f, 18f, 8, 4, 202);
                AssetDatabase.ImportAsset(RockCragPath, ImportAssetOptions.ForceUpdate);
            }
        }

        private static void GenerateProceduralRockOBJ(string path, float width, float height, float length, int radialSegments, int heightTiers, int seed)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# RED MOON Organic Rock Model");
            sb.AppendLine("o Rock_Crag");

            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int[]>();

            vertices.Add(Vector3.zero);
            uvs.Add(new Vector2(0.5f, 0.5f));

            var rnd = new System.Random(seed);

            for (int t = 0; t <= heightTiers; t++)
            {
                float tierFrac = (float)t / heightTiers;
                float y = tierFrac * height;
                float taper = Mathf.Cos(tierFrac * Mathf.PI * 0.45f);
                if (tierFrac > 0.85f) taper *= (1f - (tierFrac - 0.85f) / 0.15f) * 0.8f;

                for (int r = 0; r < radialSegments; r++)
                {
                    float angle = (float)r / radialSegments * Mathf.PI * 2f;
                    float jitter = 0.75f + (float)rnd.NextDouble() * 0.5f;
                    float rx = Mathf.Cos(angle) * (width * 0.5f * taper * jitter);
                    float rz = Mathf.Sin(angle) * (length * 0.5f * taper * jitter);
                    float yOffset = ((float)rnd.NextDouble() - 0.5f) * (height / heightTiers * 0.3f);

                    vertices.Add(new Vector3(rx, y + (t == 0 ? 0f : yOffset), rz));
                    uvs.Add(new Vector2((float)r / radialSegments * 3f, tierFrac * 4f));
                }
            }

            int tipIndex = vertices.Count;
            vertices.Add(new Vector3(0f, height + 1.5f, 0f));
            uvs.Add(new Vector2(0.5f, 1f));

            for (int t = 0; t < heightTiers; t++)
            {
                int rCur = 1 + (t * radialSegments);
                int rNext = 1 + ((t + 1) * radialSegments);

                for (int r = 0; r < radialSegments; r++)
                {
                    int nR = (r + 1) % radialSegments;
                    int c0 = rCur + r;
                    int c1 = rCur + nR;
                    int n0 = rNext + r;
                    int n1 = rNext + nR;

                    triangles.Add(new int[] { c0, n0, c1 });
                    triangles.Add(new int[] { c1, n0, n1 });
                }
            }

            for (int r = 0; r < radialSegments; r++)
            {
                int nR = (r + 1) % radialSegments;
                triangles.Add(new int[] { 0, 1 + nR, 1 + r });
            }

            int topStart = 1 + (heightTiers * radialSegments);
            for (int r = 0; r < radialSegments; r++)
            {
                int nR = (r + 1) % radialSegments;
                triangles.Add(new int[] { topStart + r, topStart + nR, tipIndex });
            }

            foreach (var v in vertices)
            {
                sb.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture, "v {0:F4} {1:F4} {2:F4}", v.x, v.y, v.z));
            }
            foreach (var uv in uvs)
            {
                sb.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture, "vt {0:F4} {1:F4}", uv.x, uv.y));
            }
            foreach (var tri in triangles)
            {
                sb.AppendLine($"f {tri[0] + 1}/{tri[0] + 1} {tri[1] + 1}/{tri[1] + 1} {tri[2] + 1}/{tri[2] + 1}");
            }

            File.WriteAllText(path, sb.ToString());
        }

        private static void SetupLightingAndAtmosphere()
        {
            var lightObj = new GameObject("Moonlight_Directional");
            var light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.78f, 0.84f, 0.96f); // Crisp cool silvery moonlight
            light.intensity = 1.20f;                      // Clear visibility
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.75f;
            lightObj.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.14f, 0.17f, 0.24f);
            RenderSettings.ambientEquatorColor = new Color(0.10f, 0.11f, 0.15f);
            RenderSettings.ambientGroundColor = new Color(0.06f, 0.06f, 0.08f);

            // Subtle atmospheric horror fog
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 90f;
            RenderSettings.fogEndDistance = 420f;
            RenderSettings.fogColor = new Color(0.10f, 0.12f, 0.16f);
        }
    }
}
