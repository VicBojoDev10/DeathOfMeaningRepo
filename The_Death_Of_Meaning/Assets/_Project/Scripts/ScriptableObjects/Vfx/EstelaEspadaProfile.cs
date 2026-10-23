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

        public AnimationCurve curvaAncho = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        [Header("Material")]
        public Gradient gradienteColor = new Gradient();

        public Material material;
    }
}
