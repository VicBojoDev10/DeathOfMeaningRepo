using TDOM.Contracts;
using TDOM.Unity.Level;
using UnityEngine;

namespace TDOM.Unity.Locomotion
{
    public class PlayerMotor : MonoBehaviour
    {
        [SerializeField]
        private CharacterController _cc;

        private PlataformaCircular _plataformaActual;

        public void Apply(MotionIntent intent, float dt)
        {
            Vector3 deltaPlataforma = _plataformaActual != null
                ? _plataformaActual.FrameDelta
                : Vector3.zero;
            _cc.Move(intent.Velocity * dt + deltaPlataforma);
            if (!_cc.isGrounded)
                _plataformaActual = null;
        }

        public void ProbeGround(LocomotionState estado)
        {
            estado.IsGrounded = _cc.isGrounded;
            if (!estado.IsGrounded)
            {
                _plataformaActual = null;
                return;
            }

            if (Physics.Raycast(_cc.bounds.center, Vector3.down, out RaycastHit hit,
                    _cc.bounds.extents.y + 0.15f, Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore))
                _plataformaActual = hit.collider.GetComponentInParent<PlataformaCircular>();
            else
                _plataformaActual = null;
        }

        public void ResetPlataforma() => _plataformaActual = null;
    }
}
