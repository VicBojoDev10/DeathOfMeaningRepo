using UnityEngine;

namespace TDOM.Gameplay.Vfx
{
    public static class VisibilidadVfx
    {
        public static float Factor(
            float tiempo,
            float duracion,
            float aparicion,
            float desaparicion
        )
        {
            float entrada = aparicion > 0f ? Mathf.Clamp01(tiempo / aparicion) : 1f;
            float salida =
                desaparicion > 0f ? Mathf.Clamp01((duracion - tiempo) / desaparicion) : 1f;
            return Mathf.Min(entrada, salida);
        }
    }
}
