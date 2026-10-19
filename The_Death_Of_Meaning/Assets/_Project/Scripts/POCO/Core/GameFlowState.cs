using System;

namespace TDOM.Gameplay.Core
{
    public enum EstadoFlujo
    {
        Gameplay,
        Cinematica,
        Transicion,
    }

    public sealed class GameFlowStateMachine
    {
        private readonly float _duracionTransicion;
        private float _restanteTransicion;
        private bool _transicionRetenida;

        public GameFlowStateMachine(float duracionTransicion) =>
            _duracionTransicion = duracionTransicion;

        public EstadoFlujo Estado { get; private set; } = EstadoFlujo.Gameplay;
        public string IdActual { get; private set; }

        public event Action<EstadoFlujo> Cambio;

        public bool IniciarCinematica(string id)
        {
            if (Estado != EstadoFlujo.Gameplay || string.IsNullOrEmpty(id))
                return false;

            IdActual = id;
            CambiarA(EstadoFlujo.Cinematica);
            return true;
        }

        public void TerminarCinematica()
        {
            if (Estado != EstadoFlujo.Cinematica)
                return;

            _restanteTransicion = _duracionTransicion;
            CambiarA(EstadoFlujo.Transicion);
        }

        public void RetenerTransicion() => _transicionRetenida = true;

        public void LiberarTransicion() => _transicionRetenida = false;

        public void Tick(float dt)
        {
            if (Estado != EstadoFlujo.Transicion)
                return;

            _restanteTransicion -= dt;
            if (_restanteTransicion > 0f || _transicionRetenida)
                return;

            IdActual = null;
            CambiarA(EstadoFlujo.Gameplay);
        }

        private void CambiarA(EstadoFlujo nuevo)
        {
            Estado = nuevo;
            Cambio?.Invoke(nuevo);
        }
    }
}
