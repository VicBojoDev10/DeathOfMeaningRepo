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
            Vector3 adelante,
            Quaternion respaldo
        )
        {
            Vector3 normal = Vector3.Cross(punta - baseHoja, punta - puntaAnterior);
            if (normal.sqrMagnitude < Minimo)
                return respaldo;

            Vector3 adelanteEnPlano = Vector3.ProjectOnPlane(adelante, normal.normalized);
            if (adelanteEnPlano.sqrMagnitude < Minimo)
                return respaldo;

            return Quaternion.LookRotation(adelanteEnPlano.normalized, normal.normalized);
        }
    }
}
