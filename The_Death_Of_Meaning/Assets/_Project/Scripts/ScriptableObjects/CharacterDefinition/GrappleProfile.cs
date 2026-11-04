using UnityEngine;

namespace TDOM.Data
{
    [CreateAssetMenu(menuName = "TDOM/Grapple Profile")]
    public sealed class GrappleProfile : ScriptableObject
    {
        public float Range;
        public float PullSpeed;
        public float Cooldown;
        public float ArrivalDistance;
        public float MaxDuration;

        [Header("Contra el jefe")]
        [Min(0f)]
        [Tooltip(
            "Si el gancho apunta al jefe, el jalón se corta a esta distancia (metros) del punto y "
                + "el jugador rebota. 0 = sin rebote."
        )]
        public float BossStopDistance = 4f;

        [Min(0f)]
        [Tooltip("Velocidad (m/s) del salto de rebote.")]
        public float BossReboundSpeed = 14f;

        [Range(0f, 80f)]
        [Tooltip("Elevación del salto de rebote sobre el horizonte, hacia atrás.")]
        public float BossReboundAngle = 45f;
    }
}
