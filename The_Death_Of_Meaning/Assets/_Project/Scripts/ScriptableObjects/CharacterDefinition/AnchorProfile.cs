using UnityEngine;

namespace TDOM.Data
{
    [CreateAssetMenu(menuName = "TDOM/Anchor Profile", fileName = "AnchorProfile")]
    public sealed class AnchorProfile : ScriptableObject
    {
        public float Range;
        public float TravelSpeed;
        public float StuckDuration;
        public float StuckDurationOnEye = 10f;
        public float Cooldown;

        [Range(0f, 1f)]
        [Tooltip("Multiplica el cooldown cuando el ancla no se pegó a nada.")]
        public float FactorCooldownSinConectar = 0.5f;

        public float ToleranciaOrigen = 6f;
    }
}
