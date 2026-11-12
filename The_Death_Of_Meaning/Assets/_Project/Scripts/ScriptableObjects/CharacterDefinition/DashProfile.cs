using UnityEngine;

namespace TDOM.Data
{
    [CreateAssetMenu(menuName = "TDOM/Dash Profile")]
    public sealed class DashProfile : ScriptableObject
    {
        public float Distance,
            Duration,
            Cooldown,
            MaxTurnRate;
        public AnimationCurve Easing = AnimationCurve.Linear(0, 0, 1, 1);

        [Header("Empuje de minions terrestres")]
        [Min(0f)]
        public float DistanciaEmpujeMinions;

        [Min(0f)]
        public float AlturaEmpujeMinions;

        [Min(0f)]
        public float AturdimientoMinions;

        public bool EmpujeMinionsValido =>
            EsPositivoFinito(DistanciaEmpujeMinions)
            && EsPositivoFinito(AlturaEmpujeMinions)
            && EsPositivoFinito(AturdimientoMinions);

        private static bool EsPositivoFinito(float valor) =>
            valor > 0f && !float.IsNaN(valor) && !float.IsInfinity(valor);
    }
}
