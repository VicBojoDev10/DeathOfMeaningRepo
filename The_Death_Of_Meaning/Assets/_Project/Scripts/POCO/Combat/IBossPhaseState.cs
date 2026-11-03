using System;
using TDOM.Contracts;
using TDOM.Gameplay.Core;
using TDOM.POCO.ScriptableObjects;

namespace TDOM.Gameplay.Combat
{
    public interface IBossPhaseState
    {
        BossAttackStep AtaqueActual { get; }
        BossAttackStep SiguienteAtaque();
        void AplicarDaño(float daño);
        void Tick(float dt);
        int FaseActual { get; }
    }

    public sealed class BossPhaseStateMachine : IBossPhaseState
    {
        private readonly BossPhaseProfile[] _fases;
        private readonly CooldownTimer _cooldown;

        private int _faseActual;
        private int _indiceAtaque;
        private float _vidaActual = 1f;
        private float _tiempoTransicion;
        private bool _muerto;

        /// <summary>Se dispara en Tick cuando termina la transición de fase.</summary>
        public event Action TransicionTerminada;

        public event Action<float> VidaCambiada;

        public event Action Muerto;

        public BossPhaseStateMachine(BossPhaseProfile[] fases, float cooldownSegundos = 0f)
        {
            if (fases == null)
                throw new ArgumentNullException(nameof(fases));

            if (fases.Length == 0)
                throw new ArgumentException("Debe existir al menos una fase.", nameof(fases));

            _fases = fases;
            _cooldown = new CooldownTimer(cooldownSegundos);

            _faseActual = 0;
            _indiceAtaque = 0;
        }

        public BossAttackStep AtaqueActual => _fases[_faseActual].OrdenDeAtaque[_indiceAtaque];

        public int FaseActual => _faseActual + 1;

        /// <summary>True durante la transición de fase: el daño se ignora.</summary>
        public bool EnTransicion => _tiempoTransicion > 0f;

        public float TiempoTransicionRestante => _tiempoTransicion;

        /// <summary>Vida normalizada (1 = llena).</summary>
        public float VidaActual => _vidaActual;

        public bool EstaMuerto => _muerto;

        private bool EnUltimaFase => _faseActual + 1 >= _fases.Length;

        public void Tick(float dt)
        {
            _cooldown.Tick(dt);

            if (!EnTransicion || dt <= 0f)
                return;

            _tiempoTransicion = Math.Max(0f, _tiempoTransicion - dt);
            if (!EnTransicion)
                TransicionTerminada?.Invoke();
        }

        public BossAttackStep SiguienteAtaque()
        {
            // Si hay cooldown y aún no termina,
            // no se avanza el patrón.
            if (!_cooldown.Listo)
            {
                return AtaqueActual;
            }

            // Reinicia cooldown sólo si existe.
            if (_cooldown.Normalizado >= 0f)
            {
                _cooldown.Disparar();
            }

            _indiceAtaque++;

            if (_indiceAtaque >= _fases[_faseActual].OrdenDeAtaque.Length)
            {
                _indiceAtaque = 0;
            }

            return AtaqueActual;
        }

        public void AplicarDaño(float daño)
        {
            if (EnTransicion || _muerto || daño <= 0f)
                return;

            _vidaActual = Math.Max(0f, _vidaActual - daño);

            float umbral = _fases[_faseActual].VidaTransicion;
            if (!EnUltimaFase && _vidaActual <= umbral)
            {
                // El daño que sobra no arrastra a la siguiente fase.
                _vidaActual = umbral;
                _faseActual++;
                _indiceAtaque = 0;
                _tiempoTransicion = Math.Max(0f, _fases[_faseActual].DuracionTransicion);
            }

            VidaCambiada?.Invoke(_vidaActual);
            ComprobarMuerte();
        }

        private void ComprobarMuerte()
        {
            if (!EnUltimaFase || _vidaActual > 0f)
                return;

            _muerto = true;
            Muerto?.Invoke();
        }
    }
}
