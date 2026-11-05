namespace TDOM.Gameplay.Vfx
{
    public enum TipoDashVfx
    {
        Suelo,
        Aire,
        Cancelacion,
    }

    public enum NubeDashVfx
    {
        Ninguna,
        Normal,
        Grande,
    }

    public sealed class ContextoDashVfx
    {
        private readonly float _ventanaTrasGancho;
        private float _segundosDesdeGancho = float.PositiveInfinity;

        public ContextoDashVfx(float ventanaTrasGancho)
        {
            _ventanaTrasGancho = ventanaTrasGancho;
        }

        public bool EnSuelo { get; private set; }
        public bool Corriendo { get; private set; }
        public bool CancelaAtaque { get; private set; }

        public bool GanchoReciente => _segundosDesdeGancho <= _ventanaTrasGancho;

        public TipoDashVfx Tipo
        {
            get
            {
                if (CancelaAtaque || GanchoReciente)
                    return TipoDashVfx.Cancelacion;

                return EnSuelo ? TipoDashVfx.Suelo : TipoDashVfx.Aire;
            }
        }

        public NubeDashVfx Nube
        {
            get
            {
                if (!EnSuelo)
                    return NubeDashVfx.Ninguna;

                return Corriendo ? NubeDashVfx.Grande : NubeDashVfx.Normal;
            }
        }

        public void Capturar(
            bool enSuelo,
            bool corriendo,
            bool cancelaAtaque,
            bool ganchoActivo,
            float dt
        )
        {
            EnSuelo = enSuelo;
            Corriendo = corriendo;
            CancelaAtaque = cancelaAtaque;
            _segundosDesdeGancho = ganchoActivo ? 0f : _segundosDesdeGancho + dt;
        }
    }
}
