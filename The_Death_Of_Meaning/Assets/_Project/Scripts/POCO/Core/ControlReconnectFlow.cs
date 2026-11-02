namespace TDOM.Gameplay.Core
{
    public sealed class ControlReconnectFlow
    {
        private bool _esperandoReconexion;
        private bool _controlReconectado;

        public bool PopupVisible => _esperandoReconexion;
        public bool PuedeContinuar => _esperandoReconexion && _controlReconectado;

        public void RegistrarDesconexion()
        {
            _esperandoReconexion = true;
            _controlReconectado = false;
        }

        public void RegistrarReconexion()
        {
            if (_esperandoReconexion)
                _controlReconectado = true;
        }

        public bool ConfirmarContinuacion()
        {
            if (!PuedeContinuar)
                return false;

            _esperandoReconexion = false;
            _controlReconectado = false;
            return true;
        }
    }
}
