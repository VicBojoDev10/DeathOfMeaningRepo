using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

namespace TDOM.Unity.Camera
{
    public sealed class PlayerCameraRig : MonoBehaviour
    {
        [SerializeField]
        private CinemachineCamera _camara;

        [SerializeField]
        private CinemachineImpulseSource _impulso;

        private float _fovBase;
        private float _fovExtra;
        private float _roll;

        private Coroutine _rutinaFovActual;
        private Coroutine _rutinaTiltActual;

        private void Awake()
        {
            if (_camara != null)
                _fovBase = _camara.Lens.FieldOfView;
        }

        public void ApplyLook(float yaw, float pitch)
        {
            transform.rotation = Quaternion.Euler(pitch, yaw, _roll);
        }

        public void PunchFov(float grados, float duracion)
        {
            if (_rutinaFovActual != null)
                StopCoroutine(_rutinaFovActual);

            _rutinaFovActual = StartCoroutine(RutinaFov(grados, duracion));
        }

        public void Tilt(float grados, float duracion)
        {
            if (_rutinaTiltActual != null)
                StopCoroutine(_rutinaTiltActual);

            _rutinaTiltActual = StartCoroutine(RutinaTilt(grados, duracion));
        }

        public void Impacto(float fuerza) => _impulso.GenerateImpulse(fuerza);

        private IEnumerator RutinaFov(float grados, float duracion)
        {
            // Sube rápido y regresa suave (como pide el ticket para el dash: "punch")
            float tiempoTranscurrido = 0f;
            _fovExtra = grados;
            AplicarFov();

            while (tiempoTranscurrido < duracion)
            {
                tiempoTranscurrido += Time.deltaTime;
                float t = Mathf.Clamp01(tiempoTranscurrido / duracion);
                _fovExtra = Mathf.Lerp(grados, 0f, t);
                AplicarFov();
                yield return null;
            }

            _fovExtra = 0f;
            AplicarFov();
            _rutinaFovActual = null;
        }

        private void AplicarFov()
        {
            if (_camara != null)
            {
                var lente = _camara.Lens;
                lente.FieldOfView = _fovBase + _fovExtra;
                _camara.Lens = lente;
            }
        }

        private IEnumerator RutinaTilt(float grados, float duracion)
        {
            float tiempoTranscurrido = 0f;
            _roll = grados;

            while (tiempoTranscurrido < duracion)
            {
                tiempoTranscurrido += Time.deltaTime;
                float t = Mathf.Clamp01(tiempoTranscurrido / duracion);
                _roll = Mathf.Lerp(grados, 0f, t);
                yield return null;
            }

            _roll = 0f;
            _rutinaTiltActual = null;
        }
    }
}
