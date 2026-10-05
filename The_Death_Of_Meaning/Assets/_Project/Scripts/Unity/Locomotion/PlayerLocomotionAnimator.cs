using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

namespace TDOM.Unity
{
    public class PlayerLocomotionAnimator : NetworkBehaviour
    {
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private const float DampTime = 0.1f;

        [SerializeField]
        private Animator _animator;
        private Vector3 _ultimaPos;

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
            Vector3 v = (pos - _ultimaPos) / dt;
            v.y = 0f;

            _animator.SetFloat(SpeedHash, v.magnitude, DampTime, dt);
            _ultimaPos = pos;
        }
    }
}
