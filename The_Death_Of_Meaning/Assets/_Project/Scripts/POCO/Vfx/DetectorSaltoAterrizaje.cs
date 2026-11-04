namespace TDOM.Gameplay.Vfx
{
    public sealed class DetectorSaltoAterrizaje
    {
        private readonly float _velocidadMinimaAterrizaje;
        private bool _saltoEnCursoPrevio;
        private bool _enSueloPrevio = true;
        private float _velocidadVerticalPrevia;

        public DetectorSaltoAterrizaje(float velocidadMinimaAterrizaje)
        {
            _velocidadMinimaAterrizaje = velocidadMinimaAterrizaje;
        }

        public bool SaltoDesdeSuelo { get; private set; }
        public bool Aterrizaje { get; private set; }

        public void Actualizar(
            bool enSuelo,
            bool saltoEnCurso,
            bool saltoAereo,
            float velocidadVertical
        )
        {
            SaltoDesdeSuelo = saltoEnCurso && !_saltoEnCursoPrevio && !saltoAereo;
            Aterrizaje =
                enSuelo
                && !_enSueloPrevio
                && -_velocidadVerticalPrevia >= _velocidadMinimaAterrizaje;

            _saltoEnCursoPrevio = saltoEnCurso;
            _enSueloPrevio = enSuelo;
            _velocidadVerticalPrevia = velocidadVertical;
        }
    }
}
