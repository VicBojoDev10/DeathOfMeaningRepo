using TDOM.Unity.Level;
using TDOM.Unity.Locomotion;
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
        private static readonly int DobleSaltoHash = Animator.StringToHash("DobleSalto");
        private static readonly int GanchoHash = Animator.StringToHash("Gancho");
        private static readonly int DashHash = Animator.StringToHash("Dash");
        private static readonly int DobleSaltoTiempoHash = Animator.StringToHash(
            "DobleSaltoTiempo"
        );

        [Header("Referencias")]
        [SerializeField]
        private Animator _animator;

        [SerializeField]
        private CharacterController _characterController;

        private PlayerMotor _playerMotor;

        [Header("Umbrales de Velocidad (m/s)")]
        [SerializeField]
        private float _walkThreshold = 0.5f;

        [SerializeField]
        private float _walkBlendMaxSpeed = 10.0f;

        [SerializeField]
        private float _runBlendMaxSpeed = 20.0f;

        [Header("Doble salto")]
        [SerializeField, Range(0.1f, 3f)]
        [Tooltip("Velocidad de la animacion (0.5 = 50% mas lenta). Se reproduce al reves.")]
        private float _velocidadDobleSalto = 0.5f;

        [SerializeField, Min(0.05f)]
        [Tooltip(
            "Duracion del clip del doble salto en segundos (frames 1 a 10 = 9 frames a 24 fps)."
        )]
        private float _duracionClipDobleSalto = 9f / 24f;

        [Header("Dash")]
        [SerializeField, Range(0f, 80f)]
        [Tooltip("Cuanto se inclina el modelo hacia adelante durante el dash (grados).")]
        private float _inclinacionDash = 45f;

        [SerializeField, Min(0.01f)]
        [Tooltip("Segundos que tarda en inclinarse al empezar el dash.")]
        private float _tiempoInclinar = 0.06f;

        [SerializeField, Min(0.01f)]
        [Tooltip("Segundos que tarda en enderezarse al terminar el dash.")]
        private float _tiempoEnderezar = 0.2f;

        [SerializeField, Min(0f)]
        [Tooltip(
            "Altura (metros desde los pies) alrededor de la que gira el modelo al inclinarse. "
                + "Cerca de la cadera para que no se separe del suelo."
        )]
        private float _alturaPivoteInclinacion = 0.8f;

        [SerializeField, Min(0f)]
        [Tooltip(
            "Segundos que la pose del dash sigue puesta tras acabar el desplazamiento "
                + "(el dash dura muy poco y asi se alcanza a ver)."
        )]
        private float _retencionDash = 0.08f;

        private Vector3 _ultimaPos;
        private float _rawSpeed;
        private float _smoothSpeed;
        private float _tiempoEnAire;
        private bool _isJumpingPrevio;
        private bool _aimState;
        private bool _isSprinting;
        private bool _ganchoAnimando;
        private bool _dashAnimando;
        private float _dashHasta;
        private float _inclinacionActual;
        private Vector3 _posLocalBase;
        private Quaternion _rotLocalBase = Quaternion.identity;
        private float _dobleSaltoTiempo;

        private void Awake()
        {
            if (_animator == null)
                _animator = GetComponentInChildren<Animator>();
            if (_characterController == null)
                _characterController = GetComponent<CharacterController>();
            _playerMotor = GetComponentInParent<PlayerMotor>();
            if (_animator != null)
            {
                _posLocalBase = _animator.transform.localPosition;
                _rotLocalBase = _animator.transform.localRotation;
            }
        }

        public void SetAimState(bool aiming)
        {
            if (!IsSpawned || !IsOwner || aiming == _aimState)
                return;

            _aimState = aiming;
            ApplyAimState(aiming);
            SyncAimStateRpc(aiming);
        }

        /// <summary>
        /// Lanza la animacion del doble salto. Solo el dueno la llama; el resto de
        /// clientes la reciben por RPC para que el companero tambien la vea.
        /// </summary>
        public void IniciarDobleSalto()
        {
            if (!IsSpawned || !IsOwner)
                return;

            AplicarDobleSalto();
            DobleSaltoRpc();
        }

        private void AplicarDobleSalto()
        {
            // El estado DobleSalto lee su tiempo de este parametro: arranca en 1 (final del
            // clip) y baja a 0 en Update, asi se reproduce de atras hacia adelante.
            _dobleSaltoTiempo = 1f;
            if (_animator == null)
                return;

            _animator.SetFloat(DobleSaltoTiempoHash, _dobleSaltoTiempo);
            _animator.SetTrigger(DobleSaltoHash);
        }

        private void ActualizarDobleSalto(float dt)
        {
            if (_dobleSaltoTiempo <= 0f || _animator == null)
                return;

            float duracion = _duracionClipDobleSalto / Mathf.Max(0.01f, _velocidadDobleSalto);
            _dobleSaltoTiempo = Mathf.Max(0f, _dobleSaltoTiempo - dt / duracion);
            _animator.SetFloat(DobleSaltoTiempoHash, _dobleSaltoTiempo);
        }

        /// <summary>
        /// Activa la pose del gancho (frame 24 del clip y luego bucle 28-29) mientras dura
        /// el jalon. Solo el dueno la llama; el resto de clientes la reciben por RPC.
        /// </summary>
        public void SetGanchoState(bool activo)
        {
            if (!IsSpawned || !IsOwner || activo == _ganchoAnimando)
                return;

            _ganchoAnimando = activo;
            ApplyGanchoState(activo);
            SyncGanchoStateRpc(activo);
        }

        private void ApplyGanchoState(bool activo)
        {
            if (_animator != null)
                _animator.SetBool(GanchoHash, activo);
        }

        /// <summary>
        /// Activa la pose del dash (frames 0 a 8 del salto, y se queda en el 8) mientras dura
        /// el desplazamiento. Se llama cada frame; la pose se mantiene un instante mas
        /// (_retencionDash). Solo el dueno la llama; el resto de clientes la reciben por RPC.
        /// </summary>
        public void SetDashState(bool activo)
        {
            if (!IsSpawned || !IsOwner)
                return;

            if (activo)
                _dashHasta = Time.time + _retencionDash;

            bool efectivo = Time.time < _dashHasta;
            if (efectivo == _dashAnimando)
                return;

            _dashAnimando = efectivo;
            ApplyDashState(efectivo);
            SyncDashStateRpc(efectivo);
        }

        private void ApplyDashState(bool activo)
        {
            if (_animator != null)
                _animator.SetBool(DashHash, activo);
        }

        // Inclina el modelo hacia adelante mientras el Animator esta en el dash, girando
        // alrededor de la cadera. Lee el parametro del Animator, que existe en todos los
        // clientes.
        private void LateUpdate()
        {
            if (_animator == null || _inclinacionDash <= 0f)
                return;

            Transform modelo = _animator.transform;
            bool dash = _animator.GetBool(DashHash);
            float objetivo = dash ? _inclinacionDash : 0f;
            float tiempo = dash ? _tiempoInclinar : _tiempoEnderezar;
            _inclinacionActual = Mathf.MoveTowards(
                _inclinacionActual,
                objetivo,
                _inclinacionDash * Time.deltaTime / tiempo
            );

            if (_inclinacionActual < 0.01f)
            {
                if (dash)
                    return;
                _inclinacionActual = 0f;
                modelo.SetLocalPositionAndRotation(_posLocalBase, _rotLocalBase);
                return;
            }

            Quaternion giro = Quaternion.AngleAxis(_inclinacionActual, Vector3.right);
            Vector3 pivote = Vector3.up * _alturaPivoteInclinacion;
            modelo.SetLocalPositionAndRotation(
                pivote + (giro * (_posLocalBase - pivote)),
                giro * _rotLocalBase
            );
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
            _ganchoAnimando = false;
            _dashAnimando = false;
            _dashHasta = 0f;
            _dobleSaltoTiempo = 0f;
            ApplyAimState(false);
            ApplyGanchoState(false);
            ApplyDashState(false);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f)
                return;

            Vector3 pos = transform.position;
            Vector3 desplazamiento = pos - _ultimaPos;
            PlataformaCircular plataforma =
                _playerMotor != null ? _playerMotor.PlataformaActual : null;
            if (plataforma != null)
                desplazamiento -= plataforma.FrameDelta;

            Vector3 v = desplazamiento / dt;
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

            // El trigger no se queda "armado" si el salto termina sin consumirlo
            // (por ejemplo, si se estaba atacando en el aire).
            if (!isJumping && _animator != null)
            {
                _animator.ResetTrigger(DobleSaltoHash);
                _dobleSaltoTiempo = 0f;
                _animator.SetFloat(DobleSaltoTiempoHash, 0f);
            }
            ActualizarDobleSalto(dt);

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
        private void DobleSaltoRpc()
        {
            AplicarDobleSalto();
        }

        [Rpc(SendTo.NotOwner)]
        private void SyncGanchoStateRpc(bool activo)
        {
            _ganchoAnimando = activo;
            ApplyGanchoState(activo);
        }

        [Rpc(SendTo.NotOwner)]
        private void SyncDashStateRpc(bool activo)
        {
            _dashAnimando = activo;
            ApplyDashState(activo);
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
