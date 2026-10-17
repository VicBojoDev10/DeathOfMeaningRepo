using System;
using System.Collections.Generic;

namespace TDOM.Gameplay.Combat
{
    public sealed class BossMovement
    {
        public bool TryElegirObjetivo(
            IReadOnlyList<BossTargetCandidate> candidatos,
            ulong? zendreAnclado,
            out ulong objetivoId
        )
        {
            if (zendreAnclado.HasValue)
            {
                objetivoId = zendreAnclado.Value;
                return true;
            }

            objetivoId = 0;
            if (candidatos == null || candidatos.Count == 0)
                return false;

            BossTargetCandidate masCercano = candidatos[0];
            for (int i = 1; i < candidatos.Count; i++)
            {
                if (candidatos[i].Distancia < masCercano.Distancia)
                    masCercano = candidatos[i];
            }

            objetivoId = masCercano.Id;
            return true;
        }

        public void Avanzar(
            int fase,
            bool zendreAnclado,
            float x,
            float z,
            float objetivoX,
            float objetivoZ,
            float velocidad,
            float distanciaMinima,
            float dt,
            out float nuevaX,
            out float nuevaZ
        )
        {
            nuevaX = x;
            nuevaZ = z;
            if (fase <= 1 || zendreAnclado || velocidad <= 0f || dt <= 0f)
                return;

            float dx = objetivoX - x;
            float dz = objetivoZ - z;
            float distancia = (float)Math.Sqrt(dx * dx + dz * dz);
            distanciaMinima = Math.Max(0f, distanciaMinima);
            float distanciaDisponible = distancia - distanciaMinima;
            if (distanciaDisponible <= 0.0001f)
                return;

            float paso = Math.Min(velocidad * dt, distanciaDisponible);
            nuevaX += dx / distancia * paso;
            nuevaZ += dz / distancia * paso;
        }
    }
}
