using Unity.Netcode;
using UnityEngine;

namespace TDOM.Unity
{
    public class AylaPlayerLocomotionAnimator : NetworkBehaviour
    {
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int MoveXHash = Animator.StringToHash("MoveX");
        private static readonly int MoveYHash = Animator.StringToHash("MoveY");
        private static readonly int AimStateHash = Animator.StringToHash("AimState");
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
        private float _runThreshold = 14.0f; // Punto medio seguro: caminar es 10 m/s y sprint es 20 m/s

        [SerializeField]
        private float _runBlendMaxSpeed = 20.0f;
        private Vector3 _ultimaPos;
        private float _rawSpeed;
        private float _smoothSpeed;
        private float _tiempoEnAire;
        private bool _isJumpingPrevio;
        private bool _aimState;

        private void Awake()
        {
            if (_animator == null)
                _animator = GetComponentInChildren<Animator>();
            if (_characterController == null)
                _characterController = GetComponent<CharacterController>();
        }

        /// <summary>Actualiza el blend tree de apuntado desde el input del jugador dueño.</summary>
        public void SetAimState(bool aiming)
        {
            if (!IsSpawned || !IsOwner || aiming == _aimState)
                return;

            _aimState = aiming;
            ApplyAimState(aiming);
            SyncAimStateRpc(aiming);
        }

        private void ApplyAimState(bool aiming)
        {
            if (_animator != null)
                _animator.SetBool(AimStateHash, aiming);
        }

        public override void OnNetworkSpawn()
        {
            _ultimaPos = transform.position;
            _smoothSpeed = 0f;
            _tiempoEnAire = 0f;
            _aimState = false;
            ApplyAimState(false);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f)
                return;
            // Cálculo de velocidad horizontal y vertical
            Vector3 pos = transform.position;
            Vector3 v = (pos - _ultimaPos) / dt;
            float velY = v.y;
            v.y = 0f;
            _rawSpeed = v.magnitude;
            _ultimaPos = pos;

            // Suavizado de velocidad para evitar micro-caídas y picos ruidosos de frames
            _smoothSpeed = Mathf.Lerp(_smoothSpeed, _rawSpeed, dt * 12f);

            // 1. Detección robusta de salto (filtra micro-pérdidas de contacto del CharacterController)
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

                // Salto real: impulso ascendente real (velY > 1.5) o caída sostenida en el aire (> 0.10s)
                isJumping = velY > 1.5f || _tiempoEnAire > 0.10f;

                // Si cambió el estado de salto, sincronizamos a los demás clientes
                if (isJumping != _isJumpingPrevio)
                {
                    _isJumpingPrevio = isJumping;
                    SyncJumpStateRpc(isJumping);
                }
            }
            // 2. Detección de suelo: caminar, correr o idle
            bool isRunning = !isJumping && _smoothSpeed >= _runThreshold;
            bool isWalking = !isJumping && _smoothSpeed >= _walkThreshold && !isRunning;
            // 3. Aplicar al Animator
            if (_animator != null)
            {
                Vector3 localVelocity = transform.InverseTransformDirection(v);
                _animator.SetFloat(SpeedHash, CalculateLocomotionBlendSpeed(_smoothSpeed));
                _animator.SetFloat(MoveXHash, localVelocity.x);
                _animator.SetFloat(MoveYHash, localVelocity.z);
                _animator.SetBool(JumpHash, isJumping);
                _animator.SetBool(WalkHash, isWalking);
                _animator.SetBool(RunHash, isRunning);
            }
        }

        private float CalculateLocomotionBlendSpeed(float speed)
        {
            if (speed <= _walkThreshold)
                return 0f;

            if (speed < _runThreshold)
                return Mathf.Lerp(0f, 1f, Mathf.InverseLerp(_walkThreshold, _runThreshold, speed));

            float maxRunSpeed = Mathf.Max(_runBlendMaxSpeed, _runThreshold);
            return Mathf.Lerp(1f, 2f, Mathf.InverseLerp(_runThreshold, maxRunSpeed, speed));
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
    }
}
