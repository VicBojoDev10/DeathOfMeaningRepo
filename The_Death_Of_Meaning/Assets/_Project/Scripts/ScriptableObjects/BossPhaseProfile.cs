using TDOM.Contracts;
using UnityEngine;

namespace TDOM.POCO.ScriptableObjects
{
    [CreateAssetMenu(fileName = "BossPhaseProfile", menuName = "TDOM/BossPhaseProfile")]
    public class BossPhaseProfile : ScriptableObject
    {
        public int Fase;

        [SerializeField]
        public BossAttackStep[] OrdenDeAtaque;

        [Range(0f, 1f)]
        public float VidaTransicion; // Ejemplo: 0.8f para 80%

        [Header("Minions (TW-125)")]
        [Min(0)]
        public int MinionsEnEspecial; // Cuántos aparecen cuando el jefe lanza su Especial (Rayo)

        [Header("Transición al entrar a esta fase")]
        [Min(0f)]
        public float DuracionTransicion = 3f; // Segundos de invulnerabilidad y sin atacar

        [Header("Visual de la fase")]
        public Color ColorFase = Color.white; // Color del cuerpo durante esta fase

        public Color ColorDestello = Color.white;

        [Min(0f)]
        public float IntensidadDestello = 4f;

        [Min(0f)]
        public float DuracionDestello = 0.5f;
    }
}
