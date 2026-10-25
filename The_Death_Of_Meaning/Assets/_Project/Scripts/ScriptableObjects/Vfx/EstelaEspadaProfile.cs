using UnityEngine;

namespace TDOM.Data
{
    [CreateAssetMenu(fileName = "EstelaEspadaProfile", menuName = "TDOM/Vfx/Estela Espada Profile")]
    public sealed class EstelaEspadaProfile : ScriptableObject
    {
        [Header("Forma")]
        [Min(0.01f)]
        public float duracionVida = 0.18f;

        [Min(0f)]
        public float distanciaMinima = 0.02f;

        [Min(2)]
        public int maxPuntos = 48;

        [Tooltip(
            "Ancho de la estela segun su edad: 0 = junto a la espada, 1 = el final de su vida. "
                + "Con 1 al inicio y 0 al final, la estela es mas ancha en la espada y termina en punta."
        )]
        public AnimationCurve curvaAncho = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        [Min(0f)]
        [Tooltip(
            "Multiplica el ancho: 1 = el largo de la hoja, 2 = el doble (se extiende desde la "
                + "punta hacia el mango y mas alla)."
        )]
        public float escalaAncho = 1f;

        [Range(0f, 1f)]
        [Tooltip(
            "Opacidad minima de la parte mas lejana de la estela. La opacidad baja desde la "
                + "espada (segun el gradiente) pero nunca por debajo de este valor."
        )]
        public float opacidadMinima = 0.35f;

        [Header("Material")]
        public Gradient gradienteColor = new Gradient();

        public Material material;
    }
}
