using TDOM.Gameplay.Combat;

namespace TDOM.Unity.Combat
{
    public interface IFiltroImpacto
    {
        bool Admite(HitZone zona, TipoImpactoOjo tipo, bool atacanteEsAyla);
    }
}
