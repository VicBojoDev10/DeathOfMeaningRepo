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
        public float MaxHealth = 100f;
        public float DownedDuration = 15f;
        public float ReviveHealth = 50f;
        public float ReviveDuration = 3f;
        public float ReviveRadius = 3.5f;

        public float VidaMaxima => MaxHealth;
        public float TiempoAbatido => DownedDuration;
        public float VidaAlRevivir => ReviveHealth;
        public float TiempoParaRevivir => ReviveDuration;
        public float RadioRevivir => ReviveRadius;
    }
}
