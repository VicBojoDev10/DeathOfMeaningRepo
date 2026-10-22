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

        private Vector3 _ultimaPos;

        private void Awake()
        {
            if (_animator == null)
                _animator = GetComponentInChildren<Animator>();
        }

        public override void OnNetworkSpawn()
        {
            _ultimaPos = transform.position;
            if (_animator != null)
            {
                _animator.SetFloat(SpeedHash, 0f);
                AplicarModos(false, false, Vector2.zero);
                _animator.SetBool(JumpHash, _jumping.Value);
                _animator.SetBool(FallingHash, _falling.Value);
            }
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

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || _animator == null)
                return;

            if (!IsOwner)
            {
                AplicarModos(_aiming.Value, _guarding.Value, _aimMove.Value);
                AplicarSalto(_jumping.Value, _falling.Value);
            }

            Vector3 posicionActual = transform.position;
            Vector3 velocidad = (posicionActual - _ultimaPos) / dt;
            velocidad.y = 0f;
            _ultimaPos = posicionActual;

            _animator.SetFloat(SpeedHash, velocidad.magnitude, SpeedDampTime, dt);
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
