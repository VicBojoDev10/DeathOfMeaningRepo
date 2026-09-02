using UnityEngine;
namespace TDOM.Contracts
{
    public readonly struct InputSnapshot
    {
        public readonly Vector2 Move;
        public readonly Vector2 Look;
        public readonly bool JumpHeld;
        public readonly bool DashPressed;
        public readonly bool SprintPressed;
        public readonly bool AttackPressed;
        public readonly bool AttackHeld;
        public readonly bool AttackReleased;
        public readonly bool AimHeld;
        public readonly bool FirePressed;
        public readonly bool FireHeld;
        public readonly bool FireReleased;
        public readonly bool GrapplePressed;
        public readonly bool JumpPressed;

        public InputSnapshot(
            Vector2 move,
            Vector2 look,
            bool jumpPressed,
            bool jumpHeld,
            bool dashPressed,
            bool sprintPressed,
            bool attackPressed,
            bool attackHeld,
            bool attackReleased,
            bool aimHeld,
            bool firePressed,
            bool fireHeld,
            bool fireReleased,
            bool grapplePressed
        )
        {
            Move = move;
            Look = look;
            JumpPressed = jumpPressed;
            JumpHeld = jumpHeld;
            DashPressed = dashPressed;
            SprintPressed = sprintPressed;
            AttackPressed = attackPressed;
            AttackHeld = attackHeld;
            AttackReleased = attackReleased;
            AimHeld = aimHeld;
            FirePressed = firePressed;
            FireHeld = fireHeld;
            FireReleased = fireReleased;
            GrapplePressed = grapplePressed;
        }
    }

    public readonly struct MotionIntent
    {
        public readonly Vector3 Velocity;
        public readonly bool IgnoreGravity;
        public readonly bool IgnoreCollision;

        public MotionIntent(
            Vector3 velocity,
            bool ignoreGravity = false,
            bool ignoreCollision = false
        )
        {
            Velocity = velocity;
            IgnoreGravity = ignoreGravity;
            IgnoreCollision = ignoreCollision;
        }
    }

    public sealed class LocomotionState
    {
        public Vector3 Velocity;
        public bool IsGrounded;
        public int JumpsUsed;
        public float CoyoteTimer;
        public float BufferTimer;
        public LocomotionPhase Phase;
    }

    public enum LocomotionPhase
    {
        Grounded,
        Airborne,
        Dashing,
        Grappling,
        Attacking,
    }

    [System.Serializable]
    public struct AttackStep
    {
        public float WindupTime;
        public float ActiveTime;
        public float RecoveryTime;
        public float ComboWindowTime;
        public float Damage;
        public string AnimTrigger;
    }
}
