using UnityEngine;

namespace RedMoon.Player
{
    /// <summary>
    /// Stable attachment points for the player visual, cloak and sword.
    /// </summary>
    public sealed class PlayerModelSocket : MonoBehaviour
    {
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Transform swordSocket;
        [SerializeField] private Transform cloakRoot;

        public Transform VisualRoot => visualRoot;
        public Transform SwordSocket => swordSocket;
        public Transform CloakRoot => cloakRoot;

        private void Reset()
        {
            if (visualRoot == null)
            {
                visualRoot = transform;
            }
        }
    }
}
