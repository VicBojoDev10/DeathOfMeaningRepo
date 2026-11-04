using UnityEngine;

namespace TDOM.Data
{
    [CreateAssetMenu(fileName = "AtaqueManoProfile", menuName = "TDOM/Boss/Ataque Mano Profile")]
    public sealed class AtaqueManoProfile : AtaqueArekProfile
    {
        [Header("Tiempos")]
        [Tooltip("Burbuja de lava + salida de la columna, antes del barrido.")]
        public float aviso = 1.25f;
        public float barrido = 1f;

        [Tooltip("Retracción de la columna tras el barrido (rápida).")]
        public float retirada = 0.18f;

        [Header("Salida y retracción")]
        [Tooltip("Parte del aviso que dura la burbuja sola; el resto la columna sale del jefe.")]
        [Range(0f, 1f)]
        public float fraccionBurbuja = 0.6f;

        [Tooltip("Segundos que sigue vivo el ataque tras retraerse, para que la estela se apague.")]
        public float retrasoDespawn = 0.3f;

        [Header("Origen")]
        [Tooltip(
            "Ajuste fino en X/Z del centro de donde parte la columna (local al jefe). "
                + "La Y no cuenta: usa alturaOrigen."
        )]
        public Vector3 desfaseOrigen = Vector3.zero;

        [Tooltip(
            "Metros sobre el piso del origen: sube la columna, la burbuja y la estela. "
                + "El abanico de aviso se queda en el piso."
        )]
        public float alturaOrigen = 0f;

        [Tooltip(
            "Activo: la banda de daño se queda a ras del piso con su alto de siempre, sin "
                + "importar alturaOrigen. Apagado: es una pared del piso a lo alto de la columna."
        )]
        public bool danoAlPiso = true;

        [Tooltip("El origen nace en la orilla de la silueta del jefe, en la dirección del ataque.")]
        public bool origenEnBordeDelJefe = true;

        [Tooltip("Metros del centro del jefe al borde si no se puede medir su silueta.")]
        public float radioBorde = 12f;

        [Tooltip("Metros que la raíz de la columna entra al modelo desde el borde.")]
        public float adentroBorde = 2f;

        [Header("Burbuja de lava")]
        [Tooltip("Tamaño en metros de la burbuja del origen.")]
        public float tamanoBurbuja = 4f;

        [Header("Dimensiones de la Mano (también la caja de daño)")]
        public float radio = 25f;
        public float alto = 1.2f;
        public float ancho = 2f;

        [Header("Grosor visual del brazo (no cambia el daño)")]
        [Tooltip(
            "Grosor del brazo según su largo. 1 = proporción natural del modelo "
                + "(TercerBrazo_Low); menos lo adelgaza, más lo engorda."
        )]
        [Min(0f)]
        public float grosorBrazo = 1f;

        [Tooltip(
            "Tope del grosor visual, como múltiplo de ancho y alto. Así el brazo no se ve "
                + "mucho más ancho que la banda de daño."
        )]
        [Min(0f)]
        public float grosorMaximo = 2f;

        [Header("Daño")]
        public float dano = 20f;

        public override float Rango => radio;

        public override float Dano => dano;
    }
}
