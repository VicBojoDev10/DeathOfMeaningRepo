using UnityEngine;

namespace TDOM.POCO.ScriptableObjects
{
    [CreateAssetMenu(menuName = "TDOM/Boss/Crown Visual")]
    public sealed class BossCrownVisualProfile : ScriptableObject
    {
        public int Fase;
        public Vector3 Origen;
        public Vector3 Cabeza;
        public float Radio;
        public float RadioInterior;
        public float VelocidadGiro;
        public float VelocidadInterior;
        public float Inclinacion;
        public float Pulsacion;
        public float FrecuenciaPulso;
        public float Intensidad;
        public float IntensidadResplandor;
        public float IntensidadAura;
        public float AuraPersistente;
        public Vector3 EscalaAura;

        [ColorUsage(false, true)]
        public Color Color;
        public AnimationCurve Ascenso;
        public AnimationCurve Aparicion;
        public AnimationCurve Expansion;
        public AnimationCurve CargaAura;
    }
}
