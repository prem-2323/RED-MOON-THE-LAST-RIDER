namespace RedMoon.Player
{
    /// <summary>
    /// Deterministic player locomotion state machine.
    /// </summary>
    public sealed class PlayerStateMachine
    {
        public PlayerState CurrentState { get; private set; }

        private float _landingTimer;

        public void Reset()
        {
            CurrentState = PlayerState.Idle;
            _landingTimer = 0f;
        }

        public PlayerState Update(
            bool isGrounded,
            bool justLanded,
            float verticalVelocity,
            float horizontalSpeed,
            bool jumpRequested,
            float deltaTime,
            PlayerConfig config)
        {
            if (jumpRequested && isGrounded)
            {
                _landingTimer = 0f;
                CurrentState = PlayerState.Jumping;
                return CurrentState;
            }

            if (!isGrounded)
            {
                _landingTimer = 0f;
                CurrentState = verticalVelocity > 0.1f ? PlayerState.Jumping : PlayerState.Falling;
                return CurrentState;
            }

            if (justLanded)
            {
                _landingTimer = config.landingDuration;
            }

            if (_landingTimer > 0f)
            {
                _landingTimer -= deltaTime;
                CurrentState = PlayerState.Landing;
                return CurrentState;
            }

            if (horizontalSpeed > config.sprintThreshold)
            {
                CurrentState = PlayerState.Sprinting;
            }
            else if (horizontalSpeed > config.runThreshold)
            {
                CurrentState = PlayerState.Running;
            }
            else if (horizontalSpeed > config.walkThreshold)
            {
                CurrentState = PlayerState.Walking;
            }
            else
            {
                CurrentState = PlayerState.Idle;
            }

            return CurrentState;
        }
    }
}
