using UnityEngine;

namespace TDOM.Data
{
    [CreateAssetMenu(fileName = "SonidosBotonesUI", menuName = "TDOM/Audio/Sonidos Botones UI")]
    public sealed class SonidosBotonesUI : ScriptableObject
    {
        public AudioClip positivo;
        public AudioClip negativo;
        public AudioClip neutral;
        public AudioClip especial;

        [Range(0f, 1f)]
        public float volumen = 0.5f;
    }
}
