using UnityEngine;

namespace TDOM.Data
{
    [CreateAssetMenu(fileName = "VfxCargaProfile", menuName = "TDOM/Vfx/Carga Profile")]
    public sealed class VfxCargaProfile : VfxProfile
    {
        [Header("Carga")]
        public AnimationCurve escala = AnimationCurve.Linear(0f, 1.5f, 1f, 0.3f);
        public float duracionMaxima = 5f;
    }
}
