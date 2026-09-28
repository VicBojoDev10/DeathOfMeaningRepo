using UnityEngine;

namespace TDOM.Data
{
    [CreateAssetMenu(menuName = "TDOM/Character Definition")]
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
}
