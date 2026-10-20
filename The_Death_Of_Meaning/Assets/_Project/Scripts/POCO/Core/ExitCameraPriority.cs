namespace TDOM.Gameplay.Core
{
    public sealed class ExitCameraPriority
    {
        private readonly int _prioridadElevada;
        private int _prioridadOriginal;
        private bool _elevada;

        public ExitCameraPriority(int prioridadElevada) => _prioridadElevada = prioridadElevada;

        public int Elevar(int prioridadActual)
        {
            if (!_elevada)
                _prioridadOriginal = prioridadActual;

            _elevada = true;
            return _prioridadElevada;
        }

        public int Restaurar(int prioridadActual)
        {
            if (!_elevada)
                return prioridadActual;

            _elevada = false;
            return _prioridadOriginal;
        }
    }
}
