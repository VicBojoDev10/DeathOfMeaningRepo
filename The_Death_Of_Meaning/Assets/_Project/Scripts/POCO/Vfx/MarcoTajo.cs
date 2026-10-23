using UnityEngine;

namespace TDOM.Gameplay.Vfx
{
    public static class MarcoTajo
    {
        private const float Minimo = 1e-6f;
        private const float MitadHoja = 0.5f;

        public static Vector3 Centro(Vector3 baseHoja, Vector3 punta) =>
            Vector3.Lerp(baseHoja, punta, MitadHoja);

        public static Quaternion Rotacion(
            Vector3 baseHoja,
            Vector3 punta,
            Vector3 puntaAnterior,
            Quaternion respaldo
        )
        {
            Vector3 barrido = punta - puntaAnterior;
            Vector3 normal = Vector3.Cross(punta - baseHoja, barrido);
            if (barrido.sqrMagnitude < Minimo || normal.sqrMagnitude < Minimo)
                return respaldo;

            return Quaternion.LookRotation(barrido.normalized, normal.normalized);
        }

        public static Vector3 Avance(Vector3 adelante, float rapidez)
        {
            Vector3 horizontal = Vector3.ProjectOnPlane(adelante, Vector3.up);
            return horizontal.sqrMagnitude < Minimo
                ? Vector3.zero
                : horizontal.normalized * rapidez;
        }
    }
}
