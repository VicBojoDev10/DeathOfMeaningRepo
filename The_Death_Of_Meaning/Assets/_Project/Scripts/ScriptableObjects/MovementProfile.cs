using UnityEditor.Embree;
using UnityEngine;

namespace TDOM.Contracts
{
    [CreateAssetMenu(fileName = "NewMovementProfile", menuName = "TDOM/Movement Profile")]
    public class MovementProfile : ScriptableObject
    {
        [Header("Gravity")]
        public float gravity = -28f;
        public float terminalVelocity = -50f;
        public float lowJumperMultiplier = 2.0f;

        [Header("Movement")]
        public float jumpVelocity = 15f;
        public int maxJumps = 1;
        public float coyoteTime = 0.12f;
        public float bufferTime = 0.15f;

        [Header("Sprint")]
        public float baseSpeed = 10f;
        public float sprintSpeed = 20f;
        public float acceleration = 15f;
        public float friction = 5f;
        public float airControl = 2.5f;
    }
}
