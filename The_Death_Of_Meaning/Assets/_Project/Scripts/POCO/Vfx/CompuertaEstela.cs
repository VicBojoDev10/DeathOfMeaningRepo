namespace TDOM.Gameplay.Vfx
{
    /// <summary>
    /// Decide si la estela de la espada debe dibujarse: solo si el ataque la permite y la hoja
    /// se mueve lo bastante rápido. Usa dos umbrales (histéresis) para que no parpadee cuando
    /// la velocidad oscila cerca del límite a mitad de un golpe.
    /// </summary>
    public sealed class CompuertaEstela
    {
        private readonly float _velocidadEncendido;
        private readonly float _velocidadApagado;

        public CompuertaEstela(float velocidadEncendido, float velocidadApagado)
        {
            _velocidadEncendido = velocidadEncendido;
            _velocidadApagado = System.Math.Min(velocidadApagado, velocidadEncendido);
        }

        public bool Abierta { get; private set; }

        public bool Evaluar(float velocidad, bool permitida)
        {
            if (!permitida)
                Abierta = false;
            else if (velocidad >= _velocidadEncendido)
                Abierta = true;
            else if (velocidad < _velocidadApagado)
                Abierta = false;

            return Abierta;
        }

        public void Reiniciar()
        {
            Abierta = false;
        }
    }
}
