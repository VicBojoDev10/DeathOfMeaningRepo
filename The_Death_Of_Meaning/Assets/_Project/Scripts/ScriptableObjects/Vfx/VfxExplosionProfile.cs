using UnityEngine;

namespace TDOM.Data
{
    [CreateAssetMenu(fileName = "VfxExplosionProfile", menuName = "TDOM/Vfx/Explosion Profile")]
    public sealed class VfxExplosionProfile : VfxProfile
    {
        [Header("Explosión")]
        public float radio = 1f;
        public AnimationCurve escala = AnimationCurve.EaseInOut(0f, 0.2f, 1f, 1f);
    }
}
