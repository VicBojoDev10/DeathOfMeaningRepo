using System;

namespace TDOM.Gameplay.Combat
{
    public sealed class BossWeakPointDamage
    {
        private readonly WeakPointStateMachine _ojos;
        private readonly IBossPhaseState _fases;

        public BossWeakPointDamage(WeakPointStateMachine ojos, IBossPhaseState fases)
        {
            _ojos = ojos ?? throw new ArgumentNullException(nameof(ojos));
            _fases = fases ?? throw new ArgumentNullException(nameof(fases));
            _ojos.ActivarFase(_fases.FaseActual);
        }

        public int TotalOjos => _ojos.TotalOjos;

        public EstadoOjo Estado(int ojo) => _ojos.Estado(ojo);

        public void AbrirOjoParaDebug(int ojo) => _ojos.AbrirOjoParaDebug(ojo);

        public bool AdmiteImpacto(int ojo, TipoImpactoOjo tipo, bool atacanteEsAyla) =>
            _ojos.ResolverImpacto(ojo, tipo, atacanteEsAyla);

        public bool AdmiteImpacto(int ojo, TipoImpactoOjo tipo) => _ojos.ResolverImpacto(ojo, tipo);

        public bool AplicarDanio(float porcentaje)
        {
            int faseAnterior = _fases.FaseActual;
            _fases.AplicarDaño(porcentaje);

            if (_fases.FaseActual == faseAnterior)
                return false;

            _ojos.ActivarFase(_fases.FaseActual);
            return true;
        }

        public void Tick(float dt) => _ojos.Tick(dt);
    }
}
