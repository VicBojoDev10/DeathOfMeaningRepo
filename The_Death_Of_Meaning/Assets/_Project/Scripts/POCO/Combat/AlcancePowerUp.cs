using System.Collections.Generic;
using TDOM.Contracts;

namespace TDOM.Gameplay.Combat
{
    /// <summary>Estado de un jugador al momento de tocar un power-up.</summary>
    public readonly struct EstadoJugadorPowerUp
    {
        public readonly ulong Id;
        public readonly bool Vivo;
        public readonly bool NecesitaEfecto;

        public EstadoJugadorPowerUp(ulong id, bool vivo, bool necesitaEfecto)
        {
            Id = id;
            Vivo = vivo;
            NecesitaEfecto = necesitaEfecto;
        }
    }

    /// <summary>
    /// Decide en POCO puro a quién se aplica un power-up y si el orbe se consume (TW-276).
    /// Un jugador abatido nunca lo recibe.
    /// </summary>
    public static class AlcancePowerUp
    {
        /// <summary>
        /// Llena <paramref name="aplicarA"/> con los ids que reciben el efecto y devuelve true si
        /// el orbe se consume. QuienLoToca: solo el que tocó, y se consume solo si lo necesita.
        /// AmbosJugadores: todos los vivos que lo necesiten; se consume si al menos uno lo recibió.
        /// </summary>
        public static bool Resolver(
            ModoAlcancePowerUp alcance,
            ulong idQuienToco,
            IReadOnlyList<EstadoJugadorPowerUp> jugadores,
            List<ulong> aplicarA
        )
        {
            aplicarA.Clear();
            if (jugadores == null)
                return false;

            for (int i = 0; i < jugadores.Count; i++)
            {
                var jugador = jugadores[i];
                if (!jugador.Vivo || !jugador.NecesitaEfecto)
                    continue;

                if (alcance == ModoAlcancePowerUp.QuienLoToca && jugador.Id != idQuienToco)
                    continue;

                aplicarA.Add(jugador.Id);
            }

            return aplicarA.Count > 0;
        }
    }
}
