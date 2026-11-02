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
            bool disparoActivo,
            bool meleeSolicitada,
            bool disparoSolicitado
        )
        {
            if (meleeActiva)
                return MaquinaCombateSeleccionada.Melee;

            if (disparoActivo)
                return MaquinaCombateSeleccionada.Disparo;

            if (meleeDisponible && meleeSolicitada)
                return MaquinaCombateSeleccionada.Melee;

            if (disparoDisponible && disparoSolicitado)
                return MaquinaCombateSeleccionada.Disparo;

            if (meleeDisponible)
                return MaquinaCombateSeleccionada.Melee;

            return disparoDisponible
                ? MaquinaCombateSeleccionada.Disparo
                : MaquinaCombateSeleccionada.Ninguna;
        }
    }
}
