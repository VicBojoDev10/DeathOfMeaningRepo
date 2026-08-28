/*using UnityEngine;

public sealed class PlayerMotor : MonoBehaviour
{
    [SerializeField] private CharacterController _cc;

    public void Apply(MotionIntent intent, float dt)
    {
        _cc.Move(intent.Velocity * dt);
    }

    public void ProbeGround(LocomotionState state)
    {
        state.IsGrounded = _cc.isGrounded;
    }
}*/
