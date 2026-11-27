using System.Collections.Generic;

namespace TDOM.Gameplay.Combat
{
    public enum ObjetivoJefe
    {
        Ninguno,
        DestruirOjos,
        RomperEscudosAzules,
        RomperEscudosRojos,
        RomperEscudosAmbos,
    }

    public enum InstruccionJugador
    {
        Ninguna,
        DestruirOjos,
        RomperEscudosAzules,
        RomperEscudosRojos,
        ApoyarAyla,
        ApoyarZendre,
    }

    public static class ResolvedorObjetivoJefe
    {
        public static ObjetivoJefe Resolver(
            IEnumerable<(EstadoOjo estado, ColorEscudo escudo)> ojos
        )
        {
            bool hayAzul = false;
            bool hayRojo = false;
            bool hayAbierto = false;
            foreach (var (estado, escudo) in ojos)
            {
                if (estado != EstadoOjo.Abierto)
                    continue;
                hayAbierto = true;
                hayAzul |= escudo == ColorEscudo.Azul;
                hayRojo |= escudo == ColorEscudo.Rojo;
            }

            return (hayAzul, hayRojo, hayAbierto) switch
            {
                (true, true, _) => ObjetivoJefe.RomperEscudosAmbos,
                (true, false, _) => ObjetivoJefe.RomperEscudosAzules,
                (false, true, _) => ObjetivoJefe.RomperEscudosRojos,
                (_, _, true) => ObjetivoJefe.DestruirOjos,
                _ => ObjetivoJefe.Ninguno,
            };
        }

        public static InstruccionJugador ParaJugador(ObjetivoJefe objetivo, bool esAyla) =>
            objetivo switch
            {
                ObjetivoJefe.DestruirOjos => InstruccionJugador.DestruirOjos,
                ObjetivoJefe.RomperEscudosAzules => esAyla
                    ? InstruccionJugador.ApoyarZendre
                    : InstruccionJugador.RomperEscudosAzules,
                ObjetivoJefe.RomperEscudosRojos => esAyla
                    ? InstruccionJugador.RomperEscudosRojos
                    : InstruccionJugador.ApoyarAyla,
                ObjetivoJefe.RomperEscudosAmbos => esAyla
                    ? InstruccionJugador.RomperEscudosRojos
                    : InstruccionJugador.RomperEscudosAzules,
                _ => InstruccionJugador.Ninguna,
            };
    }
}
