using UnityEngine;

namespace TDOM.POCO.ScriptableObjects
{
    [CreateAssetMenu(fileName = "WeakPointProfile", menuName = "TDOM/Boss/WeakPointProfile")]
    public class WeakPointProfile : ScriptableObject
    {
        [Header("Ciclo de cada ojo")]
        [Min(0.01f)]
        public float DuracionAbierto = 10f;

        [Min(0f)]
        public float Cooldown = 5f;

        public int Semilla;

        [Header("Ojos activos por fase (uno por BossPhaseProfile)")]
        public int[] OjosActivosPorFase = { 2, 2, 2, 3, 3 };

        [Header("Visual placeholder por estado")]
        public Color ColorCerrado = Color.gray;

        public Color ColorBrillando = Color.yellow;

        public Color ColorAbierto = Color.red;

        public Color ColorEnfriando = Color.blue;
    }
}
