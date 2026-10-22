using UnityEngine;

namespace TDOM.Data
{
    [CreateAssetMenu(fileName = "AtaqueManoProfile", menuName = "TDOM/Boss/Ataque Mano Profile")]
    public sealed class AtaqueManoProfile : AtaqueArekProfile
    {
        [Header("Tiempos")]
        public float aviso = 1.25f;
        public float barrido = 1f;
        public float retirada = 0.3f;

        [Header("Dimensiones de la Mano")]
        public float radio = 25f;
        public float alto = 1.2f;
        public float ancho = 2f;

        [Header("Daño")]
        public float dano = 20f;

        public override float Rango => radio;

        public override float Dano => dano;
    }
}
