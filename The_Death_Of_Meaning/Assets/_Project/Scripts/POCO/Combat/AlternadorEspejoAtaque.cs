using TDOM.Contracts;

namespace TDOM.Gameplay.Combat
{
    /// <summary>
    /// Decide si un golpe se reproduce espejado. Cada combo nuevo (golpe ligero con ComboIndex 0)
    /// alterna entre normal y espejado; el resto del combo conserva el mismo lado para que la
    /// animacion no cambie de mano a la mitad. Un cargado directo (sin combo previo) nunca se
    /// espeja y no consume la alternancia.
    /// </summary>
    public sealed class AlternadorEspejoAtaque
    {
        private bool _espejado;
        private bool _proximoEspejado;

        public bool Espejado => _espejado;

        public bool Resolver(AttackEvent evento)
        {
            if (evento.ComboIndex == 0)
            {
                if (evento.Kind == AttackKind.Light)
                {
                    _espejado = _proximoEspejado;
                    _proximoEspejado = !_proximoEspejado;
                }
                else
                {
                    _espejado = false;
                }
            }

            return _espejado;
        }

        public void Reiniciar()
        {
            _espejado = false;
            _proximoEspejado = false;
        }
    }
}
