using System;
using TDOM.Contracts;

namespace TDOM.Gameplay.Combat
{
    public enum EventoAtaque
    {
        Ninguno,
        Telegraph,
        Impacto,
    }

    /// <summary>
    /// Decide cuándo ataca AREK, en POCO puro (sin UnityEngine ni red).
    /// Ciclo: esperar cooldown → Telegraph(AtaqueActual) → esperar duracionTelegraph →
    /// Impacto(mismo ataque) → maquina.SiguienteAtaque() → repetir.
    /// Qué ataque toca lo decide la máquina de fases; este scheduler solo lleva los tiempos.
    /// </summary>
    public sealed class BossAttackScheduler
    {
        // Margen para comparar tiempos acumulados con float, para que 1/60 y 1/30
        // disparen los eventos en el mismo orden justo en los límites (3 s, 4.25 s...).
        private const float Tolerancia = 1e-4f;

        private readonly IBossPhaseState _maquina;
        private readonly float _cooldownEntreAtaques;
        private readonly float _duracionTelegraph;

        private float _tiempo;
        private bool _enTelegraph;
        private BossAttackKind _ataqueEnCurso;

        public BossAttackScheduler(
            IBossPhaseState maquina,
            float cooldownEntreAtaques = 3f,
            float duracionTelegraph = 1.25f
        )
        {
            _maquina = maquina ?? throw new ArgumentNullException(nameof(maquina));

            if (cooldownEntreAtaques < 0f)
                throw new ArgumentOutOfRangeException(
                    nameof(cooldownEntreAtaques),
                    "No puede ser negativo."
                );
            if (duracionTelegraph <= 0f)
                throw new ArgumentOutOfRangeException(
                    nameof(duracionTelegraph),
                    "Debe ser mayor que 0."
                );

            _cooldownEntreAtaques = cooldownEntreAtaques;
            _duracionTelegraph = duracionTelegraph;
        }

        /// <summary>True entre el Telegraph y el Impacto de un ataque.</summary>
        public bool EnTelegraph => _enTelegraph;

        /// <summary>El ataque avisado (si hay telegraph en curso) o el que viene.</summary>
        public BossAttackKind AtaqueActual =>
            _enTelegraph ? _ataqueEnCurso : _maquina.AtaqueActual.Tipo;

        /// <summary>Segundos que faltan para el próximo evento (Telegraph o Impacto).</summary>
        public float TiempoRestante => Math.Max(0f, DuracionEtapaActual - _tiempo);

        private float DuracionEtapaActual =>
            _enTelegraph ? _duracionTelegraph : _cooldownEntreAtaques;

        /// <summary>
        /// Avanza el tiempo y devuelve a lo mucho un evento por llamada. El tiempo sobrante
        /// se conserva para el siguiente tramo, así el ritmo no depende del framerate.
        /// </summary>
        public EventoAtaque Tick(float dt, out BossAttackKind ataque)
        {
            if (dt > 0f)
                _tiempo += dt;

            ataque = AtaqueActual;

            if (_tiempo < DuracionEtapaActual - Tolerancia)
                return EventoAtaque.Ninguno;

            _tiempo = Math.Max(0f, _tiempo - DuracionEtapaActual);

            if (!_enTelegraph)
            {
                _ataqueEnCurso = _maquina.AtaqueActual.Tipo;
                _enTelegraph = true;
                ataque = _ataqueEnCurso;
                return EventoAtaque.Telegraph;
            }

            _enTelegraph = false;
            ataque = _ataqueEnCurso;
            _maquina.SiguienteAtaque();
            return EventoAtaque.Impacto;
        }

        /// <summary>
        /// Vuelve a empezar desde el cooldown. Si había un telegraph en curso, su impacto se
        /// cancela. El índice del ataque ya lo reinicia la máquina al cambiar de fase.
        /// </summary>
        public void ReiniciarPorCambioDeFase()
        {
            _tiempo = 0f;
            _enTelegraph = false;
        }
    }
}
