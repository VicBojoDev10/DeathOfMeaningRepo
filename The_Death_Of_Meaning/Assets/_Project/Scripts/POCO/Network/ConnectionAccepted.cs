namespace TDOM.Gameplay
{
    public static class ConnectionAccepted
    {
        public const int MaximoJugadores = 2;

        public static bool PuedeAceptarConexion(int conectados, bool partidaIniciada)
        {
            return !partidaIniciada && conectados < MaximoJugadores;
        }
    }
}
