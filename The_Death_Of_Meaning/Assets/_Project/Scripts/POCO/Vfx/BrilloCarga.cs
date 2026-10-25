using UnityEngine;

namespace TDOM.Gameplay.Vfx
{
    /// <summary>
    /// Cuentas del brillo de la hoja mientras se carga el ataque: suaviza el valor para que no
    /// salte al empezar o soltar la carga, y lo convierte en intensidad entre reposo y máximo.
    /// </summary>
    public static class BrilloCarga
    {
        public static float Suavizar(
            float actual,
            float objetivo,
            float segundos,
            float velocidad
        )
        {
            float objetivoLimitado = Mathf.Clamp01(objetivo);
            if (velocidad <= 0f || segundos <= 0f)
                return velocidad <= 0f ? objetivoLimitado : Mathf.Clamp01(actual);

            float k = 1f - Mathf.Exp(-velocidad * segundos);
            return Mathf.Clamp01(actual + (objetivoLimitado - actual) * k);
        }

        public static float Mezclar(float reposo, float maximo, float brillo) =>
            reposo + (maximo - reposo) * Mathf.Clamp01(brillo);
    }
}
