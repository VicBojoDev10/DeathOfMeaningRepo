using System.Collections.Generic;
using UnityEngine;

namespace TDOM.Gameplay.Combat
{
    /// <summary>
    /// Dónde caen los disparos del Mortero, en el plano XZ. POCO puro.
    /// El primero cae sobre el objetivo; luego uno sobre cada jugador extra en rango; los que
    /// sobren se pegan al grupo. Ningún par de áreas se solapa (centros a 2 × radioArea o más)
    /// y quedan lo más juntas posible: si un jugador está demasiado cerca, su disparo se recorre
    /// lo justo para tocar el área vecina sin chocar.
    /// </summary>
    public static class MorteroPlacement
    {
        private const float Tolerancia = 0.001f;

        /// <param name="objetivo">Jugador elegido (XZ).</param>
        /// <param name="otros">Otros jugadores dentro del rango del Mortero (XZ).</param>
        /// <param name="anguloInicial">Grados; orienta el grupo cuando no hay otra referencia.</param>
        public static List<Vector2> Colocar(
            Vector2 objetivo,
            IReadOnlyList<Vector2> otros,
            int disparos,
            float radioArea,
            float anguloInicial
        )
        {
            int cantidad = Mathf.Max(1, disparos);
            float separacion = 2f * radioArea;
            var puntos = new List<Vector2>(cantidad) { objetivo };

            if (otros != null)
            {
                foreach (var otro in otros)
                {
                    if (puntos.Count >= cantidad)
                        break;

                    puntos.Add(
                        LoMasCercaDe(otro, puntos, separacion, anguloInicial, aceptarLibre: true)
                    );
                }
            }

            // Los que sobran siempre tocan un área ya colocada, aunque haya hueco libre en medio.
            while (puntos.Count < cantidad)
                puntos.Add(
                    LoMasCercaDe(
                        Centro(puntos),
                        puntos,
                        separacion,
                        anguloInicial,
                        aceptarLibre: false
                    )
                );

            return puntos;
        }

        // El punto válido (sin solapar) más cercano a "deseado": él mismo si cabe y se acepta
        // libre, o uno pegado a las áreas ya colocadas.
        private static Vector2 LoMasCercaDe(
            Vector2 deseado,
            List<Vector2> puntos,
            float separacion,
            float anguloInicial,
            bool aceptarLibre
        )
        {
            if (aceptarLibre && EsValido(deseado, puntos, separacion))
                return deseado;

            bool encontrado = false;
            Vector2 mejor = default;
            float mejorDistancia = float.MaxValue;

            foreach (var candidato in Candidatos(deseado, puntos, separacion, anguloInicial))
            {
                if (!EsValido(candidato, puntos, separacion))
                    continue;

                float distancia = (candidato - deseado).sqrMagnitude;
                if (distancia < mejorDistancia - Tolerancia)
                {
                    mejor = candidato;
                    mejorDistancia = distancia;
                    encontrado = true;
                }
            }

            // Siempre hay hueco hacia afuera; esto es solo por seguridad.
            return encontrado
                ? mejor
                : puntos[0] + Direccion(anguloInicial) * separacion * puntos.Count;
        }

        // Puntos que tocan justo a una o dos áreas ya colocadas.
        private static IEnumerable<Vector2> Candidatos(
            Vector2 deseado,
            List<Vector2> puntos,
            float separacion,
            float anguloInicial
        )
        {
            foreach (var punto in puntos)
            {
                // Pegado a este punto, en dirección al deseado.
                Vector2 hacia = deseado - punto;
                if (hacia.sqrMagnitude > Tolerancia)
                    yield return punto + hacia.normalized * separacion;

                // Alrededor de este punto, cada 60°.
                for (int k = 0; k < 6; k++)
                    yield return punto + Direccion(anguloInicial + 60f * k) * separacion;
            }

            // Pegado a dos puntos a la vez (los huecos del grupo).
            for (int i = 0; i < puntos.Count; i++)
            {
                for (int j = i + 1; j < puntos.Count; j++)
                {
                    if (TryTangenteADos(puntos[i], puntos[j], separacion, out var a, out var b))
                    {
                        yield return a;
                        yield return b;
                    }
                }
            }
        }

        // Los dos puntos a distancia "separacion" de a y de b (intersección de dos círculos).
        private static bool TryTangenteADos(
            Vector2 a,
            Vector2 b,
            float separacion,
            out Vector2 primero,
            out Vector2 segundo
        )
        {
            primero = segundo = default;
            Vector2 ab = b - a;
            float d = ab.magnitude;
            if (d < Tolerancia || d > 2f * separacion)
                return false;

            Vector2 medio = a + ab * 0.5f;
            float h = Mathf.Sqrt(Mathf.Max(0f, separacion * separacion - d * d * 0.25f));
            Vector2 perpendicular = new Vector2(-ab.y, ab.x) / d;

            primero = medio + perpendicular * h;
            segundo = medio - perpendicular * h;
            return true;
        }

        private static bool EsValido(Vector2 candidato, List<Vector2> puntos, float separacion)
        {
            foreach (var punto in puntos)
            {
                if (Vector2.Distance(candidato, punto) < separacion - Tolerancia)
                    return false;
            }
            return true;
        }

        private static Vector2 Centro(List<Vector2> puntos)
        {
            Vector2 suma = Vector2.zero;
            foreach (var punto in puntos)
                suma += punto;
            return suma / puntos.Count;
        }

        private static Vector2 Direccion(float grados)
        {
            float radianes = grados * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(radianes), Mathf.Sin(radianes));
        }
    }
}
