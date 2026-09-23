using TDOM.Gameplay.Core;
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
        private int _faseActual;
        private int _indiceAtaque;
        private float _vidaActual = 1f;

        public BossPhaseStateMachine(BossPhaseProfile[] fases)
        {
            _fases = fases;
            _faseActual = 0;
            _indiceAtaque = 0;
        }

        public BossAttackKind AtaqueActual => _fases[_faseActual].OrdenDeAtaque[_indiceAtaque];
        public int FaseActual => _faseActual + 1;

        public BossAttackKind SiguienteAtaque()
        {
            _indiceAtaque++;
            if (_indiceAtaque >= _fases[_faseActual].OrdenDeAtaque.Length)
                _indiceAtaque = 0;
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
