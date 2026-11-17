using TDOM.POCO.ScriptableObjects;
using UnityEngine;
using UnityEngine.Playables;

namespace TDOM.Unity.Flow
{
    public sealed class CinematicOrbit : MonoBehaviour
    {
        [SerializeField]
        private PlayableDirector _director;

        [SerializeField]
        private BossAbsorptionVisualProfile _perfil;

        private Vector3 _posicionInicial;

        private void Awake() => _posicionInicial = transform.localPosition;

        private void Update()
        {
            if (_director == null || _perfil == null || _director.duration <= 0d)
                return;
            float progreso = Mathf.Clamp01((float)(_director.time / _director.duration));
            transform.localRotation = Quaternion.Euler(
                0f,
                _perfil.GiroCamara.Evaluate(progreso),
                0f
            );
            float intensidad = _perfil.SacudidaCamara.Evaluate(progreso);
            float tiempo = (float)_director.time * Mathf.PI * 2f;
            var oscilacion = new Vector3(
                Mathf.Sin(tiempo * _perfil.FrecuenciaSacudida.x),
                Mathf.Sin(tiempo * _perfil.FrecuenciaSacudida.y),
                Mathf.Sin(tiempo * _perfil.FrecuenciaSacudida.z)
            );
            transform.localPosition =
                _posicionInicial + Vector3.Scale(oscilacion, _perfil.AmplitudSacudida) * intensidad;
        }

        private void OnDisable()
        {
            if (Application.isPlaying)
                transform.localPosition = _posicionInicial;
        }
    }
}
