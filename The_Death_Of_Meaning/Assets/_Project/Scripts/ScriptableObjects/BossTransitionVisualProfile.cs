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
        public AnimationCurve OpacidadHalo = new AnimationCurve();

        [ColorUsage(false, true)]
        public Color ColorRitual = Color.white;

        [Min(0f)]
        public float RadioHalo;

        [Min(0f)]
        public float AlturaHalo;

        [Min(0f)]
        public float IntensidadHalo;

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
