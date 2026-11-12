using TDOM.Data;
using UnityEngine;

namespace TDOM.POCO.ScriptableObjects
{
    [CreateAssetMenu(fileName = "WeakPointProfile", menuName = "TDOM/Boss/WeakPointProfile")]
    public class WeakPointProfile : ScriptableObject
    {
        [Header("Vida de cada ojo")]
        [Tooltip("De aquí sale la vida base: cada ojo vale MultiplicadorVidaOjo veces la de un minion.")]
        public MinionProfile PerfilMinion;

        [Tooltip("Vida del ojo en veces la vida de un minion (2 = el doble).")]
        [Min(0.1f)]
        public float MultiplicadorVidaOjo = 2f;

        [Header("Visual placeholder por estado")]
        public Color ColorCerrado = Color.gray;

        public Color ColorAbierto = Color.red;

        [Header("Glow de los ojos del modelo (OjoLow)")]
        [Min(0f)]
        public float IntensidadGlow = 2f;

        private const float VidaMinionPorDefecto = 100f;

        /// <summary>Vida de cada ojo: la del minion por el multiplicador.</summary>
        public float VidaOjo =>
            (PerfilMinion != null ? PerfilMinion.vida : VidaMinionPorDefecto) * MultiplicadorVidaOjo;
    }
}
