using UnityEngine;

namespace TDOM.Contracts
{
    public class LocomotionState
    {
        public bool IsGrounded;
        public Vector3 Velocity;
        public int JumpsUsed;
        public float CoyoteTimer;
        public float BufferTimer;
    }
}
