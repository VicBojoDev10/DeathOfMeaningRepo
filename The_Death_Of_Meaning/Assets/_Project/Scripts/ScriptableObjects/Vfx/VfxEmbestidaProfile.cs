using UnityEngine;

namespace TDOM.Data
{
    [CreateAssetMenu(fileName = "VfxEmbestidaProfile", menuName = "TDOM/Vfx/Embestida Profile")]
    public sealed class VfxEmbestidaProfile : VfxProfile
    {
        [Header("Recorrido")]
        public AnimationCurve avance = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        public float aparicion = 0.05f;
        public float desaparicion = 0.1f;

        [Header("Lineas de velocidad")]
        [Min(0f)]
        public float longitudLineas = 2f;

        [Min(0.001f)]
        public float grosorLineas = 0.04f;

        [Min(0f)]
        public float lineasPorSegundo = 120f;

        [Min(0f)]
        public float velocidadLineas = 6f;

        [Min(0f)]
        public float vidaLineas = 0.3f;

        [Min(0f)]
        public float radioLineas = 0.9f;
    }
}
