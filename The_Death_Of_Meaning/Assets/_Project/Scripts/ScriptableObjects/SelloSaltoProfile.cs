using UnityEngine;

namespace TDOM.POCO.ScriptableObjects
{
    [CreateAssetMenu(menuName = "TDOM/Locomotion/Sello Salto")]
    public sealed class SelloSaltoProfile : ScriptableObject
    {
        [Header("Juego")]
        [Min(0f)]
        public float DuracionSellado = 4f;

        [Min(0f)]
        public float DuracionRuptura = 1f;

        [Range(0f, 1f)]
        public float MultiplicadorVelocidad = 0.4f;

        [Header("Runa")]
        [ColorUsage(false, true)]
        public Color ColorRitual = new Color(1f, 0.015f, 0.025f);

        [Min(0f)]
        public float Intensidad = 3.8f;

        [Min(0f)]
        public float Radio = 1.6f;

        [Min(0f)]
        public float AlturaInicial = 3f;

        [Min(0f)]
        public float AlturaFinal = 0.05f;

        [Min(0f)]
        public float VelocidadGiro = 90f;

        [Range(0f, 1f)]
        public float OpacidadSellado = 0.45f;

        [Min(1f)]
        public float EscalaRuptura = 3f;

        public AnimationCurve TrazoSellado = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        public AnimationCurve DescensoSellado = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    }
}
