using UnityEngine;

namespace TDOM.Gameplay.Camera
{
    public static class AimResolver
    {
        private const float DistanciaMinimaCuadrada = 0.0001f;

        public static AimSolution ResolveHit(Ray rayoCamara, Vector3 puntoImpacto, Vector3 origen)
        {
            return Construir(rayoCamara, puntoImpacto, origen, hayImpacto: true);
        }

        public static AimSolution ResolveMiss(Ray rayoCamara, float alcanceMaximo, Vector3 origen)
        {
            return Construir(
                rayoCamara,
                rayoCamara.GetPoint(alcanceMaximo),
                origen,
                hayImpacto: false
            );
        }

        public static bool IsWithinRange(Vector3 origen, Vector3 punto, float alcance)
        {
            return (punto - origen).sqrMagnitude <= alcance * alcance;
        }

        private static AimSolution Construir(
            Ray rayoCamara,
            Vector3 puntoObjetivo,
            Vector3 origen,
            bool hayImpacto
        )
        {
            Vector3 haciaObjetivo = puntoObjetivo - origen;
            Vector3 direccion =
                haciaObjetivo.sqrMagnitude < DistanciaMinimaCuadrada
                    ? rayoCamara.direction
                    : haciaObjetivo.normalized;
            return new AimSolution(puntoObjetivo, direccion, hayImpacto);
        }
    }
}
