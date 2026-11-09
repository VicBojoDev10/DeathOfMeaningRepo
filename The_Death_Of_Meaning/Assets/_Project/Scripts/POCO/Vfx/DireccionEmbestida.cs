using UnityEngine;

namespace TDOM.Gameplay.Vfx
{
    public static class DireccionEmbestida
    {
        public static Vector3 Calcular(
            Vector3 desplazamiento,
            Vector3 direccionActual,
            float velocidadMinima,
            float suavizado,
            float dt
        )
        {
            Vector3 plano = new Vector3(desplazamiento.x, 0f, desplazamiento.z);
            float distanciaMinima = velocidadMinima * dt;
            if (dt <= 0f || plano.sqrMagnitude <= distanciaMinima * distanciaMinima)
                return direccionActual;

            float factor = suavizado > 0f ? 1f - Mathf.Exp(-suavizado * dt) : 1f;
            return Vector3.Slerp(direccionActual, plano.normalized, factor).normalized;
        }
    }
}
