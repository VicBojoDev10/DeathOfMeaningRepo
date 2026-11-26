using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace TDOM.Unity.Flow
{
    public sealed class CinematicaVideo : MonoBehaviour
    {
        [SerializeField]
        private VideoClip[] _clips = new VideoClip[0];

        [SerializeField]
        private VideoPlayer _reproductor;

        [SerializeField]
        private RawImage _pantalla;

        [SerializeField]
        private CanvasGroup _negro;

        [SerializeField, Min(0.01f)]
        private float _duracionFade = 0.6f;

        public double Duracion
        {
            get
            {
                double total = 0d;
                foreach (var clip in _clips)
                    if (clip != null)
                        total += clip.length;
                return total;
            }
        }

        private void Awake()
        {
            _negro.alpha = 0f;
            _negro.blocksRaycasts = false;
            _pantalla.enabled = false;
        }

        public void Reproducir(double desfase)
        {
            Detener();
            StartCoroutine(ReproducirSecuencia(desfase));
        }

        public void Detener()
        {
            StopAllCoroutines();
            _reproductor.Stop();
            _pantalla.enabled = false;
        }

        public void Desvanecer()
        {
            Detener();
            StartCoroutine(Fundir(0f));
        }

        private IEnumerator ReproducirSecuencia(double desfase)
        {
            yield return Fundir(1f);
            double restante = desfase;
            foreach (var clip in _clips)
            {
                if (clip == null)
                    continue;
                if (restante >= clip.length)
                {
                    restante -= clip.length;
                    continue;
                }
                yield return ReproducirClip(clip, restante);
                restante = 0d;
            }
            _pantalla.enabled = false;
        }

        private IEnumerator ReproducirClip(VideoClip clip, double desde)
        {
            _reproductor.clip = clip;
            _reproductor.Prepare();
            while (!_reproductor.isPrepared)
                yield return null;
            _reproductor.time = desde;
            _reproductor.Play();
            _pantalla.enabled = true;
            while (_reproductor.isPlaying)
                yield return null;
            _reproductor.Stop();
        }

        private IEnumerator Fundir(float destino)
        {
            float inicio = _negro.alpha;
            for (float t = 0f; t < _duracionFade; t += Time.unscaledDeltaTime)
            {
                _negro.alpha = Mathf.Lerp(inicio, destino, t / _duracionFade);
                yield return null;
            }
            _negro.alpha = destino;
        }
    }
}
