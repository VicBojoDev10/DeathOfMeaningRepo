using UnityEngine;

namespace TDOM.Data
{
    public abstract class VfxProfile : ScriptableObject
    {
        [Header("Trayectoria")]
        public Vector3 puntoA = Vector3.zero;
        public Vector3 puntoB = new Vector3(0f, 0f, 2f);
        public float curvatura = 0f;
        public float rotacion = 0f;
        public Vector3 orientacion = Vector3.zero;

        [Header("Tiempo")]
        public float duracion = 0.5f;

        [Header("Material")]
        public Gradient color = new Gradient();
        public AnimationCurve intensidad = AnimationCurve.Constant(0f, 1f, 1f);
        public AnimationCurve disolucion = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Header("Pool")]
        public int tamanoPool = 4;
    }
}
