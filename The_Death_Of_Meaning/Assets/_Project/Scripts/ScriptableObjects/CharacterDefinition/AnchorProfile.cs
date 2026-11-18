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
        [Header("Costo del gancho por segundo de uso")]
        [Min(0f)]
        [Tooltip("Stamina consumida por segundo mientras el gancho está activo.")]
        public float StaminaCosto = 25f;

        [Range(0f, 1f)]
        public float ReembolsoSinConectar = 0f;

        public float ToleranciaOrigen = 6f;
    }
}
