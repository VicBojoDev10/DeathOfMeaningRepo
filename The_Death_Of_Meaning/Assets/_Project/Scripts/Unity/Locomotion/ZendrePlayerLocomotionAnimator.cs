using TDOM.Gameplay.Core;
using TDOM.Unity.Player;
using Unity.Netcode;
using UnityEngine;

namespace TDOM.Unity
{
    public class ZendrePlayerLocomotionAnimator : NetworkBehaviour
    {
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int AimHash = Animator.StringToHash("Aim");
        private static readonly int GardHash = Animator.StringToHash("Gard");
        private static readonly int MoveXHash = Animator.StringToHash("MoveX");
        private static readonly int MoveYHash = Animator.StringToHash("MoveY");
        private static readonly int JumpHash = Animator.StringToHash("Jump");
        private static readonly int FallingHash = Animator.StringToHash("Falling");
        private static readonly int ShootHash = Animator.StringToHash("Shoot");
        private static readonly int AssaultHash = Animator.StringToHash("assault");
        private static readonly int AssaultStandStateHash = Animator.StringToHash("AssaultStand");
        private static readonly int DamageHash = Animator.StringToHash("Damage");
        private static readonly int DamageStateHash = Animator.StringToHash("Base Layer.Damage");
        private static readonly int DeadHash = Animator.StringToHash("Dead");
        private static readonly int ReviveHash = Animator.StringToHash("Revive");
        private static readonly int ReviveStateHash = Animator.StringToHash("Revive");
        private const float DamageBlendMaximum = 50f;
        private const float DamageTransitionDuration = 0.1f;
        private const float SpeedDampTime = 0.1f;

        [Header("Referencias")]
        [SerializeField]
        private Animator _animator;

        private readonly NetworkVariable<bool> _aiming = new(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner
        );
        private readonly NetworkVariable<Vector2> _aimMove = new(
            Vector2.zero,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner
        );
        private readonly NetworkVariable<bool> _guarding = new(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner
        );
        private readonly NetworkVariable<bool> _jumping = new(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner
        );
        private readonly NetworkVariable<bool> _falling = new(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner
        );
        private readonly NetworkVariable<bool> _assault = new(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner
        );

        private Vector3 _ultimaPos;
        private bool _assaultAutoReleasePending;
        private PlayerRoot _playerRoot;

        private void Awake()
        {
            if (_animator == null)
                _animator = GetComponentInChildren<Animator>();
        }

        public override void OnNetworkSpawn()
        {
            _ultimaPos = transform.position;
            _playerRoot = GetComponentInParent<PlayerRoot>();
            if (_playerRoot != null)
            {
                _playerRoot.Vida.OnValueChanged += AlCambiarVida;
                _playerRoot.EstadoVida.OnValueChanged += AlCambiarEstadoVida;
            }

            if (_animator != null)
            {
                _animator.SetFloat(SpeedHash, 0f);
                _animator.SetFloat(DamageHash, 0f);
                if (_playerRoot != null)
                    AplicarEstadoVida(_playerRoot.EstadoVida.Value, false);
                AplicarModos(false, false, Vector2.zero);
                _animator.SetBool(JumpHash, _jumping.Value);
                _animator.SetBool(FallingHash, _falling.Value);
                _animator.SetBool(AssaultHash, _assault.Value);
            }
        }

        public override void OnNetworkDespawn()
        {
            if (_playerRoot != null)
            {
                _playerRoot.Vida.OnValueChanged -= AlCambiarVida;
                _playerRoot.EstadoVida.OnValueChanged -= AlCambiarEstadoVida;
            }

            _playerRoot = null;
            base.OnNetworkDespawn();
        }

        private void AlCambiarVida(float vidaAnterior, float vidaActual)
        {
            if (_animator == null)
                return;

            float danioRecibido = vidaAnterior - vidaActual;
            if (danioRecibido <= 0f)
                return;

            _animator.SetFloat(DamageHash, Mathf.Clamp(danioRecibido, 0f, DamageBlendMaximum));
            _animator.CrossFade(DamageStateHash, DamageTransitionDuration, 0, 0f);
        }

        private void AlCambiarEstadoVida(
            PlayerLifeState estadoAnterior,
            PlayerLifeState estadoActual
        )
        {
            bool iniciarRevive =
                estadoAnterior != PlayerLifeState.Vivo && estadoActual == PlayerLifeState.Vivo;
            AplicarEstadoVida(estadoActual, iniciarRevive);
        }

        private void AplicarEstadoVida(PlayerLifeState estado, bool iniciarRevive)
        {
            if (_animator == null)
                return;

            _animator.SetBool(DeadHash, estado != PlayerLifeState.Vivo);
            _animator.SetBool(ReviveHash, iniciarRevive);
        }

        public void ActualizarApuntado(bool apuntando, bool guardia, Vector2 movimiento)
        {
            if (!IsOwner)
                return;

            bool usarBlendDireccional = apuntando || guardia;
            Vector2 movimientoDireccional = usarBlendDireccional
                ? Vector2.ClampMagnitude(movimiento, 1f)
                : Vector2.zero;

            _aiming.Value = apuntando;
            _guarding.Value = guardia;
            _aimMove.Value = movimientoDireccional;
            AplicarModos(apuntando, guardia, movimientoDireccional);
        }

        public void ActualizarSalto(bool saltando, bool cayendo)
        {
            if (!IsOwner)
                return;

            if (_jumping.Value != saltando)
                _jumping.Value = saltando;
            if (_falling.Value != cayendo)
                _falling.Value = cayendo;
            if (saltando && _animator != null)
                _animator.SetBool(ShootHash, false);
            AplicarSalto(saltando, cayendo);
        }

        public void IniciarAssault()
        {
            if (!IsOwner)
                return;

            if (_assault.Value)
                return;

            _assault.Value = true;
            _assaultAutoReleasePending = true;
            if (_animator != null)
                _animator.SetBool(AssaultHash, _assault.Value);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || _animator == null)
                return;

            if (!IsOwner)
            {
                AplicarModos(_aiming.Value, _guarding.Value, _aimMove.Value);
                AplicarSalto(_jumping.Value, _falling.Value);
                _animator.SetBool(AssaultHash, _assault.Value);
            }
            else if (_assaultAutoReleasePending && LlegoAAssaultStand())
            {
                _assault.Value = false;
                _assaultAutoReleasePending = false;
                _animator.SetBool(AssaultHash, false);
            }

            if (_animator.GetCurrentAnimatorStateInfo(0).shortNameHash == ReviveStateHash)
                _animator.SetBool(ReviveHash, false);

            Vector3 posicionActual = transform.position;
            Vector3 velocidad = (posicionActual - _ultimaPos) / dt;
            velocidad.y = 0f;
            _ultimaPos = posicionActual;

            _animator.SetFloat(SpeedHash, velocidad.magnitude, SpeedDampTime, dt);
        }

        private bool LlegoAAssaultStand()
        {
            if (_animator.layerCount == 0)
                return false;

            if (_animator.GetCurrentAnimatorStateInfo(0).shortNameHash == AssaultStandStateHash)
                return true;

            return _animator.IsInTransition(0)
                && _animator.GetNextAnimatorStateInfo(0).shortNameHash == AssaultStandStateHash;
        }

        private void AplicarModos(bool apuntando, bool guardia, Vector2 movimiento)
        {
            if (_animator == null)
                return;

            _animator.SetBool(AimHash, apuntando);
            _animator.SetBool(GardHash, guardia);
            _animator.SetFloat(MoveXHash, movimiento.x);
            _animator.SetFloat(MoveYHash, movimiento.y);
        }

        private void AplicarSalto(bool saltando, bool cayendo)
        {
            if (_animator == null)
                return;

            _animator.SetBool(JumpHash, saltando);
            _animator.SetBool(FallingHash, cayendo);
            if (saltando)
                _animator.SetBool(ShootHash, false);
        }
    }
}
