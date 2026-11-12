using System;
using UnityEngine;

namespace RedMoon.World
{
    /// <summary>
    /// Represents a winding mountain road spline through the RED MOON environment.
    /// Provides continuous spline evaluation, tangent headings, road width boundaries,
    /// and spatial proximity queries for gameplay systems, AI navigation, and level design.
    /// </summary>
    [ExecuteAlways]
    public class RoadSpline : MonoBehaviour
    {
        [Header("Road Configuration")]
        [SerializeField] private float roadWidth = 5.0f;
        [SerializeField] private float shoulderWidth = 2.5f;

        [Header("Waypoints (World Space)")]
        [SerializeField] private Vector3[] waypoints = new Vector3[0];

        public float RoadWidth => roadWidth;
        public float ShoulderWidth => shoulderWidth;
        public Vector3[] Waypoints => waypoints;
        public int WaypointCount => waypoints != null ? waypoints.Length : 0;

        public void SetWaypoints(Vector3[] newWaypoints, float width = 5.0f, float shoulder = 2.5f)
        {
            waypoints = newWaypoints ?? new Vector3[0];
            roadWidth = width;
            shoulderWidth = shoulder;
        }

        /// <summary>
        /// Evaluates a smooth Catmull-Rom position along the road spline.
        /// t ranges from 0.0 (start of road) to 1.0 (end of road).
        /// </summary>
        public Vector3 GetPoint(float t)
        {
            if (waypoints == null || waypoints.Length == 0)
                return transform.position;

            if (waypoints.Length == 1)
                return waypoints[0];

            t = Mathf.Clamp01(t);
            int numSegments = waypoints.Length - 1;
            float scaledT = t * numSegments;
            int segIndex = Mathf.Min(Mathf.FloorToInt(scaledT), numSegments - 1);
            float localT = scaledT - segIndex;

            Vector3 p0 = segIndex > 0 ? waypoints[segIndex - 1] : waypoints[segIndex];
            Vector3 p1 = waypoints[segIndex];
            Vector3 p2 = waypoints[segIndex + 1];
            Vector3 p3 = (segIndex + 2 < waypoints.Length) ? waypoints[segIndex + 2] : p2;

            return EvaluateCatmullRom(p0, p1, p2, p3, localT);
        }

        /// <summary>
        /// Evaluates the normalized tangent (forward direction) of the road at t.
        /// </summary>
        public Vector3 GetTangent(float t)
        {
            float dt = 0.002f;
            float t1 = Mathf.Max(0f, t - dt);
            float t2 = Mathf.Min(1f, t + dt);
            Vector3 delta = GetPoint(t2) - GetPoint(t1);
            if (delta.sqrMagnitude < 0.0001f)
                return Vector3.forward;
            return delta.normalized;
        }

        /// <summary>
        /// Evaluates the normal (horizontal perpendicular) to the road heading.
        /// </summary>
        public Vector3 GetNormal(float t)
        {
            Vector3 tangent = GetTangent(t);
            return Vector3.Cross(Vector3.up, tangent).normalized;
        }

        /// <summary>
        /// Approximates total road spline length in world units.
        /// </summary>
        public float GetTotalLength(int samples = 100)
        {
            if (waypoints == null || waypoints.Length < 2)
                return 0f;

            float length = 0f;
            Vector3 prev = GetPoint(0f);
            for (int i = 1; i <= samples; i++)
            {
                float t = (float)i / samples;
                Vector3 curr = GetPoint(t);
                length += Vector3.Distance(prev, curr);
                prev = curr;
            }
            return length;
        }

        /// <summary>
        /// Finds the closest point on the road spline to a given world position.
        /// </summary>
        public Vector3 GetClosestPoint(Vector3 worldPos, out float normalizedT, out float distance)
        {
            normalizedT = 0f;
            distance = float.MaxValue;
            Vector3 bestPoint = Vector3.zero;

            if (waypoints == null || waypoints.Length < 2)
            {
                if (waypoints != null && waypoints.Length == 1)
                {
                    bestPoint = waypoints[0];
                    distance = Vector3.Distance(worldPos, bestPoint);
                }
                return bestPoint;
            }

            int samples = Mathf.Max(50, waypoints.Length * 10);
            for (int i = 0; i <= samples; i++)
            {
                float t = (float)i / samples;
                Vector3 pt = GetPoint(t);
                float dist = Vector3.Distance(worldPos, pt);
                if (dist < distance)
                {
                    distance = dist;
                    normalizedT = t;
                    bestPoint = pt;
                }
            }

            // Fine sub-step refinement around best sample
            float step = 1.0f / samples;
            float tMin = Mathf.Max(0f, normalizedT - step);
            float tMax = Mathf.Min(1f, normalizedT + step);
            for (int j = 0; j <= 20; j++)
            {
                float t = Mathf.Lerp(tMin, tMax, (float)j / 20f);
                Vector3 pt = GetPoint(t);
                float dist = Vector3.Distance(worldPos, pt);
                if (dist < distance)
                {
                    distance = dist;
                    normalizedT = t;
                    bestPoint = pt;
                }
            }

            return bestPoint;
        }

        /// <summary>
        /// Checks whether a world position is within the road bounds (including tolerance).
        /// </summary>
        public bool IsOnRoad(Vector3 worldPos, float tolerance = 0f)
        {
            GetClosestPoint(worldPos, out _, out float dist);
            return dist <= ((roadWidth * 0.5f) + tolerance);
        }

        private static Vector3 EvaluateCatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;

            return 0.5f * (
                (2f * p1) +
                (-p0 + p2) * t +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                (-p0 + 3f * p1 - 3f * p2 + p3) * t3
            );
        }

        private void OnDrawGizmos()
        {
            if (waypoints == null || waypoints.Length < 2)
                return;

            Gizmos.color = new Color(0.9f, 0.6f, 0.2f, 0.85f);
            int samples = Mathf.Max(60, waypoints.Length * 12);
            Vector3 prev = GetPoint(0f);

            for (int i = 1; i <= samples; i++)
            {
                float t = (float)i / samples;
                Vector3 curr = GetPoint(t);
                Gizmos.DrawLine(prev, curr);

                // Draw lateral road boundaries every few samples
                if (i % 6 == 0)
                {
                    Vector3 normal = GetNormal(t);
                    Vector3 left = curr - normal * (roadWidth * 0.5f);
                    Vector3 right = curr + normal * (roadWidth * 0.5f);
                    Gizmos.color = new Color(0.8f, 0.4f, 0.1f, 0.4f);
                    Gizmos.DrawLine(left, right);
                    Gizmos.color = new Color(0.9f, 0.6f, 0.2f, 0.85f);
                }

                prev = curr;
            }

            // Draw waypoints
            Gizmos.color = Color.red;
            for (int w = 0; w < waypoints.Length; w++)
            {
                Gizmos.DrawSphere(waypoints[w], 0.75f);
            }
        }
    }
}

