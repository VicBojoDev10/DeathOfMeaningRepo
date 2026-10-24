using UnityEngine;

namespace TDOM.Gameplay.Vfx
{
    public static class MarcoTajo
    {
        private const float Minimo = 1e-6f;
        private const float MitadHoja = 0.5f;

        public static Vector3 Centro(Vector3 baseHoja, Vector3 punta) =>
            Vector3.Lerp(baseHoja, punta, MitadHoja);

        public static Vector3 Avance(Vector3 adelante, float rapidez)
        {
            Vector3 horizontal = Vector3.ProjectOnPlane(adelante, Vector3.up);
            return horizontal.sqrMagnitude < Minimo
                ? Vector3.zero
                : horizontal.normalized * rapidez;
        }
    }
}
