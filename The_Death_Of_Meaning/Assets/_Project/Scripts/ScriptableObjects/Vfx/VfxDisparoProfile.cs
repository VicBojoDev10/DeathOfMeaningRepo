using UnityEngine;

namespace TDOM.Data
{
    [CreateAssetMenu(fileName = "VfxDisparoProfile", menuName = "TDOM/Vfx/Disparo Profile")]
    public sealed class VfxDisparoProfile : VfxProfile
    {
        [Header("Disparo")]
        public AnimationCurve avance = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        public float largoEstela = 0.3f;
        public float tamano = 1f;
        public AnimationCurve escala = AnimationCurve.Constant(0f, 1f, 1f);
    }
}
