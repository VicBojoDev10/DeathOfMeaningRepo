using UnityEngine;
using UnityEngine.Serialization;

namespace TDOM.Data
{
    [CreateAssetMenu(
        fileName = "ThirdPersonCameraConfig",
        menuName = "TDOM/Third Person Camera Config"
    )]
    public sealed class ThirdPersonCameraConfig : ScriptableObject
    {
        [Header("Exploracion")]
        public CameraStanceValues Exploration = new CameraStanceValues(4f, 0.8f, 0.6f, 40f);

        [Header("Sprint")]
        public CameraStanceValues Sprint = new CameraStanceValues(5f, 0.8f, 0.5f, 50f);

        [Header("Combate")]
        public CameraStanceValues Combat = new CameraStanceValues(3.3f, 0.8f, 0.6f, 37f);

        [Header("Apuntado (L2)")]
        public CameraStanceValues Aim = new CameraStanceValues(2f, 0.5f, 0.6f, 27f);

        [Header("Transiciones")]
        [FormerlySerializedAs("ZoomSpeed")]
        public float TransitionSpeed = 12f;
        public float CombatLingerSeconds = 3f;

        [Header("Cambio de hombro por dash o carga lateral en combate")]
        public float ShoulderSwapSpeed = 8f;

        [Range(0f, 1f)]
        public float StrafeSwingThreshold = 0.5f;

        [Header("Pitch")]
        public float PitchMin = -40f;
        public float PitchMax = 70f;

        [Header("Apuntado")]
        public float AimMaxRange = 200f;
    }
}
