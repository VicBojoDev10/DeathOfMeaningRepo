using UnityEngine;

namespace TDOM.Gameplay.Vfx
{
    /// <summary>
    /// Posición y orientación de la estela para colocar efectos sobre ella: el centro del
    /// tramo de estela vigente y una rotación con el eje Z siguiendo el recorrido de la
    /// hoja y el eje Y perpendicular a la superficie que barre (el plano de la cinta).
    /// </summary>
    public static class MarcoEstela
    {
        private const float AvanceMinimo = 0.05f;
        private const float SenoMinimo = 0.1f;

        public static bool Calcular(
            MuestrasEstela muestras,
            float edadMaximaDeLaMasNueva,
            out Vector3 centro,
            out Quaternion rotacion
        )
        {
            centro = Vector3.zero;
            rotacion = Quaternion.identity;
            if (muestras == null || muestras.Cantidad < 2)
                return false;

            int ultimo = muestras.Cantidad - 1;
            if (muestras[ultimo].Edad > edadMaximaDeLaMasNueva)
                return false;

            // Solo el tramo vigente: una cancelación o cambio de ataque abre otro tramo.
            int primero = ultimo;
            while (primero > 0 && muestras[primero].ConectaConAnterior)
                primero--;

            int cantidad = ultimo - primero + 1;
            if (cantidad < 2)
                return false;

            Vector3 suma = Vector3.zero;
            for (int i = primero; i <= ultimo; i++)
                suma += Medio(muestras[i]);
            centro = suma / cantidad;

            Vector3 avance = Medio(muestras[ultimo]) - Medio(muestras[primero]);
            Vector3 hoja = muestras[ultimo].Punta - muestras[ultimo].BaseHoja;
            Vector3 normal = Vector3.Cross(avance, hoja);
            if (
                avance.sqrMagnitude < AvanceMinimo * AvanceMinimo
                || normal.sqrMagnitude
                    < SenoMinimo * SenoMinimo * avance.sqrMagnitude * hoja.sqrMagnitude
            )
                return false;

            rotacion = Quaternion.LookRotation(avance, normal);
            return true;
        }

        private static Vector3 Medio(MuestraEstela muestra) =>
            (muestra.BaseHoja + muestra.Punta) * 0.5f;
    }
}
