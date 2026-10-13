using UnityEngine;

namespace TDOM.Data
{
    [CreateAssetMenu(
        fileName = "ThirdPersonCameraConfig",
        menuName = "TDOM/Third Person Camera Config"
    )]
    public sealed class ThirdPersonCameraConfig : ScriptableObject
    {
        [Header("Pivot")]
        public float PivotHeight = 1.6f;

        [Header("Encuadre libre")]
        public float Distance = 4f;
        public float ShoulderOffsetX = 0.5f;

        [Header("Encuadre apuntando (L2)")]
        public float AimDistance = 2f;
        public float AimShoulderOffsetX = 0.3f;
        public float ZoomSpeed = 12f;

        [Header("Pitch")]
        public float PitchMin = -40f;
        public float PitchMax = 70f;

        [Header("Apuntado")]
        public float AimMaxRange = 200f;
    }
}
