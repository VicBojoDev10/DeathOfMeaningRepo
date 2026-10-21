using UnityEngine;

namespace TDOM.Data
{
    [CreateAssetMenu(fileName = "VfxDisparoProfile", menuName = "TDOM/Vfx/Disparo Profile")]
    public sealed class VfxDisparoProfile : VfxProfile
    {
        [Header("Disparo")]
        public float tamano = 1f;
        public AnimationCurve escala = AnimationCurve.EaseInOut(0f, 1f, 1f, 0.5f);
    }
}
