using System.Collections.Generic;
using UnityEngine;

namespace TDOM.Gameplay.Vfx
{
    public static class EtapasCarga
    {
        public const int SinEtapa = -1;

        public static float Progreso(
            float segundosCargados,
            float segundosTranscurridos,
            float segundosMaximos
        )
        {
            if (segundosMaximos <= 0f)
                return 1f;

            return Mathf.Clamp01((segundosCargados + segundosTranscurridos) / segundosMaximos);
        }

        public static int Indice(IReadOnlyList<float> inicios, float progreso)
        {
            int indice = SinEtapa;
            for (int i = 0; i < inicios.Count; i++)
            {
                if (progreso < inicios[i])
                    break;
                indice = i;
            }
            return indice;
        }
    }
}
