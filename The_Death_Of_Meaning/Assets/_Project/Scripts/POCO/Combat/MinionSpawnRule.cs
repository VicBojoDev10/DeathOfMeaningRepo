using System;

namespace TDOM.Gameplay.Combat
{
    /// <summary>
    /// Decide si el Especial de AREK puede spawnear minions, en POCO puro.
    /// Confluence: son limitados y reaparecen solo si todos fueron eliminados y el jefe
    /// recibe más daño después de eso. La primera oleada no necesita daño previo.
    /// Al cambiar de fase, el siguiente Especial spawnea aunque queden vivos de la fase anterior
    /// (esos se quedan); después vuelve la regla normal.
    /// </summary>
    public sealed class MinionSpawnRule
    {
        private bool _jefeDaniadoTrasOleada = true;
        private bool _liberadoPorCambioDeFase;

        /// <summary>Minions vivos, de cualquier oleada.</summary>
        public int Vivos { get; private set; }

        public bool PuedeSpawnear =>
            _liberadoPorCambioDeFase || (Vivos == 0 && _jefeDaniadoTrasOleada);

        /// <summary>Cuántos spawnear con la cantidad de la fase; 0 si la regla no lo permite.</summary>
        public int CantidadASpawnear(int minionsEnEspecial) =>
            PuedeSpawnear ? Math.Max(0, minionsEnEspecial) : 0;

        public void RegistrarSpawn(int cantidad)
        {
            if (cantidad <= 0)
                return;

            Vivos += cantidad;
            _jefeDaniadoTrasOleada = false;
            _liberadoPorCambioDeFase = false;
        }

        public void RegistrarMuerte()
        {
            if (Vivos > 0)
                Vivos--;
        }

        /// <summary>El siguiente Especial spawnea aunque queden minions vivos.</summary>
        public void LiberarPorCambioDeFase() => _liberadoPorCambioDeFase = true;

        /// <summary>Solo cuenta si ya no queda ningún minion vivo.</summary>
        public void RegistrarDanioAlJefe()
        {
            if (Vivos == 0)
                _jefeDaniadoTrasOleada = true;
        }
    }
}
