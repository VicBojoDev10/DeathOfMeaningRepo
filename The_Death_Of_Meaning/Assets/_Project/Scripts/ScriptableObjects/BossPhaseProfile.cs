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
        public float VidaTransicion;

        [Header("Minions (TW-125)")]
        [Min(0)]
        public int MinionsEnEspecial;

        [Header("Transición al entrar a esta fase")]
        [Min(0f)]
        public float DuracionTransicion = 3f;

        [Header("Movimiento de AREK")]
        [Min(0f)]
        public float VelocidadMovimiento;

        [Min(0f)]
        public float DistanciaMinimaJugador = 5f;

        [Header("Detener movimiento durante el ataque")]
        public bool DetenerseEnBasico = true;
        public bool DetenerseEnPesado = true;
        public bool DetenerseEnEspecial = true;
        public bool DetenerseEnInstaKill = true;

        public bool DetenerseDuranteAtaque(BossAttackKind tipo)
        {
            switch (tipo)
            {
                case BossAttackKind.Basico: return DetenerseEnBasico;
                case BossAttackKind.Pesado: return DetenerseEnPesado;
                case BossAttackKind.Especial: return DetenerseEnEspecial;
                case BossAttackKind.InstaKill: return DetenerseEnInstaKill;
                default: return false;
            }
        }

        [Header("Visual de la fase")]
        public Color ColorFase = Color.white;

        public Color ColorDestello = Color.white;

        [Min(0f)]
        public float IntensidadDestello = 4f;

        [Min(0f)]
        public float DuracionDestello = 0.5f;
    }
}
