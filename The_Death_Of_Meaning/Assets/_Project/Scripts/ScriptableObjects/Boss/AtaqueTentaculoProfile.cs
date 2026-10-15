using UnityEngine;

namespace TDOM.Data
{
    [CreateAssetMenu(
        fileName = "AtaqueTentaculoProfile",
        menuName = "TDOM/Boss/Ataque Tentaculo Profile"
    )]
    public sealed class AtaqueTentaculoProfile : AtaqueArekProfile
    {
        [Header("Tiempos")]
        public float aviso = 1.25f;
        public float azote = 0.35f;
        public float retirada = 0.4f;

        [Header("Dimensiones del Tentáculo")]
        public float largo = 25f;
        public float ancho = 3f;
        public float alto = 1.5f;

        [Header("Dirección")]
        public float desvioMaximo = 15f;

        public override float Rango => largo;
    }
}
