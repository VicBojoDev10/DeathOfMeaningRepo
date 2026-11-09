namespace TDOM.Gameplay.Vfx
{
    public sealed class DetectorFinEmbestida
    {
        private bool _activaPrevia;

        public bool Termino { get; private set; }

        public void Actualizar(bool activa)
        {
            Termino = _activaPrevia && !activa;
            _activaPrevia = activa;
        }
    }
}
