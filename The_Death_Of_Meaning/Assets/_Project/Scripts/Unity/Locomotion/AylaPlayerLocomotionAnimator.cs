using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

namespace TDOM.Unity
{
    public class AylaPlayerLocomotionAnimator : NetworkBehaviour
    {
        // 1. Hashes correspondientes a los nombres exactos en tu Animator Controller
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
        private Vector3 _ultimaPos;
        private float _rawSpeed;
        private float _smoothSpeed;
        private float _tiempoEnAire;
        private bool _isJumpingPrevio;
        private void Awake()
        {
            if (_animator == null)
                _animator = GetComponentInChildren<Animator>();
            if (_characterController == null)
                _characterController = GetComponent<CharacterController>();
        }
        public override void OnNetworkSpawn()
        {
            _ultimaPos = transform.position;
            _smoothSpeed = 0f;
            _tiempoEnAire = 0f;
            // Ocultar sombras del dueño local para la cámara en primera persona / feedback
            if (IsOwner)
            {
                foreach (var r in GetComponentsInChildren<Renderer>(true))
                {
                    r.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                }
            }
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
                _animator.SetBool(JumpHash, isJumping);
                _animator.SetBool(WalkHash, isWalking);
                _animator.SetBool(RunHash, isRunning);
            }
        }
        [Rpc(SendTo.NotOwner)]
        private void SyncJumpStateRpc(bool isJumping)
        {
            _isJumpingPrevio = isJumping;
        }
    }


}
