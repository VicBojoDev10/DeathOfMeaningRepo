namespace TDOM.Gameplay.Vfx
{
    public static class IndiceVfxCombo
    {
        public const int SinEfecto = -1;

        private const int LigerosAntesDelCierre = 1;
        private const int IndiceDelCierre = 2;

        public static int Resolver(bool cargado, int comboIndex, int ligerosPrevios)
        {
            if (!cargado)
                return comboIndex;

            return ligerosPrevios == LigerosAntesDelCierre ? IndiceDelCierre : SinEfecto;
        }
    }
}
