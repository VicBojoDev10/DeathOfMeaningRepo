using UnityEngine;

namespace TDOM.Data
{
    [CreateAssetMenu(fileName = "Sfx_", menuName = "TDOM/Audio/Sfx Banco")]
    public sealed class SfxBanco : ScriptableObject
    {
        public AudioClip[] clips = new AudioClip[0];

        [Range(0f, 1f)]
        public float volumen = 1f;

        [Range(0f, 0.5f)]
        public float variacionVolumen = 0.05f;

        [Range(0.1f, 3f)]
        public float pitch = 1f;

        [Range(0f, 0.5f)]
        public float variacionPitch = 0.05f;

        [Min(0f)]
        public float retraso;

        public bool loop;

        [Min(0f)]
        public float duracionSubida;

        [Range(0f, 1f)]
        public float volumenInicialSubida = 0.3f;

        [Range(0.1f, 3f)]
        public float pitchInicialSubida = 0.8f;

        [Range(0f, 1f)]
        public float mezcla3D = 1f;

        [Min(0.01f)]
        public float distanciaMinima = 2f;

        [Min(0.01f)]
        public float distanciaMaxima = 40f;

        public AudioRolloffMode atenuacion = AudioRolloffMode.Logarithmic;

        [Min(1)]
        public int maximoVoces = 4;
    }
}
