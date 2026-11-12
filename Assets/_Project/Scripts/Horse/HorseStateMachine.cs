using System;
using UnityEngine;

namespace RedMoon.Horse
{
    /// <summary>
    /// Explicit state machine for the RED MOON horse.
    /// Manages locomotion, aerial, transition and mounting states with strict validation.
    /// </summary>
    public sealed class HorseStateMachine
    {
        public HorseState CurrentState { get; private set; } = HorseState.Idle;
        public HorseState PreviousState { get; private set; } = HorseState.Idle;

        public event Action<HorseState, HorseState> StateChanged;

        private float _stateTimer;

        public float StateTime => _stateTimer;

        public void Update(float deltaTime)
        {
            _stateTimer += deltaTime;
        }

        public bool ChangeState(HorseState newState)
        {
            if (CurrentState == newState)
            {
                return false;
            }

            // Can't leave Dead state
            if (CurrentState == HorseState.Dead)
            {
                return false;
            }

            PreviousState = CurrentState;
            CurrentState = newState;
            _stateTimer = 0f;

            StateChanged?.Invoke(PreviousState, CurrentState);
            return true;
        }

        /// <summary>
        /// Evaluates appropriate locomotion state given current physical velocity and ground state.
        /// </summary>
        public void EvaluateLocomotionState(
            bool isGrounded,
            float horizontalSpeed,
            float turnRate,
            bool isJumping,
            bool isLanding,
            bool isMounting,
            bool isDismounting,
            HorseConfig config)
        {
            if (CurrentState == HorseState.Dead)
            {
                return;
            }

            if (isMounting)
            {
                ChangeState(HorseState.Mounting);
                return;
            }

            if (isDismounting)
            {
                ChangeState(HorseState.Dismounting);
                return;
            }

            if (!isGrounded)
            {
                if (isJumping)
                {
                    ChangeState(HorseState.Jumping);
                }
                else
                {
                    ChangeState(HorseState.Falling);
                }
                return;
            }

            if (isLanding)
            {
                ChangeState(HorseState.Landing);
                return;
            }

            // Grounded locomotion
            if (horizontalSpeed < 0.15f)
            {
                if (Mathf.Abs(turnRate) > 15f)
                {
                    ChangeState(HorseState.Turning);
                }
                else
                {
                    ChangeState(HorseState.Idle);
                }
            }
            else if (horizontalSpeed <= config.walkSpeed + 0.5f)
            {
                ChangeState(HorseState.Walking);
            }
            else if (horizontalSpeed <= config.trotSpeed + 1.0f)
            {
                ChangeState(HorseState.Trotting);
            }
            else if (horizontalSpeed <= config.gallopSpeed + 1.5f)
            {
                ChangeState(HorseState.Galloping);
            }
            else
            {
                ChangeState(HorseState.Sprinting);
            }
        }
    }
}

