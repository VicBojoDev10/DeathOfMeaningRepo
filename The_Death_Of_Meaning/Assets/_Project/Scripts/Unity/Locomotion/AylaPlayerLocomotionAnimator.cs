using Unity.Netcode;
using UnityEngine;

namespace TDOM.Unity
{
    public class AylaPlayerLocomotionAnimator : NetworkBehaviour
    {
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int MoveXHash = Animator.StringToHash("MoveX");
        private static readonly int MoveYHash = Animator.StringToHash("MoveY");
        private static readonly int IsAimingHash = Animator.StringToHash("IsAiming");
        private static readonly int WalkHash = Animator.StringToHash("walk");
        private static readonly int RunHash = Animator.StringToHash("run");
        private static readonly int JumpHash = Animator.StringToHash("jump");

        [Header("Referencias")]
        [SerializeField]
        private Animator _animator;

        [SerializeField]
        private CharacterController _characterController;

        [Header("Umbrales de Velocidad (m/s)")]
        [SerializeField]
        private float _walkThreshold = 0.5f;

        [SerializeField]
        private float _walkBlendMaxSpeed = 10.0f;

        [SerializeField]
        private float _runBlendMaxSpeed = 20.0f;
        private Vector3 _ultimaPos;
        private float _rawSpeed;
        private float _smoothSpeed;
        private float _tiempoEnAire;
        private bool _isJumpingPrevio;
        private bool _aimState;
        private bool _isSprinting;

        private void Awake()
        {
            if (_animator == null)
                _animator = GetComponentInChildren<Animator>();
            if (_characterController == null)
                _characterController = GetComponent<CharacterController>();
        }

        public void SetAimState(bool aiming)
        {
            if (!IsSpawned || !IsOwner || aiming == _aimState)
                return;

            _aimState = aiming;
            ApplyAimState(aiming);
            SyncAimStateRpc(aiming);
        }

        public void SetSprintState(bool sprinting)
        {
            if (!IsSpawned || !IsOwner || sprinting == _isSprinting)
                return;

            _isSprinting = sprinting;
            SyncSprintStateRpc(sprinting);
        }

        private void ApplyAimState(bool aiming)
        {
            if (_animator != null)
                _animator.SetBool(IsAimingHash, aiming);
        }

        public override void OnNetworkSpawn()
        {
            _ultimaPos = transform.position;
            _smoothSpeed = 0f;
            _tiempoEnAire = 0f;
            _aimState = false;
            _isSprinting = false;
            ApplyAimState(false);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f)
                return;

            Vector3 pos = transform.position;
            Vector3 v = (pos - _ultimaPos) / dt;
            float velY = v.y;
            v.y = 0f;
            _rawSpeed = v.magnitude;
            _ultimaPos = pos;

            _smoothSpeed = Mathf.Lerp(_smoothSpeed, _rawSpeed, dt * 12f);

            bool isJumping = _isJumpingPrevio;
            if (IsOwner && _characterController != null)
            {
                if (!_characterController.isGrounded)
                {
                    _tiempoEnAire += dt;
                }
                else
                {
                    _tiempoEnAire = 0f;
                }

                isJumping = velY > 1.5f || _tiempoEnAire > 0.10f;

                if (isJumping != _isJumpingPrevio)
                {
                    _isJumpingPrevio = isJumping;
                    SyncJumpStateRpc(isJumping);
                }
            }

            bool isRunning = !isJumping && _isSprinting;
            bool isWalking = !isJumping && _smoothSpeed >= _walkThreshold && !isRunning;

            if (_animator != null)
            {
                Vector3 localVelocity = transform.InverseTransformDirection(v);
                _animator.SetFloat(SpeedHash, CalculateLocomotionBlendSpeed(_smoothSpeed));
                Vector3 localDirection =
                    localVelocity.sqrMagnitude > 0.01f ? localVelocity.normalized : Vector3.zero;
                _animator.SetFloat(MoveXHash, localDirection.x);
                _animator.SetFloat(MoveYHash, localDirection.z);
                _animator.SetBool(JumpHash, isJumping);
                _animator.SetBool(WalkHash, isWalking);
                _animator.SetBool(RunHash, _isSprinting && !isJumping);
            }
        }

        private float CalculateLocomotionBlendSpeed(float speed)
        {
            if (speed <= _walkThreshold)
                return 0f;

            if (!_isSprinting)
                return Mathf.InverseLerp(
                    _walkThreshold,
                    Mathf.Max(_walkBlendMaxSpeed, _walkThreshold),
                    speed
                );

            float maxRunSpeed = Mathf.Max(_runBlendMaxSpeed, _walkBlendMaxSpeed);
            return Mathf.Lerp(0f, 2f, Mathf.InverseLerp(0f, maxRunSpeed, speed));
        }

        [Rpc(SendTo.NotOwner)]
        private void SyncJumpStateRpc(bool isJumping)
        {
            _isJumpingPrevio = isJumping;
        }

        [Rpc(SendTo.NotOwner)]
        private void SyncAimStateRpc(bool aiming)
        {
            _aimState = aiming;
            ApplyAimState(aiming);
        }

        [Rpc(SendTo.NotOwner)]
        private void SyncSprintStateRpc(bool sprinting)
        {
            _isSprinting = sprinting;
        }
    }
}
