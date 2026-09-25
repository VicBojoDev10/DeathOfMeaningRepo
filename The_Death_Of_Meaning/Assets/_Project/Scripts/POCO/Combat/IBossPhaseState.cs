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

        public BossPhaseStateMachine(
            BossPhaseProfile[] fases,
            float cooldownSegundos = 0f)
        {
            if (fases == null)
                throw new ArgumentNullException(nameof(fases));

            if (fases.Length == 0)
                throw new ArgumentException(
                    "Debe existir al menos una fase.",
                    nameof(fases));

            _fases = fases;
            _cooldown = new CooldownTimer(cooldownSegundos);

            _faseActual = 0;
            _indiceAtaque = 0;
        }

        public BossAttackStep AtaqueActual =>
            _fases[_faseActual].OrdenDeAtaque[_indiceAtaque];

        public int FaseActual => _faseActual + 1;

        public void Tick(float dt)
        {
            _cooldown.Tick(dt);
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
            _vidaActual -= daño;

            if (_faseActual + 1 < _fases.Length &&
                _vidaActual <= _fases[_faseActual].VidaTransicion)
            {
                _faseActual++;
                _indiceAtaque = 0;
            }
        }
    }
}