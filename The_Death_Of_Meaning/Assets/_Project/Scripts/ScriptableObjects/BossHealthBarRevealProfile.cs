using UnityEngine;

namespace TDOM.Data
{
    [CreateAssetMenu(
        fileName = "BossHealthBarRevealProfile",
        menuName = "TDOM/Boss Health Bar Reveal Profile"
    )]
    public sealed class BossHealthBarRevealProfile : ScriptableObject
    {
        [Min(0f)]
        public float DuracionBarrido = 1.7f;

        public AnimationCurve CurvaBarrido = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Min(0f)]
        public float RetrasoNombre = 0.15f;

        [Min(0f)]
        public float DuracionNombre = 1.3f;
    }
}
