using System;

namespace TDOM.Gameplay.Combat
{
    public static class ProgresoTransicionVisual
    {
        public const float SinPausa = -1f;

        public static float Calcular(
            double fin,
            double ahora,
            float restanteEnPausa,
            float duracion
        )
        {
            if (duracion <= 0f)
                return 1f;

            double restante = restanteEnPausa >= 0f ? restanteEnPausa : fin - ahora;
            return (float)Math.Clamp(1d - restante / duracion, 0d, 1d);
        }
    }
}
