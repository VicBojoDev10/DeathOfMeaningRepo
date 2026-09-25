using TDOM.Contracts;
using UnityEngine;

namespace TDOM.POCO.ScriptableObjects
{
    [CreateAssetMenu(fileName = "BossPhaseProfile", menuName = "TDOM/BossPhaseProfile")]
    public class BossPhaseProfile : ScriptableObject
    {
        public int Fase;
        public BossAttackKind[] OrdenDeAtaque;

        [Range(0f, 1f)]
        public float VidaTransicion; // Ejemplo: 0.8f para 80%
    }
}
