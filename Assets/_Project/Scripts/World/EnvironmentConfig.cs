using UnityEngine;

namespace RedMoon.World
{
    /// <summary>
    /// Configuration asset for RED MOON environment parameters.
    /// Defines terrain scale, elevation profiles, road dimensions, and atmospheric lighting targets.
    /// </summary>
    [CreateAssetMenu(fileName = "Environment_Config", menuName = "RED MOON/Environment/Environment Config")]
    public class EnvironmentConfig : ScriptableObject
    {
        [Header("Terrain Dimensions (Meters)")]
        [Tooltip("Width of the terrain along the X axis.")]
        public float terrainWidth = 500f;

        [Tooltip("Length of the terrain along the Z axis.")]
        public float terrainLength = 500f;

        [Tooltip("Maximum height of the terrain along the Y axis.")]
        public float terrainHeight = 120f;

        [Header("Terrain Resolution (Optimized for RTX 3050 4GB VRAM)")]
        [Tooltip("Resolution of the heightmap (must be 2^n + 1).")]
        public int heightmapResolution = 513;

        [Tooltip("Resolution of the splatmap alphamap.")]
        public int alphamapResolution = 512;

        [Tooltip("Detail resolution for grass/terrain details.")]
        public int detailResolution = 512;

        [Header("Topography Elevation Targets")]
        [Tooltip("Average base elevation of the playable mountain valley floor in meters.")]
        public float valleyBaseHeight = 10f;

        [Tooltip("Maximum elevation of the mountain boundary ridges in meters.")]
        public float mountainPeakHeight = 95f;

        [Tooltip("Slope angle threshold in degrees where rock cliff splatmap starts taking over.")]
        public float cliffSlopeAngleThreshold = 25f;

        [Header("Mountain Road Geometry")]
        [Tooltip("Width of the main road surface in meters.")]
        public float roadWidth = 5.0f;

        [Tooltip("Width of the road shoulder transition in meters.")]
        public float roadShoulderWidth = 3.0f;

        [Tooltip("Maximum allowable slope angle along the graded roadbed in degrees.")]
        public float maxRoadSlope = 12f;

        [Header("Staging & Spawning")]
        [Tooltip("Initial spawn position for the player and horse in world space.")]
        public Vector3 spawnPosition = new Vector3(230f, 10.5f, 230f);

        [Tooltip("Heading yaw angle in degrees when spawning.")]
        public float spawnYaw = 65f;

        [Header("Atmosphere Baseline (Dark Horror)")]
        public Color directionalLightColor = new Color(0.65f, 0.72f, 0.85f); // Cool desaturated moonlight
        public float directionalLightIntensity = 0.65f;
        public Vector3 directionalLightRotation = new Vector3(45f, -40f, 0f);
        public Color ambientEquatorColor = new Color(0.08f, 0.09f, 0.12f);
        public Color ambientGroundColor = new Color(0.04f, 0.04f, 0.05f);
    }
}

