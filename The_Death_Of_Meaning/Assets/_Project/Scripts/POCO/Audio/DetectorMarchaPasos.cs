namespace TDOM.Gameplay.Audio
{
    public enum MarchaPasos
    {
        Quieto,
        Caminando,
        Corriendo,
    }

    public sealed class DetectorMarchaPasos
    {
        public MarchaPasos Actual { get; private set; } = MarchaPasos.Quieto;
        public MarchaPasos Anterior { get; private set; } = MarchaPasos.Quieto;
        public bool Cambio { get; private set; }
        public bool Freno { get; private set; }

        public void Actualizar(bool enSuelo, float rapidez, bool corriendo, float rapidezMinima)
        {
            MarchaPasos nueva = Calcular(enSuelo, rapidez, corriendo, rapidezMinima);
            Anterior = Actual;
            Actual = nueva;
            Cambio = Anterior != Actual;
            Freno = Cambio && enSuelo && Actual == MarchaPasos.Quieto;
        }

        public void Reiniciar()
        {
            Anterior = Actual;
            Actual = MarchaPasos.Quieto;
            Cambio = Anterior != Actual;
            Freno = false;
        }

        private static MarchaPasos Calcular(
            bool enSuelo,
            float rapidez,
            bool corriendo,
            float rapidezMinima
        )
        {
            if (!enSuelo || rapidez < rapidezMinima)
                return MarchaPasos.Quieto;

            return corriendo ? MarchaPasos.Corriendo : MarchaPasos.Caminando;
        }
    }
}
