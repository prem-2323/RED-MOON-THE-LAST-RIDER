using UnityEngine;

namespace RedMoon.Player
{
    /// <summary>
    /// Bridges player locomotion state to Animator parameters.
    /// The animator is optional so the player remains functional before the
    /// final character model and animation set are available.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public sealed class PlayerAnimator : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private PlayerController controller;

        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int VerticalSpeedId = Animator.StringToHash("VerticalSpeed");
        private static readonly int GroundedId = Animator.StringToHash("Grounded");
        private static readonly int TurnId = Animator.StringToHash("Turn");
        private static readonly int StateId = Animator.StringToHash("State");
        private static readonly int JumpId = Animator.StringToHash("Jump");
        private static readonly int LandId = Animator.StringToHash("Land");

        private PlayerState _lastState = PlayerState.Idle;

        private void Awake()
        {
            if (controller == null)
            {
                controller = GetComponent<PlayerController>();
            }

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (animator == null)
            {
                Debug.LogWarning("[RED MOON] PlayerAnimator found no Animator; animation bridging disabled.", this);
                enabled = false;
            }
        }

        private void Update()
        {
            if (animator == null || controller == null)
            {
                return;
            }

            animator.SetFloat(SpeedId, controller.HorizontalSpeed);
            animator.SetFloat(VerticalSpeedId, controller.VerticalVelocity);
            animator.SetBool(GroundedId, controller.IsGrounded);
            animator.SetFloat(TurnId, controller.TurnAngle);
            animator.SetInteger(StateId, (int)controller.CurrentState);

            if (controller.CurrentState != _lastState)
            {
                if (controller.CurrentState == PlayerState.Jumping)
                {
                    animator.SetTrigger(JumpId);
                }
                else if (controller.CurrentState == PlayerState.Landing)
                {
                    animator.SetTrigger(LandId);
                }

                _lastState = controller.CurrentState;
            }
        }
    }
}
