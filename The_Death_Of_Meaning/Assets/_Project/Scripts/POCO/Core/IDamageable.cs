namespace TDOM.Gameplay.Core
{
    public interface IDamageable
    {
        void RecibirDanio(float cantidad, bool esInstaKill = false);
    }
}
