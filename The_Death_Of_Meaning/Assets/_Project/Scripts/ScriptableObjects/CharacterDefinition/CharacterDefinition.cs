using TDOM.Contracts;
using UnityEngine;

namespace TDOM.Data
{
    [CreateAssetMenu(menuName = "tdom/Character Definition")]
    public sealed class CharacterDefinition : ScriptableObject
    {
        public string DisplayName;
        public MovementProfile Movement;
        public DashProfile Dash;
        public ComboProfile Melee;
        public ComboProfile Ranged;
        public GrappleProfile Grapple;
        public EnergyProfile Energy;
    }

    [CreateAssetMenu(menuName = "tdom/Movement Profile")]
    public sealed class MovementProfile : ScriptableObject
    {
        public float Gravity,
            TerminalVelocity,
            LowJumpMultiplier,
            JumpVelocity;
        public int MaxJumps;
        public float CoyoteTime,
            BufferTime;
        public float BaseSpeed,
            SprintSpeed,
            Acceleration,
            Friction,
            AirControl;
    }

    [CreateAssetMenu(menuName = "tdom/Dash Profile")]
    public sealed class DashProfile : ScriptableObject
    {
        public float Distance,
            Duration,
            Cooldown,
            MaxTurnRate;
        public AnimationCurve Easing = AnimationCurve.Linear(0, 0, 1, 1);
    }

    [CreateAssetMenu(menuName = "tdom/Combo Profile")]
    public sealed class ComboProfile : ScriptableObject
    {
        public AttackStep[] Steps;
        public AttackStep Charged;
        public float HoldThreshold,
            MaxChargeTime,
            BufferWindow;
    }

    [CreateAssetMenu(menuName = "tdom/Grapple Profile")]
    public sealed class GrappleProfile : ScriptableObject
    {
        public float Range;
        public float PullSpeed;
    }

    [CreateAssetMenu(menuName = "tdom/Energy Profile")]
    public sealed class EnergyProfile : ScriptableObject
    {
        public float Max;
        public float RegenPerSecond;
        public float ChargedCost;
    }
}
