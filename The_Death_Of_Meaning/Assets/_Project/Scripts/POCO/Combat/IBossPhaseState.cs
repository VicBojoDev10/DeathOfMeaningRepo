using System;
using TDOM.Contracts;
using TDOM.POCO.ScriptableObjects;

namespace TDOM.Gameplay.Combat
{
    public interface IBossPhaseState
    {
        BossAttackKind AtaqueActual { get; }
        BossAttackKind SiguienteAtaque();
        void AplicarDaño(float daño);
        int FaseActual { get; }
    }

    public sealed class BossPhaseStateMachine : IBossPhaseState
    {
        private readonly BossPhaseProfile[] _fases;
        private readonly TimeSpan _cooldown;

        private int _faseActual;
        private int _indiceAtaque;
        private float _vidaActual = 1f;

        private DateTime _ultimoCambioAtaque = DateTime.MinValue;

        public BossPhaseStateMachine(
            BossPhaseProfile[] fases,
            float cooldownSegundos = 0f)
        {
            _fases = fases;
            _cooldown = TimeSpan.FromSeconds(cooldownSegundos);

            _faseActual = 0;
            _indiceAtaque = 0;
        }

        public BossAttackKind AtaqueActual =>
            _fases[_faseActual].OrdenDeAtaque[_indiceAtaque];

        public int FaseActual => _faseActual + 1;

        public BossAttackKind SiguienteAtaque()
        {
            var ahora = DateTime.UtcNow;

            if (_cooldown > TimeSpan.Zero &&
                ahora - _ultimoCambioAtaque < _cooldown)
            {
                return AtaqueActual;
            }

            _ultimoCambioAtaque = ahora;

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

                // Reiniciar cooldown al cambiar de fase
                _ultimoCambioAtaque = DateTime.MinValue;
            }
        }
    }
}