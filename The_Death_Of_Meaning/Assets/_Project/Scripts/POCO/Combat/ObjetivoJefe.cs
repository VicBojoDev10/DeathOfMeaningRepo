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
    }
}
