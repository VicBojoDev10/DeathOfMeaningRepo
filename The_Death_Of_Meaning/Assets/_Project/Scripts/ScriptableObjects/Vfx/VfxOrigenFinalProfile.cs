using UnityEngine;

namespace TDOM.Data
{
    [CreateAssetMenu(
        fileName = "VfxOrigenFinalProfile",
        menuName = "TDOM/Vfx/Origen-final Profile"
    )]
    public sealed class VfxOrigenFinalProfile : VfxProfile
    {
        [Header("Origen-final")]
        public AnimationCurve avance = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        public bool orientarATrayectoria = true;
        public float particulasResiduales = 20f;
        public float ancho = 0.2f;
        public float aparicion = 0.05f;
        public float desaparicion = 0.15f;
    }
}
