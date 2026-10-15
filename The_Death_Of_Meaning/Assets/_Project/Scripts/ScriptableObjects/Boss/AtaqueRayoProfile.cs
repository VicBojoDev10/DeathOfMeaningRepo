using UnityEngine;

namespace TDOM.Data
{
    [CreateAssetMenu(fileName = "AtaqueRayoProfile", menuName = "TDOM/Boss/Ataque Rayo Profile")]
    public sealed class AtaqueRayoProfile : AtaqueArekProfile
    {
        [Header("Tiempos")]
        public float aviso = 1.25f;
        public float activo = 0.6f;
        public float retirada = 0.2f;

        [Header("Forma")]
        public float largo = 50f;
        public float diametro = 2f;
        public int lineas = 3;
        public float aberturaGrados = 180f; // abanico frente al jefe

        [Tooltip(
            "Ángulo máximo desde el frente del jefe al que puede salir un rayo. 90 = nunca hacia atrás de la boca."
        )]
        public float limiteFrontalGrados = 90f;

        [Header("Origen")]
        public float alturaBocaPorDefecto = 5f; // si no hay boca: centro del jefe + esta altura

        public override float Rango => largo;
    }
}
