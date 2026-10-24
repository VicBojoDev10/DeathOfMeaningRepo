namespace TDOM.Unity.Vfx
{
    public interface ICargaVfx
    {
        void SetCarga(bool activa, float segundosCargados, float segundosMaximos);

        int EtapaDe(float progreso);
    }
}
