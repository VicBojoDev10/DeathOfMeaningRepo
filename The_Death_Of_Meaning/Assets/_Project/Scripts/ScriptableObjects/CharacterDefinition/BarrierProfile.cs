using UnityEngine;

namespace TDOM.Data
{
    [CreateAssetMenu(menuName = "TDOM/Character Definition/Barrier Profile")]
    public sealed class BarrierProfile : ScriptableObject
    {
        [Header("Defensa y energía")]
        [Range(0f, 1f)] public float BossDamageReduction = 0.65f;
        [Min(0f)] public float BossEnergyPerDamageIntercepted = 0.2f;
        [Min(0f)] public float MinionHitEnergyCost = 8f;
        [Range(0f, 180f)] public float FrontArcDegrees = 140f;
        [Range(0f, 1f)] public float MoveSpeedMultiplier = 0.5f;

        [Header("Protección del aliado")]
        [Min(0f)] public float AllyProtectionDepth = 3.5f;
        [Min(0f)] public float AllyProtectionHalfWidth = 1.5f;

        [Header("Cámara")]
        [Min(0f)] public float CameraDistanceOffset = 0.85f;

        [Header("Visual de barrera")]
        public Material VisualMaterial;
        public Color VisualColor = new Color(0.32f, 0.82f, 1f, 0.48f);
        [Min(0f)] public float VisualIntensity = 1.2f;
        [Min(0f)] public float VisualPulseAmplitude = 0.18f;
        [Min(0f)] public float VisualPulseSpeed = 2.2f;
        [Min(0.1f)] public float VisualRadius = 1.15f;
        [Min(0.1f)] public float VisualHeight = 2.65f;
        [Min(0f)] public float VisualBaseHeight = 0.12f;
        [Min(0f)] public float VisualForwardOffset = 1.15f;
        [Min(0f)] public float VisualCurveDepth = 0.28f;
    }
}
