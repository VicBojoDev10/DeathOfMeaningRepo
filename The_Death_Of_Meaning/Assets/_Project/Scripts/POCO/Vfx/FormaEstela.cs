using UnityEngine;

namespace TDOM.Gameplay.Vfx
{
    /// <summary>Forma de la estela de la espada: cuanto mide de ancha y que tan opaca es.</summary>
    public static class FormaEstela
    {
        /// <summary>
        /// Ancho relativo a la hoja. El valor de la curva (0 a 1) se limita y se multiplica
        /// por la escala: 1 es la hoja completa, 2 el doble.
        /// </summary>
        public static float Ancho(float valorCurva, float escala) =>
            Mathf.Clamp01(valorCurva) * Mathf.Max(0f, escala);

        /// <summary>Opacidad del gradiente, sin bajar de la opacidad minima.</summary>
        public static float Opacidad(float alfaGradiente, float opacidadMinima) =>
            Mathf.Max(Mathf.Clamp01(alfaGradiente), Mathf.Clamp01(opacidadMinima));
    }
}
