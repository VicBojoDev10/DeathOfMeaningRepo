using System.Collections;
using TDOM.Data;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;

namespace TDOM.Unity.Camera
{
    public sealed class FeedbackDirector : MonoBehaviour
    {
        [SerializeField]
        private PlayerCameraRig _cameraRig;

        [SerializeField]
        private CameraFeedbackConfig _config;

        [SerializeField]
        private Animator _animatorAtacante;

        private Coroutine _hitstop;
        private float _velocidadAnimatorBase = 1f;

        private void Awake()
        {
            if (_animatorAtacante == null)
                _animatorAtacante = transform.root.GetComponentInChildren<Animator>();
        }

        private void OnDisable()
        {
            DetenerHitstop();
        }

        public void OnDashAyla()
        {
            _cameraRig.PunchFov(_config.dashFov, _config.dashFovDuracion);
            _cameraRig.Tilt(_config.dashTilt, _config.dashTiltDuracion);
            Rumble(_config.dashRumbleBajo, _config.dashRumbleAlto, _config.dashRumbleDuracion);
        }

        public void OnEmbestidaZendre()
        {
            _cameraRig.PunchFovSostenido(_config.embestidaFov, _config.embestidaDuracion);
        }

        public void OnDobleSalto()
        {
            _cameraRig.Tilt(_config.dobleSaltoTilt, _config.dobleSaltoTiltDuracion);
            Rumble(
                _config.dobleSaltoRumbleBajo,
                _config.dobleSaltoRumbleAlto,
                _config.dobleSaltoRumbleDuracion
            );
        }

        public void OnAtaquePropio()
        {
            IniciarHitstop(_config.golpeHitstopFrames);
        }

        /// <summary>Pulso de zoom (acerca y regresa) en cada golpe de combo, conecte o no.</summary>
        public void OnComboGolpe()
        {
            if (_cameraRig != null)
                _cameraRig.PulsoCombate();
        }

        /// <summary>Zoom-in y zoom-out rapido al conectar un golpe de combo.</summary>
        public void OnComboConectado()
        {
            if (_cameraRig == null || _config == null || _config.comboZoomFov == 0f)
                return;

            _cameraRig.PunchFovSostenido(
                _config.comboZoomFov,
                0f,
                Mathf.Max(0.01f, _config.comboZoomSubida),
                Mathf.Max(0.01f, _config.comboZoomBajada)
            );
        }

        public void OnGolpeRecibido()
        {
            _cameraRig.Impacto(_config.golpeRecibidoImpulso);
        }

        public void OnAterrizajeFuerte()
        {
            _cameraRig.Impacto(-_config.aterrizajeImpulso);
        }

        public void OnCambioDeFase()
        {
            _cameraRig.Impacto(_config.cambioDeFaseImpulso);
        }

        private void IniciarHitstop(int frames)
        {
            if (_animatorAtacante == null || frames <= 0)
                return;

            if (_hitstop != null)
                StopCoroutine(_hitstop);
            else
                _velocidadAnimatorBase = _animatorAtacante.speed;
            _hitstop = StartCoroutine(Hitstop(frames));
        }

        private IEnumerator Hitstop(int frames)
        {
            _animatorAtacante.speed = 0f;
            yield return new WaitForSecondsRealtime(frames * Time.fixedUnscaledDeltaTime);

            RestaurarAnimator();
        }

        private void DetenerHitstop()
        {
            if (_hitstop == null)
            {
                return;
            }
            StopCoroutine(_hitstop);
            RestaurarAnimator();
        }

        private void RestaurarAnimator()
        {
            _hitstop = null;
            if (_animatorAtacante != null)
                _animatorAtacante.speed = _velocidadAnimatorBase;
        }

        public void Rumble(float bajo, float alto, float duracion)
        {
            if (Gamepad.current is not DualShockGamepad pad)
                return;

            pad.SetMotorSpeeds(bajo, alto);
            StartCoroutine(DetenerRumble(pad, duracion));
        }

        private IEnumerator DetenerRumble(DualShockGamepad pad, float duracion)
        {
            yield return new WaitForSeconds(duracion);
            pad.SetMotorSpeeds(0f, 0f);
        }
    }
}
