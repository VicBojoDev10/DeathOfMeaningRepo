using UnityEngine;

namespace TDOM.Data
{
    [CreateAssetMenu(fileName = "NewMovementProfile", menuName = "TDOM/Movement Profile")]
    public sealed class MovementProfile : ScriptableObject
    {
        [Header("Gravity")]
        public float Gravity = -28f;
        public float TerminalVelocity = -50f;
        public float LowJumpMultiplier = 2.0f;

        [Header("Movement")]
        public float JumpVelocity = 15f;
        public int MaxJumps = 1;
        public float CoyoteTime = 0.12f;
        public float BufferTime = 0.15f;

        [Header("Sprint")]
        public float BaseSpeed = 10f;
        public float SprintSpeed = 20f;
        public float Acceleration = 15f;
        public float Friction = 5f;
        public float AirControl = 2.5f;
    }
}
