using UnityEngine;

namespace TDOM.Data
{
    [CreateAssetMenu(fileName = "CameraFeedbackConfig", menuName = "TDOM/Camera Feedback Config")]
    public sealed class CameraFeedbackConfig : ScriptableObject
    {
        [Header("Dash de Ayla")]
        public float dashTilt = 5f;
        public float dashTiltDuracion = 0.25f;
        public float dashRumbleBajo = 0.3f;
        public float dashRumbleAlto = 0.6f;
        public float dashRumbleDuracion = 0.1f;

        [Header("Doble salto")]
        public float dobleSaltoTilt = 2f;
        public float dobleSaltoTiltDuracion = 0.15f;
        public float dobleSaltoRumbleBajo = 0.15f;
        public float dobleSaltoRumbleAlto = 0.15f;
        public float dobleSaltoRumbleDuracion = 0.05f;

        [Header("Golpe conectado y disparo")]
        public int golpeHitstopFrames = 3;

        [Header("Golpe de combo conectado (zoom)")]
        [Tooltip("Grados de FOV del zoom-in (negativo = acerca). 0 = sin zoom.")]
        public float comboZoomFov = -4f;

        [Tooltip("Segundos que tarda en llegar al zoom maximo.")]
        public float comboZoomSubida = 0.05f;

        [Tooltip("Segundos que tarda en volver al FOV normal.")]
        public float comboZoomBajada = 0.14f;

        [Header("Golpe recibido de un enemigo")]
        public float golpeRecibidoImpulso = 1f;

        [Header("Shake de aterrizaje")]
        [Tooltip(
            "Intensidad base al alcanzar la velocidad mínima; reducida a la mitad del valor anterior."
        )]
        [Min(0f)]
        public float aterrizajeImpulso = 0.2f;

        [Tooltip("Velocidad vertical descendente mínima, en m/s, para generar shake.")]
        [Min(0f)]
        public float aterrizajeVelocidadMinima = 16f;

        [Tooltip("Velocidad descendente a la que se alcanza el tope, en m/s.")]
        [Min(0.01f)]
        public float aterrizajeVelocidadMaxima = 35f;

        [Tooltip("Tope de intensidad del shake para caídas rápidas.")]
        [Min(0f)]
        public float aterrizajeImpulsoMaximo = 0.4f;

        [Tooltip("Duración de la sacudida, en segundos.")]
        [Min(0.01f)]
        public float aterrizajeDuracion = 0.14f;

        [Header("Cambio de fase del jefe")]
        public float cambioDeFaseImpulso = 3f;

        [Header("Zoom de dash de Ayla (metros de acercamiento)")]
        public float zoomAmount = 0.9f;
        public float tiempoEntrada = 0.06f;
        public float tiempoSalida = 0.3f;
        public AnimationCurve curvaZoom = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Zoom de embestida de Zendre (metros de acercamiento)")]
        public float embestidaZoomAmount = 1.2f;
        public float embestidaTiempoEntrada = 0.08f;
        public float embestidaTiempoSalida = 0.4f;
        public AnimationCurve embestidaCurvaZoom = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Accesibilidad")]
        [Range(0f, 1f)]
        public float multiplicadorEfectosCamara = 1f;
    }
}
