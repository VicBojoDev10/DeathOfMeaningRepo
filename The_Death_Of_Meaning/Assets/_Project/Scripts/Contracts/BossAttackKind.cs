namespace TDOM.Contracts
{
    public enum BossAttackKind
    {
        Basico,
        Pesado,
        Especial,
        InstaKill,
    }

    public struct BossAttackStep
    {
        public BossAttackKind Tipo;
        public int Daño;
        public bool EsInstaKill;

        public BossAttackStep(BossAttackKind tipo, int daño, bool esInstaKill = false)
        {
            Tipo = tipo;
            Daño = daño;
            EsInstaKill = esInstaKill;
        }
    }
}
