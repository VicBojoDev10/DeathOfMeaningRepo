using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

namespace TDOM.Unity
{
    public class ZendrePlayerLocomotionAnimator : NetworkBehaviour
    {
        private static readonly int RunHash = Animator.StringToHash("run");

        [Header("Referencias")]
        [SerializeField]
        private Animator _animator;
        private static readonly int WalkHash = Animator.StringToHash("walk");

        [Header("Umbrales de Velocidad (m/s)")]
        [SerializeField]
        private float _walkThreshold = 0.5f;

        [SerializeField]
        private float _runThreshold = 8.5f;
        private Vector3 _ultimaPos;
        private float _rawSpeed;

        public override void OnNetworkSpawn()
        {
            _ultimaPos = transform.position;
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
            Vector3 pos = transform.position;
            Vector3 velocidad = (pos - _ultimaPos) / dt;
            velocidad.y = 0f;
            _rawSpeed = velocidad.magnitude;
            _ultimaPos = pos;
            bool isRunning = _rawSpeed >= _runThreshold;
            bool isWalking = _rawSpeed >= _walkThreshold && !isRunning;
            if (_animator != null)
            {
                _animator.SetBool(WalkHash, isWalking);
                _animator.SetBool(RunHash, isRunning);
            }
        }
    }
}
