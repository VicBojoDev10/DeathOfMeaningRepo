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

    public sealed class BossAttackScheduler
    {
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

        public bool EnTelegraph => _enTelegraph;

        public BossAttackKind AtaqueActual =>
            _enTelegraph ? _ataqueEnCurso : _maquina.AtaqueActual.Tipo;

        public float TiempoRestante => Math.Max(0f, DuracionEtapaActual - _tiempo);

        private float DuracionEtapaActual =>
            _enTelegraph ? _duracionTelegraph : _cooldownEntreAtaques;

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

        public void ReiniciarPorCambioDeFase()
        {
            _tiempo = 0f;
            _enTelegraph = false;
        }

        public void Interrumpir(float espera)
        {
            _tiempo = Math.Max(0f, espera);
            _enTelegraph = false;
        }
    }
}
