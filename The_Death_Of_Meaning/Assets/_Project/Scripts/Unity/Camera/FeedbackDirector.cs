using System.Collections;
using TDOM.Data;
using TDOM.Unity;
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

        public void OnGolpeConectado()
        {
            _cameraRig.Impacto(_config.golpeImpulso);
            StartCoroutine(Hitstop(_config.golpeHitstopFrames));
        }

        public void OnAterrizajeFuerte()
        {
            _cameraRig.Impacto(-_config.aterrizajeImpulso);
        }

        private IEnumerator Hitstop(int frames)
        {
            float escalaOriginal = Time.timeScale;
            Time.timeScale = 0f;

            for (int i = 0; i < frames; i++)
                yield return new WaitForSecondsRealtime(Time.fixedUnscaledDeltaTime);

            Time.timeScale = escalaOriginal;
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
