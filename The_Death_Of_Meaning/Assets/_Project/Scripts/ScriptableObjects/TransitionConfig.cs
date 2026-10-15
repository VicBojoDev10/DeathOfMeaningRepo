using UnityEngine;

namespace TDOM.Data
{
    [CreateAssetMenu(fileName = "TransitionConfig", menuName = "TDOM/Transition Config")]
    public sealed class TransitionConfig : ScriptableObject
    {
        [Header("Fade entre ventanas")]
        [Min(0f)]
        public float duracionFadeVentana = 0.25f;

        [Header("Pantalla de carga")]
        [Min(0f)]
        public float duracionFadePantallaCarga = 0.25f;

        [Min(0f)]
        public float tiempoMinimoPantallaCarga = 0.5f;

        public float velocidadSpinnerGradosPorSegundo = 270f;
    }
}
