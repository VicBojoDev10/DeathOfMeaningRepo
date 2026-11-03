namespace TDOM.Gameplay.Combat
{
    /// <summary>
    /// Alterna el sentido del barrido de la Mano de AREK en cada ataque. +1 es el sentido
    /// original (de -90° a +90° respecto al objetivo, de izquierda a derecha) y -1 es el
    /// contrario. El primer ataque sale siempre con +1.
    /// </summary>
    public sealed class AlternadorDireccionMano
    {
        private int _proximo = 1;

        /// <summary>Sentido del próximo ataque, sin consumirlo.</summary>
        public int Proximo => _proximo;

        /// <summary>Devuelve el sentido de este ataque (+1 o -1) y alterna el siguiente.</summary>
        public int Siguiente()
        {
            int sentido = _proximo;
            _proximo = -_proximo;
            return sentido;
        }

        public void Reiniciar()
        {
            _proximo = 1;
        }
    }
}
