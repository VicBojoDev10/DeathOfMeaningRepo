using System;
using System.Collections.Generic;

namespace TDOM.Gameplay.Combat
{
    /// <summary>Reglas deterministas de desplazamiento de AREK sobre el plano XZ.</summary>
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
            if (distancia <= 0.0001f)
                return;

            float paso = Math.Min(velocidad * dt, distancia);
            nuevaX += dx / distancia * paso;
            nuevaZ += dz / distancia * paso;
        }
    }
}
