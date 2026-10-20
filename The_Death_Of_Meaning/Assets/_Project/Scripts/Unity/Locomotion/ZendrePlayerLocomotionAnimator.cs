using Unity.Netcode;
using UnityEngine;

namespace TDOM.Unity
{
    public class ZendrePlayerLocomotionAnimator : NetworkBehaviour
    {
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private const float SpeedDampTime = 0.1f;

        [Header("Referencias")]
        [SerializeField]
        private Animator _animator;

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
                _animator.SetFloat(SpeedHash, 0f);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || _animator == null)
                return;

            Vector3 posicionActual = transform.position;
            Vector3 velocidad = (posicionActual - _ultimaPos) / dt;
            velocidad.y = 0f;
            _ultimaPos = posicionActual;

            _animator.SetFloat(SpeedHash, velocidad.magnitude, SpeedDampTime, dt);
        }
    }
}
