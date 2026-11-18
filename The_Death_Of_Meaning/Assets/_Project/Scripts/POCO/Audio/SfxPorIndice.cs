using System;

namespace TDOM.Gameplay.Audio
{
    public static class SfxPorIndice
    {
        public const int SinSfx = -1;

        public static int Elegir(int indice, int cantidad)
        {
            if (indice < 0 || cantidad <= 0)
                return SinSfx;

            return Math.Min(indice, cantidad - 1);
        }
    }
}
