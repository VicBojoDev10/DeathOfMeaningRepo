using UnityEngine;

namespace TDOM.Data
{
    [CreateAssetMenu(menuName = "TDOM/Energy Profile")]
    public sealed class EnergyProfile : ScriptableObject
    {
        public float Max;
        public float RegenPerSecond;
        public float ChargedCost;
        public float DrainPerSecondDowned;

        [Header("Costos de movimiento y disparo")]
        [Tooltip("Costo de cada disparo normal (no cargado). 0 = gratis.")]
        [Min(0f)]
        public float ShotCost;

        [Tooltip("Costo de cada dash (Ayla) o embestida (Zendre). 0 = gratis.")]
        [Min(0f)]
        public float DashCost;

        [Tooltip("Costo del doble salto (salto en el aire). 0 = gratis.")]
        [Min(0f)]
        public float AirJumpCost;
    }
}
