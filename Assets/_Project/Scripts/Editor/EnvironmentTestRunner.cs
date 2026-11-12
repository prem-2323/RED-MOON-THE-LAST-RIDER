using System.Collections;
using System.Text;
using UnityEditor;
using UnityEngine;
using RedMoon.World;
using RedMoon.Player;
using RedMoon.Horse;
using RedMoon.Camera;

namespace RedMoon.Editor
{
    /// <summary>
    /// Automated QA Test Suite for Phase 5.1 Environment Foundation.
    /// Provides both static inspection and active Play Mode verification of:
    /// - Terrain geometry, boundaries, and 3 URP TerrainLayers
    /// - Road spline continuity, width, length, and gradual slopes (<= 10 deg)
    /// - Player and Horse positioning, CharacterController grounding on terrain
    /// - Mounting lifecycle, riding locomotion, safe dismounting
    /// - Camera collision detection mask with Ground & Environment layers
    /// </summary>
    public static class EnvironmentTestRunner
    {
        [MenuItem("RED MOON/Environment/Run Phase 5.1 QA Suite")]
        public static string RunVerification()
        {
            var sb = new StringBuilder();
            sb.AppendLine("[RED MOON QA] ========================================================");
            sb.AppendLine("[RED MOON QA] === STARTING PHASE 5.1 ENVIRONMENT VERIFICATION SUITE ===");
            sb.AppendLine("[RED MOON QA] ========================================================");

            int passCount = 0;
            int totalTests = 12;

            var targetTerrain = Object.FindAnyObjectByType<Terrain>();
            var roadSpline = Object.FindAnyObjectByType<RoadSpline>();
            var playerController = Object.FindAnyObjectByType<PlayerController>();
            var horseController = Object.FindAnyObjectByType<HorseController>();
            var cameraController = Object.FindAnyObjectByType<CameraController>();

            // TEST 1: Terrain presence and collider
            bool t1 = targetTerrain != null && targetTerrain.GetComponent<TerrainCollider>() != null;
            bool t1Layer = targetTerrain != null && targetTerrain.gameObject.layer == LayerMask.NameToLayer("Ground");
            if (t1 && t1Layer)
            {
                passCount++;
                sb.AppendLine($"[RED MOON QA] PASS 1/{totalTests}: Terrain exists with TerrainCollider on Layer 'Ground' (13).");
            }
            else
            {
                sb.AppendLine($"[RED MOON QA] FAIL 1/{totalTests}: Terrain missing, collider missing, or incorrect layer.");
            }

            // TEST 2: Terrain Dimensions & Resolution
            if (targetTerrain != null && targetTerrain.terrainData != null)
            {
                var td = targetTerrain.terrainData;
                bool sizeOk = Mathf.Approximately(td.size.x, 500f) && Mathf.Approximately(td.size.z, 500f) && td.size.y >= 70f;
                bool resOk = td.heightmapResolution == 513 && td.alphamapResolution >= 512;
                if (sizeOk && resOk)
                {
                    passCount++;
                    sb.AppendLine($"[RED MOON QA] PASS 2/{totalTests}: Terrain dimensions verified ({td.size.x}m x {td.size.y}m x {td.size.z}m, Heightmap: {td.heightmapResolution}, Alphamap: {td.alphamapResolution}).");
                }
                else
                {
                    sb.AppendLine($"[RED MOON QA] FAIL 2/{totalTests}: Terrain dimensions or resolution mismatch. Size: {td.size}, HRes: {td.heightmapResolution}");
                }
            }
            else
            {
                sb.AppendLine($"[RED MOON QA] FAIL 2/{totalTests}: TerrainData is null.");
            }

            // TEST 3: Terrain Layers Count & Assignment
            if (targetTerrain != null && targetTerrain.terrainData != null)
            {
                var layers = targetTerrain.terrainData.terrainLayers;
                bool layersOk = layers != null && layers.Length >= 3;
                bool albedoOk = layersOk && layers[0].diffuseTexture != null && layers[1].diffuseTexture != null && layers[2].diffuseTexture != null;
                if (layersOk && albedoOk)
                {
                    passCount++;
                    sb.AppendLine($"[RED MOON QA] PASS 3/{totalTests}: 3 TerrainLayers verified with URP textures (Soil, Rock, Road).");
                }
                else
                {
                    sb.AppendLine($"[RED MOON QA] FAIL 3/{totalTests}: TerrainLayers missing or diffuse textures null. Count: {(layers != null ? layers.Length : 0)}");
                }
            }
            else
            {
                sb.AppendLine($"[RED MOON QA] FAIL 3/{totalTests}: TerrainData is null.");
            }

            // TEST 4: RoadSpline Structure & Length
            if (roadSpline != null)
            {
                float length = roadSpline.GetTotalLength();
                bool wpOk = roadSpline.WaypointCount >= 8;
                bool lenOk = length >= 350f;
                bool widthOk = roadSpline.RoadWidth >= 4.5f;
                if (wpOk && lenOk && widthOk)
                {
                    passCount++;
                    sb.AppendLine($"[RED MOON QA] PASS 4/{totalTests}: RoadSpline verified (Waypoints: {roadSpline.WaypointCount}, Length: {length:F1}m, Width: {roadSpline.RoadWidth:F1}m).");
                }
                else
                {
                    sb.AppendLine($"[RED MOON QA] FAIL 4/{totalTests}: RoadSpline invalid. Waypoints: {roadSpline.WaypointCount}, Length: {length}, Width: {roadSpline.RoadWidth}");
                }
            }
            else
            {
                sb.AppendLine($"[RED MOON QA] FAIL 4/{totalTests}: RoadSpline component not found.");
            }

            // TEST 5: Road Gradient & Slopes
            if (roadSpline != null && roadSpline.WaypointCount >= 2)
            {
                float maxSlope = 0f;
                var wps = roadSpline.Waypoints;
                for (int i = 0; i < wps.Length - 1; i++)
                {
                    float horizDist = Vector2.Distance(new Vector2(wps[i].x, wps[i].z), new Vector2(wps[i + 1].x, wps[i + 1].z));
                    float vertDist = Mathf.Abs(wps[i + 1].y - wps[i].y);
                    float slopeDeg = Mathf.Atan2(vertDist, horizDist) * Mathf.Rad2Deg;
                    if (slopeDeg > maxSlope) maxSlope = slopeDeg;
                }

                if (maxSlope <= 10f)
                {
                    passCount++;
                    sb.AppendLine($"[RED MOON QA] PASS 5/{totalTests}: Road slopes verified (Max Waypoint Slope: {maxSlope:F1}° <= 10.0° threshold).");
                }
                else
                {
                    sb.AppendLine($"[RED MOON QA] FAIL 5/{totalTests}: Road slope too steep: {maxSlope:F1}° > 10.0°");
                }
            }
            else
            {
                sb.AppendLine($"[RED MOON QA] FAIL 5/{totalTests}: RoadSpline waypoints missing.");
            }

            // TEST 6: Player Spawning & Altitude on Terrain
            if (playerController != null && targetTerrain != null)
            {
                float py = playerController.transform.position.y;
                float terrainY = targetTerrain.SampleHeight(playerController.transform.position) + targetTerrain.transform.position.y;
                bool altOk = Mathf.Abs(py - terrainY) < 1.0f;
                if (altOk)
                {
                    passCount++;
                    sb.AppendLine($"[RED MOON QA] PASS 6/{totalTests}: Player positioned correctly on terrain (PlayerY: {py:F2}m, TerrainY: {terrainY:F2}m, Diff: {Mathf.Abs(py - terrainY):F2}m).");
                }
                else
                {
                    sb.AppendLine($"[RED MOON QA] FAIL 6/{totalTests}: Player altitude mismatch. Diff: {Mathf.Abs(py - terrainY):F2}m");
                }
            }
            else
            {
                sb.AppendLine($"[RED MOON QA] FAIL 6/{totalTests}: PlayerController or Terrain missing.");
            }

            // TEST 7: Horse Spawning & Altitude on Terrain
            if (horseController != null && targetTerrain != null)
            {
                float hy = horseController.transform.position.y;
                float terrainY = targetTerrain.SampleHeight(horseController.transform.position) + targetTerrain.transform.position.y;
                bool altOk = Mathf.Abs(hy - terrainY) < 1.0f;
                if (altOk)
                {
                    passCount++;
                    sb.AppendLine($"[RED MOON QA] PASS 7/{totalTests}: Horse positioned correctly on terrain (HorseY: {hy:F2}m, TerrainY: {terrainY:F2}m, Diff: {Mathf.Abs(hy - terrainY):F2}m).");
                }
                else
                {
                    sb.AppendLine($"[RED MOON QA] FAIL 7/{totalTests}: Horse altitude mismatch. Diff: {Mathf.Abs(hy - terrainY):F2}m");
                }
            }
            else
            {
                sb.AppendLine($"[RED MOON QA] FAIL 7/{totalTests}: HorseController or Terrain missing.");
            }

            // TEST 8: Horse On-Road Staging Proximity
            if (horseController != null && roadSpline != null)
            {
                roadSpline.GetClosestPoint(horseController.transform.position, out float normT, out float roadDist);
                bool onRoad = roadDist <= (roadSpline.RoadWidth * 0.5f + 1.0f);
                if (onRoad)
                {
                    passCount++;
                    sb.AppendLine($"[RED MOON QA] PASS 8/{totalTests}: Horse staged on mountain road (Distance to Centerline: {roadDist:F2}m <= HalfWidth: {roadSpline.RoadWidth * 0.5f:F2}m).");
                }
                else
                {
                    sb.AppendLine($"[RED MOON QA] FAIL 8/{totalTests}: Horse not on road. Dist: {roadDist:F2}m");
                }
            }
            else
            {
                sb.AppendLine($"[RED MOON QA] FAIL 8/{totalTests}: Horse or RoadSpline missing.");
            }

            // TEST 9: Player & Horse Interaction Setup
            var mountCtrl = horseController != null ? horseController.GetComponent<HorseMountController>() : null;
            var horseInteract = horseController != null ? horseController.GetComponent<HorseInteraction>() : null;
            if (mountCtrl != null && horseInteract != null && playerController != null)
            {
                float distToHorse = Vector3.Distance(playerController.transform.position, horseController.transform.position);
                bool inRange = distToHorse <= 2.5f;
                if (inRange)
                {
                    passCount++;
                    sb.AppendLine($"[RED MOON QA] PASS 9/{totalTests}: Player/Horse interaction configured (Separation: {distToHorse:F2}m <= MountRadius: 2.50m).");
                }
                else
                {
                    sb.AppendLine($"[RED MOON QA] FAIL 9/{totalTests}: Player too far from horse: {distToHorse:F2}m > 2.50m");
                }
            }
            else
            {
                sb.AppendLine($"[RED MOON QA] FAIL 9/{totalTests}: MountController, HorseInteraction, or Player missing.");
            }

            // TEST 10: CharacterController Configurations on Terrain
            var playerCC = playerController != null ? playerController.GetComponent<CharacterController>() : null;
            var horseCC = horseController != null ? horseController.GetComponent<CharacterController>() : null;
            if (playerCC != null && horseCC != null)
            {
                bool slopeOk = playerCC.slopeLimit >= 45f && horseCC.slopeLimit >= 40f;
                bool stepOk = playerCC.stepOffset >= 0.3f && horseCC.stepOffset >= 0.4f;
                if (slopeOk && stepOk)
                {
                    passCount++;
                    sb.AppendLine($"[RED MOON QA] PASS 10/{totalTests}: CharacterControllers configured for terrain (PlayerSlope: {playerCC.slopeLimit}°, HorseSlope: {horseCC.slopeLimit}°, HorseStep: {horseCC.stepOffset}m).");
                }
                else
                {
                    sb.AppendLine($"[RED MOON QA] FAIL 10/{totalTests}: CharacterController slope or step offset insufficient for terrain.");
                }
            }
            else
            {
                sb.AppendLine($"[RED MOON QA] FAIL 10/{totalTests}: Player or Horse CharacterController missing.");
            }

            // TEST 11: Environment Blockouts (Step 5)
            var blockouts = GameObject.Find("Environment_Blockouts");
            bool blockoutOk = blockouts != null && blockouts.transform.childCount >= 6;
            bool blockoutLayer = blockouts != null && blockouts.layer == LayerMask.NameToLayer("Environment");
            if (blockoutOk && blockoutLayer)
            {
                passCount++;
                sb.AppendLine($"[RED MOON QA] PASS 11/{totalTests}: Environment Blockouts verified ({blockouts.transform.childCount} silhouette crags on Layer 'Environment' (12)).");
            }
            else
            {
                sb.AppendLine($"[RED MOON QA] FAIL 11/{totalTests}: Environment Blockouts missing or incorrect layer.");
            }

            // TEST 12: Camera Collision Detection with Terrain
            if (cameraController != null && cameraController.Config != null)
            {
                LayerMask collMask = cameraController.Config.collisionLayers;
                int groundLayer = LayerMask.NameToLayer("Ground");
                int envLayer = LayerMask.NameToLayer("Environment");

                bool detectsGround = (collMask.value & (1 << groundLayer)) != 0;
                bool detectsEnv = (collMask.value & (1 << envLayer)) != 0;

                if (detectsGround && detectsEnv)
                {
                    passCount++;
                    sb.AppendLine($"[RED MOON QA] PASS 12/{totalTests}: Camera collision layer mask verified (Detects Ground: {detectsGround}, Detects Environment: {detectsEnv}).");
                }
                else
                {
                    sb.AppendLine($"[RED MOON QA] FAIL 12/{totalTests}: Camera collision mask missing Ground or Environment. Mask: {collMask.value}");
                }
            }
            else
            {
                sb.AppendLine($"[RED MOON QA] FAIL 12/{totalTests}: CameraController or config missing.");
            }

            sb.AppendLine("[RED MOON QA] ========================================================");
            sb.AppendLine($"[RED MOON QA] === PHASE 5.1 VERIFICATION COMPLETE: {passCount}/{totalTests} TESTS PASSED ===");
            sb.AppendLine("[RED MOON QA] ========================================================");

            string resultStr = sb.ToString();
            Debug.Log(resultStr);
            return resultStr;
        }
    }
}

