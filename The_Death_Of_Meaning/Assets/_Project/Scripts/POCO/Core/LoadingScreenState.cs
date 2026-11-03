using System;

namespace TDOM.Gameplay.Core
{
    public sealed class LoadingScreenState
    {
        private const float ProgresoInicial = 0f;
        private const float ProgresoCompleto = 1f;

        private readonly float _tiempoMinimoVisible;
        private float _tiempoVisible;
        private bool _cargaCompleta;

        public LoadingScreenState(float tiempoMinimoVisible) =>
            _tiempoMinimoVisible = Math.Max(0f, tiempoMinimoVisible);

        public bool Visible { get; private set; }
        public float Progreso { get; private set; }

        public event Action Mostrada;
        public event Action Ocultada;

        public void Mostrar()
        {
            _tiempoVisible = 0f;
            _cargaCompleta = false;
            Progreso = ProgresoInicial;

            if (Visible)
                return;

            Visible = true;
            Mostrada?.Invoke();
        }

        public void ReportarProgreso(float progreso)
        {
            if (!Visible || _cargaCompleta)
                return;

            Progreso = Math.Max(Progreso, Math.Clamp(progreso, ProgresoInicial, ProgresoCompleto));
        }

        public void CompletarCarga()
        {
            if (!Visible)
                return;

            _cargaCompleta = true;
            Progreso = ProgresoCompleto;
            IntentarOcultar();
        }

        public void Tick(float dt)
        {
            if (!Visible)
                return;

            _tiempoVisible += dt;
            IntentarOcultar();
        }

        private void IntentarOcultar()
        {
            if (!_cargaCompleta || _tiempoVisible < _tiempoMinimoVisible)
                return;

            Visible = false;
            Ocultada?.Invoke();
        }
    }
}
