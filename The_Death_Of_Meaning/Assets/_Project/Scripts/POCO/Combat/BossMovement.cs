using System;
using System.Collections.Generic;

namespace TDOM.Gameplay.Combat
{
    public sealed class BossMovement
    {
        public ulong? ObjetivoFijado { get; private set; }

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

            if (candidatos == null || candidatos.Count == 0)
            {
                ObjetivoFijado = null;
                objetivoId = 0;
                return false;
            }

            if (ObjetivoFijado.HasValue)
            {
                foreach (var candidato in candidatos)
                {
                    if (candidato.Id != ObjetivoFijado.Value)
                        continue;

                    objetivoId = candidato.Id;
                    return true;
                }
            }

            BossTargetCandidate masCercano = candidatos[0];
            for (int i = 1; i < candidatos.Count; i++)
            {
                if (candidatos[i].Distancia < masCercano.Distancia)
                    masCercano = candidatos[i];
            }

            objetivoId = masCercano.Id;
            ObjetivoFijado = objetivoId;
            return true;
        }

        public void ResetearObjetivo() => ObjetivoFijado = null;

        public void LimitarDentroDeCirculo(
            float x,
            float z,
            float centroX,
            float centroZ,
            float radio,
            out float nuevaX,
            out float nuevaZ
        )
        {
            nuevaX = x;
            nuevaZ = z;
            if (radio <= 0f)
                return;

            float dx = x - centroX;
            float dz = z - centroZ;
            float distancia = (float)Math.Sqrt(dx * dx + dz * dz);
            if (distancia <= radio || distancia <= 0.0001f)
                return;

            float escala = radio / distancia;
            nuevaX = centroX + dx * escala;
            nuevaZ = centroZ + dz * escala;
        }

        public static float CalcularRadioSeguro(
            float radioArena,
            float margenBorde,
            float radioBoss
        )
        {
            return Math.Max(0f, radioArena - Math.Max(0f, margenBorde) - Math.Max(0f, radioBoss));
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
