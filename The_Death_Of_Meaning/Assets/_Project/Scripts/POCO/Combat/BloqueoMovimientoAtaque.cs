namespace TDOM.Gameplay.Combat
{
    /// <summary>
    /// Decide cuándo el jefe puede rotar o trasladarse. Desde que empieza un ataque (aviso
    /// incluido) hasta que termina su flujo completo el jefe no se mueve, y al terminar espera
    /// <see cref="EsperaTrasAtaque"/> segundos más antes de reanudar. Cuenta ataques activos
    /// (<see cref="Iniciar"/> / <see cref="Terminar"/>) por si se solapan, y tiene un tiempo
    /// máximo de seguridad por si un ataque nunca avisa de que terminó.
    /// </summary>
    public sealed class BloqueoMovimientoAtaque
    {
        public const float EsperaPorDefecto = 0.5f;

        private int _activos;
        private float _tiempoEnAtaque;
        private float _esperaRestante;

        /// <param name="esperaTrasAtaque">Segundos de espera tras el último ataque.</param>
        /// <param name="tiempoMaximoAtaque">
        /// Segundos tras los que se da por terminado un ataque que no avisó de su fin.
        /// 0 o menos desactiva el límite.
        /// </param>
        public BloqueoMovimientoAtaque(
            float esperaTrasAtaque = EsperaPorDefecto,
            float tiempoMaximoAtaque = 0f
        )
        {
            EsperaTrasAtaque = esperaTrasAtaque < 0f ? 0f : esperaTrasAtaque;
            TiempoMaximoAtaque = tiempoMaximoAtaque < 0f ? 0f : tiempoMaximoAtaque;
        }

        public float EsperaTrasAtaque { get; }
        public float TiempoMaximoAtaque { get; }

        /// <summary>Hay al menos un ataque en curso.</summary>
        public bool EnAtaque => _activos > 0;

        public int AtaquesActivos => _activos;
        public float EsperaRestante => _esperaRestante;

        /// <summary>El jefe puede rotar o trasladarse: sin ataque y sin espera pendiente.</summary>
        public bool PuedeMoverse => _activos == 0 && _esperaRestante <= 0f;

        /// <summary>Un ataque empieza. Cancela cualquier espera pendiente.</summary>
        public void Iniciar()
        {
            if (_activos == 0)
            {
                _tiempoEnAtaque = 0f;
            }

            _activos++;
            _esperaRestante = 0f;
        }

        /// <summary>
        /// Un ataque termina. Al terminar el último arranca la espera. Ignorado si no hay
        /// ataques activos (por ejemplo, tras un timeout).
        /// </summary>
        public void Terminar()
        {
            if (_activos == 0)
            {
                return;
            }

            _activos--;
            if (_activos == 0)
            {
                ArrancarEspera();
            }
        }

        /// <summary>
        /// Avanza el tiempo. Devuelve true si en este tick saltó el tiempo máximo de seguridad
        /// y se liberaron los ataques activos (la espera posterior sí se aplica).
        /// </summary>
        public bool Tick(float dt)
        {
            if (dt <= 0f)
            {
                return false;
            }

            if (_activos > 0)
            {
                _tiempoEnAtaque += dt;
                if (TiempoMaximoAtaque > 0f && _tiempoEnAtaque >= TiempoMaximoAtaque)
                {
                    _activos = 0;
                    ArrancarEspera();
                    return true;
                }

                return false;
            }

            if (_esperaRestante > 0f)
            {
                _esperaRestante -= dt;
                if (_esperaRestante < 0f)
                {
                    _esperaRestante = 0f;
                }
            }

            return false;
        }

        /// <summary>Olvida todo: sin ataques ni espera (reinicio de partida).</summary>
        public void Reiniciar()
        {
            _activos = 0;
            _tiempoEnAtaque = 0f;
            _esperaRestante = 0f;
        }

        private void ArrancarEspera()
        {
            _tiempoEnAtaque = 0f;
            _esperaRestante = EsperaTrasAtaque;
        }
    }
}
