using UnityEngine;

namespace TDOM.Gameplay.Vfx
{
    public static class ColorPolvoSuelo
    {
        public static Color Calcular(
            bool haySuelo,
            Color colorSuelo,
            Color colorSinSuelo,
            float aclarado
        )
        {
            Color baseColor = haySuelo ? colorSuelo : colorSinSuelo;
            Color resultado = Color.Lerp(baseColor, Color.white, Mathf.Clamp01(aclarado));
            resultado.a = 1f;
            return resultado;
        }
    }
}
