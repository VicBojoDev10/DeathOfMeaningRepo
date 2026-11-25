namespace TDOM.Gameplay.Audio
{
    public sealed class DetectorFlancoSfx
    {
        private bool _activoPrevio;

        public bool Actualizar(bool activo)
        {
            bool entro = activo && !_activoPrevio;
            _activoPrevio = activo;
            return entro;
        }
    }
}
