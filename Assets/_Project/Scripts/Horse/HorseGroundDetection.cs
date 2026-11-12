using UnityEngine;

namespace RedMoon.Horse
{
    /// <summary>
    /// Dedicated ground detection and slope evaluation component for the RED MOON horse.
    /// Employs SphereCast queries to verify contact, determine slope angles, and align movement.
    /// </summary>
    public sealed class HorseGroundDetection : MonoBehaviour
    {
        [SerializeField] private HorseConfig config;
        [SerializeField] private Transform groundCheckPoint;

        public bool IsGrounded { get; private set; }
        public float SlopeAngle { get; private set; }
        public Vector3 GroundNormal { get; private set; } = Vector3.up;
        public RaycastHit LastGroundHit { get; private set; }

        public void Initialize(HorseConfig horseConfig, Transform checkPoint)
        {
            config = horseConfig;
            groundCheckPoint = checkPoint;
        }

        public bool CheckGrounded(Vector3 basePosition)
        {
            if (config == null)
            {
                return true;
            }

            // Hoof level check: origin 0.30m above base, radius 0.22m, sweep down 0.20m
            Vector3 origin = basePosition + Vector3.up * 0.30f;
            float radius = 0.22f;
            float castDistance = 0.20f;

            if (Physics.SphereCast(origin, radius, Vector3.down, out RaycastHit hit, castDistance, config.groundLayers, QueryTriggerInteraction.Ignore))
            {
                IsGrounded = true;
                LastGroundHit = hit;
                GroundNormal = hit.normal;
                SlopeAngle = Vector3.Angle(Vector3.up, GroundNormal);
                return true;
            }

            // Fallback raycast directly downward
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit rayHit, castDistance + radius + 0.05f, config.groundLayers, QueryTriggerInteraction.Ignore))
            {
                IsGrounded = true;
                LastGroundHit = rayHit;
                GroundNormal = rayHit.normal;
                SlopeAngle = Vector3.Angle(Vector3.up, GroundNormal);
                return true;
            }

            IsGrounded = false;
            GroundNormal = Vector3.up;
            SlopeAngle = 0f;
            return false;
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 origin = transform.position + Vector3.up * 0.30f;
            Gizmos.color = IsGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(origin, 0.22f);
            Gizmos.DrawRay(origin, Vector3.down * 0.20f);
        }
    }
}
