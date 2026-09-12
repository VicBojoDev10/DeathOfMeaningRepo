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
