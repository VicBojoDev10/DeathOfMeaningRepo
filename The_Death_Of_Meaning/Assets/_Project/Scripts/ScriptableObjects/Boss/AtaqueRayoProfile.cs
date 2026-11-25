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

        [Tooltip(
            "Velocidad a la que el frente del rayo recorre la franja, en unidades por segundo."
        )]
        [Min(0.01f)]
        public float velocidadRecorrido = 83.3333f;

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

        [Header("Daño")]
        [Min(0f)]
        public float dano = 40f;

        public override float Rango => largo;

        public override float Dano => dano;

        public override float Aviso => aviso;
    }
}
