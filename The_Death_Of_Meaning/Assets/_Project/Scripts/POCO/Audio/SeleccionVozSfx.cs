using System.Collections.Generic;

namespace TDOM.Gameplay.Audio
{
    public static class SeleccionVozSfx
    {
        public const int CrearNueva = -1;

        public static int Elegir(
            IReadOnlyList<bool> ocupadas,
            IReadOnlyList<float> inicios,
            int maximo
        )
        {
            int masVieja = CrearNueva;

            for (int i = 0; i < ocupadas.Count; i++)
            {
                if (!ocupadas[i])
                    return i;

                if (masVieja == CrearNueva || inicios[i] < inicios[masVieja])
                    masVieja = i;
            }

            return ocupadas.Count < maximo ? CrearNueva : masVieja;
        }
    }
}
