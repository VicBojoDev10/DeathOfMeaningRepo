using UnityEngine;

namespace TDOM.Data.Unity
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
