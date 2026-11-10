using System;

namespace TDOM.Gameplay.Audio
{
    public static class SubidaIntensidadSfx
    {
        public static float Factor(float tiempo, float duracion) =>
            duracion > 0f ? Math.Clamp(tiempo / duracion, 0f, 1f) : 1f;

        public static float Interpolar(float inicial, float final, float factor) =>
            inicial + (final - inicial) * Math.Clamp(factor, 0f, 1f);
    }
}
