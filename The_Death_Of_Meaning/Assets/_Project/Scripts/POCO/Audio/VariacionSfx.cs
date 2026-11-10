using System;

namespace TDOM.Gameplay.Audio
{
    public static class VariacionSfx
    {
        public const float PitchMinimo = 0.1f;
        public const int SinClip = -1;

        public static float Volumen(float volumenBase, float variacion, float aleatorio01) =>
            Limitar01(Desplazar(volumenBase, variacion, aleatorio01));

        public static float Pitch(float pitchBase, float variacion, float aleatorio01) =>
            Math.Max(PitchMinimo, Desplazar(pitchBase, variacion, aleatorio01));

        public static int IndiceClip(int cantidad, int anterior, float aleatorio01)
        {
            if (cantidad <= 0)
                return SinClip;

            if (cantidad == 1)
                return 0;

            bool evitarAnterior = anterior >= 0 && anterior < cantidad;
            int candidatos = evitarAnterior ? cantidad - 1 : cantidad;
            int indice = Math.Min((int)(Limitar01(aleatorio01) * candidatos), candidatos - 1);

            return evitarAnterior && indice >= anterior ? indice + 1 : indice;
        }

        private static float Desplazar(float valorBase, float variacion, float aleatorio01) =>
            valorBase + (Limitar01(aleatorio01) * 2f - 1f) * Math.Abs(variacion);

        private static float Limitar01(float valor) => Math.Clamp(valor, 0f, 1f);
    }
}
