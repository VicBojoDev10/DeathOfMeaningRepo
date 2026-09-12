using TDOM.Contracts;
using UnityEngine;

namespace TDOM.Unity.Locomotion
{
    public class PlayerMotor : MonoBehaviour
    {
        [SerializeField]
        private CharacterController _cc;

        public void Apply(MotionIntent intent, float dt)
        {
            _cc.Move(intent.Velocity * dt);
        }

        public void ProbeGround(LocomotionState estado)
        {
            estado.IsGrounded = _cc.isGrounded;
        }
    }
}
