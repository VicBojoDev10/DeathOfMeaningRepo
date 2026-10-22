using UnityEngine;

namespace TDOM.Gameplay.Vfx
{
    public static class TrayectoriaVfx
    {
        private const float Minimo = 1e-6f;

        public static Vector3 Normal(Vector3 a, Vector3 b, Vector3 arriba, float rotacionGrados)
        {
            Vector3 eje = b - a;
            if (eje.sqrMagnitude < Minimo)
                return arriba.sqrMagnitude < Minimo ? Vector3.up : arriba.normalized;

            Vector3 direccion = eje.normalized;
            Vector3 normal = Vector3.ProjectOnPlane(arriba, direccion);
            if (normal.sqrMagnitude < Minimo)
                normal = Vector3.ProjectOnPlane(Vector3.right, direccion);
            if (normal.sqrMagnitude < Minimo)
                normal = Vector3.ProjectOnPlane(Vector3.forward, direccion);

            return Quaternion.AngleAxis(rotacionGrados, direccion) * normal.normalized;
        }

        public static Vector3 Control(
            Vector3 a,
            Vector3 b,
            Vector3 arriba,
            float curvatura,
            float rotacionGrados
        )
        {
            Vector3 medio = (a + b) * 0.5f;
            float distancia = Vector3.Distance(a, b);
            return medio + Normal(a, b, arriba, rotacionGrados) * (curvatura * distancia);
        }

        public static float Curvatura(
            Vector3 a,
            Vector3 b,
            Vector3 arriba,
            float rotacionGrados,
            Vector3 control
        )
        {
            float distancia = Vector3.Distance(a, b);
            if (distancia < Minimo)
                return 0f;

            Vector3 medio = (a + b) * 0.5f;
            return Vector3.Dot(control - medio, Normal(a, b, arriba, rotacionGrados)) / distancia;
        }

        public static Vector3 Punto(Vector3 a, Vector3 control, Vector3 b, float t)
        {
            t = Mathf.Clamp01(t);
            float u = 1f - t;
            return u * u * a + 2f * u * t * control + t * t * b;
        }

        public static Vector3 Tangente(Vector3 a, Vector3 control, Vector3 b, float t)
        {
            t = Mathf.Clamp01(t);
            return 2f * (1f - t) * (control - a) + 2f * t * (b - control);
        }
    }
}
