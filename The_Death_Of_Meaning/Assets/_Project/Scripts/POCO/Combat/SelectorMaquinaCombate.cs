namespace TDOM.Gameplay.Combat
{
    public enum MaquinaCombateSeleccionada
    {
        Ninguna,
        Melee,
        Disparo,
    }

    public static class SelectorMaquinaCombate
    {
        public static MaquinaCombateSeleccionada Seleccionar(
            bool meleeDisponible,
            bool disparoDisponible,
            bool meleeActiva,
            bool disparoActivo
        )
        {
            if (meleeDisponible && meleeActiva)
                return MaquinaCombateSeleccionada.Melee;

            if (disparoDisponible && disparoActivo)
                return MaquinaCombateSeleccionada.Disparo;

            if (meleeDisponible)
                return MaquinaCombateSeleccionada.Melee;

            return disparoDisponible
                ? MaquinaCombateSeleccionada.Disparo
                : MaquinaCombateSeleccionada.Ninguna;
        }
    }
}
