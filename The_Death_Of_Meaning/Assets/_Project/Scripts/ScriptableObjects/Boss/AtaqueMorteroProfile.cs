using UnityEngine;

namespace TDOM.Data
{
    [CreateAssetMenu(
        fileName = "AtaqueMorteroProfile",
        menuName = "TDOM/Boss/Ataque Mortero Profile"
    )]
    public sealed class AtaqueMorteroProfile : AtaqueArekProfile
    {
        [Header("Tiempos")]
        public float aviso = 1f;
        public float caida = 0.3f;
        public float retirada = 0.2f;

        [Header("Disparos")]
        public int disparos = 3;
        public float radioArea = 10f;
        public float radioArena = 25f;
        public int intentosPorPunto = 30;
        public float alturaCaida = 15f;
        public float altoPiedra = 6f;

        public override float Rango => radioArena;
    }
}
