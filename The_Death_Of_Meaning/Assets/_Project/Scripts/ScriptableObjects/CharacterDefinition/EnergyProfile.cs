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
    }
}
