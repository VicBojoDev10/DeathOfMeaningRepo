using UnityEngine;

namespace TDOM.Contracts
{
    public sealed class InputSnapshot
    {
        public Vector2 Move;
        public Vector2 Look;
        public bool JumpHeld;
        public bool DashPressed;
        public bool SprintPressed;
        public bool AttackPressed;
        public bool AttackHeld;
        public bool AttackReleased;
        public bool AimHeld;
        public bool FirePressed;
        public bool FireHeld;
        public bool FireReleased;
        public bool GrapplePressed;
        public bool JumpPressed;

        public InputSnapshot(bool jumpPressed, bool jumpHeld)
        {
            JumpPressed = jumpPressed;
            JumpHeld = jumpHeld;
        }
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

    public class LocomotionState
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
