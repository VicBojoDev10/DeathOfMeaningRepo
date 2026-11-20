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

        [Header("Lanzamiento de minions")]
        [Min(0.1f)]
        public float TiempoMaximoGiroMinion = 2.5f;

        [Min(0f)]
        public float VelocidadGiroMinion = 180f;

        [Min(0f)]
        public float AceleracionGiroMinion = 90f;

        [Min(0f)]
        public float VelocidadGiroMaximaMinion = 540f;

        [Min(0f)]
        public float FuerzaLanzamientoMinion = 12f;

        [Min(0f)]
        public float AlturaLanzamientoMinion = 3f;

        [Min(0f)]
        public float DanioColisionMinionLanzado = 0f;

        [Min(0f)]
        public float MultiplicadorEmpujeColisionMinion = 0.75f;

        [Range(0f, 1f)]
        public float MovimientoDuranteGiro = 0f;

        [Min(0f)]
        public float AlejamientoCamaraCapturaMinion = 1.25f;

        [Min(0f)]
        public float DistanciaSujecionMinion = 1.1f;

        [Min(0f)]
        public float AlturaSujecionMinion = 1.2f;
    }
}
