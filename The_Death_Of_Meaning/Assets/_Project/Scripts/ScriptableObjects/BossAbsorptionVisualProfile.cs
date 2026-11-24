using UnityEngine;

namespace TDOM.POCO.ScriptableObjects
{
    [CreateAssetMenu(menuName = "TDOM/Boss/Absorption Visual")]
    public sealed class BossAbsorptionVisualProfile : ScriptableObject
    {
        public int Fase;
        public Vector3 CentroOrbita;
        public Vector3 DestinoAbsorcion;
        public float RadioOrbita;
        public float SeparacionVertical;
        public float Vueltas;
        public float TamanoSimbolo;
        public float DesfaseAbsorcion;
        public float IntensidadSimbolos;
        public float RadioRuna;
        public float AlturaFragmentos;
        public float GiroFragmentos;
        public float IntensidadRuna;
        public Vector3 EscalaAura;
        public float IntensidadAura;
        public Color ColorRuna;
        public Gradient ColorAura;
        public AnimationCurve AparicionSimbolos;
        public AnimationCurve Absorcion;
        public AnimationCurve OpacidadSimbolos;
        public AnimationCurve Ruptura;
        public AnimationCurve OpacidadRuna;
        public AnimationCurve CargaAura;
        public AnimationCurve MezclaColor;
        public AnimationCurve GiroCamara;
        public AnimationCurve SacudidaCamara = new AnimationCurve();
        public Vector3 AmplitudSacudida;
        public Vector3 FrecuenciaSacudida;
    }
}
