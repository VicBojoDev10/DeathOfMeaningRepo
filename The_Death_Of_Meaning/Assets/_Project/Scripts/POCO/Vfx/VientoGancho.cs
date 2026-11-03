using UnityEngine;

namespace TDOM.Gameplay.Vfx
{
    /// <summary>
    /// Intensidad del viento del gancho (TW-255). Mientras el jalon esta activo sube hacia
    /// velocidad / velocidadReferencia (0 a 1) y, al soltarse, baja gradualmente a 0 en vez de
    /// cortarse. No guarda estado del gancho: quien lo usa le pasa si el jalon esta activo.
    /// </summary>
    public sealed class VientoGancho
    {
        /// <summary>Por debajo de este valor el efecto ya no se ve y puede apagarse.</summary>
        public const float UmbralVisible = 0.001f;

        private readonly float _velocidadReferencia;
        private readonly float _tiempoSubida;
        private readonly float _tiempoBajada;

        public VientoGancho(float velocidadReferencia, float tiempoSubida, float tiempoBajada)
        {
            _velocidadReferencia = velocidadReferencia;
            _tiempoSubida = tiempoSubida;
            _tiempoBajada = tiempoBajada;
        }

        /// <summary>Intensidad actual, de 0 a 1.</summary>
        public float Intensidad { get; private set; }

        public bool Visible => Intensidad > UmbralVisible;

        public float Actualizar(bool activo, float velocidad, float dt)
        {
            float objetivo = activo ? ObjetivoPorVelocidad(velocidad) : 0f;
            float tiempo = objetivo > Intensidad ? _tiempoSubida : _tiempoBajada;
            Intensidad =
                tiempo <= 0f
                    ? objetivo
                    : Mathf.MoveTowards(Intensidad, objetivo, Mathf.Max(0f, dt) / tiempo);
            return Intensidad;
        }

        public void Reiniciar()
        {
            Intensidad = 0f;
        }

        private float ObjetivoPorVelocidad(float velocidad) =>
            _velocidadReferencia <= 0f ? 1f : Mathf.Clamp01(velocidad / _velocidadReferencia);
    }
}
