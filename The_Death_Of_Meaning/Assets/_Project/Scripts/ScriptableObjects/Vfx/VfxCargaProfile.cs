using UnityEngine;

namespace TDOM.Data
{
    [CreateAssetMenu(fileName = "VfxCargaProfile", menuName = "TDOM/Vfx/Carga Profile")]
    public sealed class VfxCargaProfile : VfxProfile
    {
        [Header("Carga")]
        public AnimationCurve escala = AnimationCurve.Linear(0f, 0.5f, 1f, 1f);
        public float radioCentro = 0.2f;
        public float velocidadParticulas = 1f;
        public float duracionMaxima = 5f;
    }
}
