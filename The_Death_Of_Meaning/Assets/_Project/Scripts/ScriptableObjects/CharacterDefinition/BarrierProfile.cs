using UnityEngine;

namespace TDOM.Data
{
    [CreateAssetMenu(menuName = "TDOM/Character Definition/Barrier Profile")]
    public sealed class BarrierProfile : ScriptableObject
    {
        [Header("Defensa y energía")]
        [Tooltip("Reducción para ataques de jefe no especificados más abajo.")]
        [Range(0f, 1f)]
        public float BossDamageReduction = 0.65f;

        [Tooltip(
            "Daño que recibe el jugador si la barrera bloquea la mano. 0 absorbe todo el golpe."
        )]
        [Min(0f)]
        public float HandDamageWhileShielding = 0f;

        [Tooltip(
            "Daño que recibe el jugador si la barrera bloquea el tentáculo. 0 absorbe todo el golpe."
        )]
        [Min(0f)]
        public float TentacleDamageWhileShielding = 0f;

        [Tooltip("Daño que atraviesa la barrera durante el rayo, en puntos de vida por impacto.")]
        [Min(0f)]
        public float BeamDamageWhileShielding = 20f;

        [Tooltip(
            "Daño que recibe el jugador si la barrera bloquea el mortero. Ajústalo por debajo de la vida para poder absorberlo."
        )]
        [Min(0f)]
        public float MortarDamageWhileShielding = 9999f;

        [Tooltip("Porcentaje de la energía máxima que cuesta bloquear un golpe de mano.")]
        [Range(0f, 100f)]
        public float HandStaminaCostPercent = 30f;

        [Tooltip("Porcentaje de la energía máxima que cuesta bloquear un golpe de tentáculo.")]
        [Range(0f, 100f)]
        public float TentacleStaminaCostPercent = 15f;

        [Tooltip(
            "Porcentaje de la energía máxima que cuesta bloquear cada golpe o proyectil de minion."
        )]
        [Range(0f, 100f)]
        public float MinionStaminaCostPercent = 0.15f;

        [Tooltip("Separación visible que mantienen los minions frente al plano de la barrera.")]
        [Min(0f)]
        public float MinionStandOffDistance = 0.7f;

        [Tooltip(
            "Costo adicional por cada punto de daño interceptado por rayo y otros ataques de jefe."
        )]
        [Min(0f)]
        public float BossEnergyPerDamageIntercepted = 0.2f;

        [Range(0f, 180f)]
        public float FrontArcDegrees = 140f;

        [Range(0f, 1f)]
        public float MoveSpeedMultiplier = 0.5f;

        [Header("Protección del aliado")]
        [Min(0f)]
        public float AllyProtectionDepth = 3.5f;

        [Min(0f)]
        public float AllyProtectionHalfWidth = 1.5f;

        [Header("Cámara")]
        [Min(0f)]
        public float CameraDistanceOffset = 0.85f;

        [Header("Visual de barrera")]
        public Material VisualMaterial;
        public Color VisualColor = new Color(0.32f, 0.82f, 1f, 0.48f);

        [Min(0f)]
        public float VisualIntensity = 1.2f;

        [Min(0f)]
        public float VisualPulseAmplitude = 0.18f;

        [Min(0f)]
        public float VisualPulseSpeed = 2.2f;

        [Min(0.1f)]
        public float VisualRadius = 1.15f;

        [Min(0.1f)]
        public float VisualHeight = 2.65f;

        [Min(0f)]
        public float VisualBaseHeight = 0.12f;

        [Min(0f)]
        public float VisualForwardOffset = 1.15f;

        [Min(0f)]
        public float VisualCurveDepth = 0.28f;
    }
}
