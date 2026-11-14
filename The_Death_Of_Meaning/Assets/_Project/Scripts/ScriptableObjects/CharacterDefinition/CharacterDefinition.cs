using UnityEngine;

namespace TDOM.Data
{
    [CreateAssetMenu(menuName = "TDOM/Character Definition")]
    public sealed class CharacterDefinition : ScriptableObject
    {
        public string DisplayName;
        public MovementProfile Movement;
        public bool CanClimb;
        public DashProfile Dash;
        public ComboProfile Melee;
        public ComboProfile Ranged;
        public GrappleProfile Grapple;
        public EnergyProfile Energy;
        [Header("Barrera Zendre")]
        [Range(0f, 1f)] public float BarrierBossDamageReduction = 0.65f;
        [Min(0f)] public float BarrierBossEnergyPerDamage = 0.2f;
        [Min(0f)] public float BarrierMinionHitEnergyCost = 8f;
        [Range(0f, 180f)] public float BarrierFrontArc = 110f;
        [Range(0f, 1f)] public float BarrierMoveSpeedMultiplier = 0.5f;
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
