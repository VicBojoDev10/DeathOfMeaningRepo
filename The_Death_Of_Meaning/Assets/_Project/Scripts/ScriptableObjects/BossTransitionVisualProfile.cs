using UnityEngine;

namespace TDOM.POCO.ScriptableObjects
{
    [CreateAssetMenu(menuName = "TDOM/Boss/Transition Visual")]
    public sealed class BossTransitionVisualProfile : ScriptableObject
    {
        [Min(1)]
        public int Fase;
        public AnimationCurve Carga = new AnimationCurve();
        public AnimationCurve MezclaColor = new AnimationCurve();
        public AnimationCurve OpacidadOnda = new AnimationCurve();
        public AnimationCurve ExpansionOnda = new AnimationCurve();
        public AnimationCurve TrazoRuna = new AnimationCurve();
        public AnimationCurve OpacidadRuna = new AnimationCurve();

        [Min(0f)]
        public float RadioRuna;

        [Min(0f)]
        public float IntensidadRuna;

        [Min(0f)]
        public float IntensidadContorno;

        [Min(0f)]
        public float Vibracion;

        [Min(0f)]
        public float RadioOnda;
    }
}
