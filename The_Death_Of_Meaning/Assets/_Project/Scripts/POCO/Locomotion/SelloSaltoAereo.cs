using System;

namespace TDOM.Gameplay
{
    public enum EstadoSelloSalto : byte
    {
        Libre,
        Sellando,
        Sellado,
        Rompiendo,
    }

    public sealed class SelloSaltoAereo
    {
        private readonly float _duracionSellado;
        private readonly float _duracionRuptura;
        private readonly float _multiplicadorLentitud;
        private float _tiempoEnEstado;
        private bool _yaRoto;

        public SelloSaltoAereo(
            float duracionSellado,
            float duracionRuptura,
            float multiplicadorLentitud
        )
        {
            _duracionSellado = Math.Max(0f, duracionSellado);
            _duracionRuptura = Math.Max(0f, duracionRuptura);
            _multiplicadorLentitud = Math.Clamp(multiplicadorLentitud, 0f, 1f);
        }

        public EstadoSelloSalto Estado { get; private set; } = EstadoSelloSalto.Libre;

        public bool PermiteSaltoAereo =>
            Estado == EstadoSelloSalto.Libre || Estado == EstadoSelloSalto.Rompiendo;

        public float MultiplicadorVelocidad =>
            Estado == EstadoSelloSalto.Sellando ? _multiplicadorLentitud : 1f;

        public float Progreso =>
            Estado switch
            {
                EstadoSelloSalto.Sellando => Fraccion(_duracionSellado),
                EstadoSelloSalto.Rompiendo => Fraccion(_duracionRuptura),
                EstadoSelloSalto.Sellado => 1f,
                _ => 0f,
            };

        public bool Sellar()
        {
            if (_yaRoto || Estado != EstadoSelloSalto.Libre)
                return false;
            Cambiar(EstadoSelloSalto.Sellando);
            return true;
        }

        public bool Romper()
        {
            if (Estado != EstadoSelloSalto.Sellando && Estado != EstadoSelloSalto.Sellado)
                return false;
            _yaRoto = true;
            Cambiar(EstadoSelloSalto.Rompiendo);
            return true;
        }

        public void Reiniciar()
        {
            _yaRoto = false;
            Cambiar(EstadoSelloSalto.Libre);
        }

        public void Tick(float dt)
        {
            _tiempoEnEstado += Math.Max(0f, dt);
            if (Estado == EstadoSelloSalto.Sellando && _tiempoEnEstado >= _duracionSellado)
                Cambiar(EstadoSelloSalto.Sellado);
            else if (Estado == EstadoSelloSalto.Rompiendo && _tiempoEnEstado >= _duracionRuptura)
                Cambiar(EstadoSelloSalto.Libre);
        }

        private void Cambiar(EstadoSelloSalto estado)
        {
            Estado = estado;
            _tiempoEnEstado = 0f;
        }

        private float Fraccion(float duracion) =>
            duracion <= 0f ? 1f : Math.Clamp(_tiempoEnEstado / duracion, 0f, 1f);
    }
}
