using UnityEngine;

namespace RedMoon.Horse
{
    /// <summary>
    /// Bridges horse state and physical locomotion parameters to Unity Animator parameters.
    /// Operates gracefully if no Animator or animation clips are present.
    /// </summary>
    public sealed class HorseAnimator : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private HorseConfig config;

        private int _speedHash;
        private int _turnHash;
        private int _groundedHash;
        private int _verticalSpeedHash;
        private int _stateHash;
        private int _jumpHash;
        private int _landHash;
        private int _mountHash;
        private int _dismountHash;

        private float _currentSpeedParam;
        private float _currentTurnParam;
        private bool _hasAnimator;

        public bool HasAnimator => _hasAnimator;

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            _hasAnimator = animator != null;

            if (!_hasAnimator)
            {
                // Clean informative note; procedural motor functions without clips
                Debug.LogWarning("[RED MOON] HorseAnimator found no Animator component; running in procedural locomotion mode.", this);
                return;
            }

            InitializeHashes();
        }

        private void InitializeHashes()
        {
            string speedParam = config != null ? config.animParamSpeed : "Speed";
            string turnParam = config != null ? config.animParamTurn : "Turn";
            string groundedParam = config != null ? config.animParamGrounded : "Grounded";
            string vertParam = config != null ? config.animParamVerticalSpeed : "VerticalSpeed";
            string stateParam = config != null ? config.animParamState : "State";
            string jumpTrigger = config != null ? config.animTriggerJump : "Jump";
            string landTrigger = config != null ? config.animTriggerLand : "Land";
            string mountTrigger = config != null ? config.animTriggerMount : "Mount";
            string dismountTrigger = config != null ? config.animTriggerDismount : "Dismount";

            _speedHash = Animator.StringToHash(speedParam);
            _turnHash = Animator.StringToHash(turnParam);
            _groundedHash = Animator.StringToHash(groundedParam);
            _verticalSpeedHash = Animator.StringToHash(vertParam);
            _stateHash = Animator.StringToHash(stateParam);
            _jumpHash = Animator.StringToHash(jumpTrigger);
            _landHash = Animator.StringToHash(landTrigger);
            _mountHash = Animator.StringToHash(mountTrigger);
            _dismountHash = Animator.StringToHash(dismountTrigger);
        }

        public void UpdateAnimation(float speed, float turnRate, bool isGrounded, float verticalVelocity, HorseState state, float deltaTime)
        {
            if (!_hasAnimator || animator == null)
            {
                return;
            }

            float blendSpeed = config != null ? config.animationBlendSpeed : 8f;
            _currentSpeedParam = Mathf.MoveTowards(_currentSpeedParam, speed, blendSpeed * deltaTime);
            _currentTurnParam = Mathf.MoveTowards(_currentTurnParam, turnRate, blendSpeed * deltaTime);

            animator.SetFloat(_speedHash, _currentSpeedParam);
            animator.SetFloat(_turnHash, _currentTurnParam);
            animator.SetBool(_groundedHash, isGrounded);
            animator.SetFloat(_verticalSpeedHash, verticalVelocity);
            animator.SetInteger(_stateHash, (int)state);
        }

        public void TriggerJump()
        {
            if (_hasAnimator && animator != null)
            {
                animator.SetTrigger(_jumpHash);
            }
        }

        public void TriggerLand()
        {
            if (_hasAnimator && animator != null)
            {
                animator.SetTrigger(_landHash);
            }
        }

        public void TriggerMount()
        {
            if (_hasAnimator && animator != null)
            {
                animator.SetTrigger(_mountHash);
            }
        }

        public void TriggerDismount()
        {
            if (_hasAnimator && animator != null)
            {
                animator.SetTrigger(_dismountHash);
            }
        }
    }
}

