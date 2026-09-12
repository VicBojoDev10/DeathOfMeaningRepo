using UnityEngine;

namespace TDOM.Data
{
    [CreateAssetMenu(fileName = "CameraFeedbackConfig", menuName = "TDOM/Camera Feedback Config")]
    public sealed class CameraFeedbackConfig : ScriptableObject
    {
        [Header("Dash de Ayla (punch)")]
        public float dashFov = 15f;
        public float dashFovDuracion = 0.25f;
        public float dashTilt = 5f;
        public float dashTiltDuracion = 0.25f;
        public float dashRumbleBajo = 0.3f;
        public float dashRumbleAlto = 0.6f;
        public float dashRumbleDuracion = 0.1f;

        [Header("Embestida de Zendre (sostenido)")]
        public float embestidaFov = 10f;
        public float embestidaDuracion = 0.8f;

        [Header("Doble salto")]
        public float dobleSaltoTilt = 2f;
        public float dobleSaltoTiltDuracion = 0.15f;
        public float dobleSaltoRumbleBajo = 0.15f;
        public float dobleSaltoRumbleAlto = 0.15f;
        public float dobleSaltoRumbleDuracion = 0.05f;

        [Header("Golpe conectado")]
        public float golpeImpulso = 0.5f;
        public int golpeHitstopFrames = 3;

        [Header("Aterrizaje fuerte")]
        public float aterrizajeImpulso = 0.4f;
    }
}
