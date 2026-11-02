using TDOM.Contracts;
using UnityEngine;

namespace TDOM.Gameplay.Locomotion
{
    public sealed class ClimbResolver
    {
        private readonly float _velocidad;
        private readonly float _distanciaMaximaDeAproximacion;
        private readonly float _alturaMinima;
        private readonly float _alturaMaxima;
        private readonly float _duracionMaxima;
        private Vector3 _posicionInicial;
        private Vector3 _posicionFinal;
        private float _duracion;
        private float _tiempoTranscurrido;

        public bool Activo { get; private set; }

        public ClimbResolver(
            float velocidad,
            float distanciaMaximaDeAproximacion,
            float alturaMinima,
            float alturaMaxima,
            float duracionMaxima
        )
        {
            _velocidad = velocidad;
            _distanciaMaximaDeAproximacion = distanciaMaximaDeAproximacion;
            _alturaMinima = alturaMinima;
            _alturaMaxima = alturaMaxima;
            _duracionMaxima = duracionMaxima;
        }

        public bool IntentarIniciar(
            Vector3 posicionActual,
            Vector3 posicionSuperior,
            float distanciaDeAproximacion,
            bool acercandose,
            bool bordeMarcado
        )
        {
            if (
                Activo
                || !acercandose
                || !bordeMarcado
                || _velocidad <= 0f
                || _duracionMaxima <= 0f
                || distanciaDeAproximacion < 0f
                || distanciaDeAproximacion > _distanciaMaximaDeAproximacion
            )
                return false;

            float altura = posicionSuperior.y - posicionActual.y;
            if (altura < _alturaMinima || altura > _alturaMaxima)
                return false;

            float distancia = Vector3.Distance(posicionActual, posicionSuperior);
            _duracion = Mathf.Min(distancia / _velocidad, _duracionMaxima);
            if (_duracion <= 0f)
                return false;

            _posicionInicial = posicionActual;
            _posicionFinal = posicionSuperior;
            _tiempoTranscurrido = 0f;
            Activo = true;
            return true;
        }

        public MotionIntent Tick(LocomotionState estado, Vector3 posicionActual, float dt)
        {
            if (!Activo)
                return new MotionIntent(estado.Velocity);

            if (dt <= 0f)
            {
                estado.Phase = LocomotionPhase.Climbing;
                return new MotionIntent(Vector3.zero, ignoreGravity: true);
            }

            float tiempoSiguiente = _tiempoTranscurrido + dt;
            _tiempoTranscurrido =
                tiempoSiguiente >= _duracion || Mathf.Approximately(tiempoSiguiente, _duracion)
                    ? _duracion
                    : tiempoSiguiente;
            float progreso = Mathf.Clamp01(_tiempoTranscurrido / _duracion);
            Vector3 posicionDeseada = Vector3.Lerp(_posicionInicial, _posicionFinal, progreso);
            Vector3 velocidad = (posicionDeseada - posicionActual) / dt;

            estado.Velocity = velocidad;
            estado.Phase = LocomotionPhase.Climbing;

            if (progreso >= 1f)
            {
                Activo = false;
                estado.IsGrounded = true;
                estado.Velocity = Vector3.zero;
                estado.Phase = LocomotionPhase.Grounded;
            }

            return new MotionIntent(velocidad, ignoreGravity: true);
        }

        public void Cancelar() => Activo = false;
    }
}
