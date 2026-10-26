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

        [Header("Zoom de combate (pulso)")]
        [Tooltip(
            "Apagado: el combate ya no deja la camara acercada (usa la distancia y el FOV de "
                + "exploracion). El acercamiento sale como un pulso al golpear."
        )]
        public bool CombatZoomSostenido = false;

        [Tooltip(
            "Fuerza del pulso respecto a la diferencia Exploracion -> Combate. 0.5 = la mitad "
                + "del zoom que antes se quedaba fijo. 0 = sin pulso."
        )]
        [Range(0f, 1f)]
        public float CombatPulsoFactor = 0.5f;

        [Tooltip("Segundos que tarda el pulso en llegar al maximo.")]
        public float CombatPulsoSubida = 0.06f;

        [Tooltip("Segundos que tarda en volver al encuadre normal.")]
        public float CombatPulsoBajada = 0.22f;

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

        [Header("Lock-on")]
        public float LockOnRange = 60f;
        public float LockOnSmoothTime = 0.15f;
        public float LockOnPitchMin = -60f;
        public float LockOnPitchMax = 60f;

        [Range(0f, 1f)]
        public float LockOnSwitchThreshold = 0.7f;

        [Range(0f, 1f)]
        public float LockOnSwitchResetThreshold = 0.3f;

        [Range(0f, 180f)]
        public float LockOnSwitchMaxAngle = 60f;

        [Header("Pitch")]
        public float PitchMin = -40f;
        public float PitchMax = 70f;

        [Header("Apuntado")]
        public float AimMaxRange = 200f;
    }
}
