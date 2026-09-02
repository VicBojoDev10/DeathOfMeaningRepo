namespace TDOM.Gameplay.Core
{
    public enum EstadoSesion
    {
        Desconectado,
        Conectando,
        EsperandoJugador,
        Listo,
        Error,
    }

    public sealed class SessionStatus
    {
        public EstadoSesion Estado { get; private set; }
        public string Mensaje { get; private set; }

        public void Marcar(EstadoSesion estado, string mensaje = "")
        {
            Estado = estado;
            Mensaje = mensaje;
        }

        public bool PuedeIniciarPartida => Estado == EstadoSesion.Listo;
    }
}

public enum EstadoSesion
{
    Desconectado,
    Conectando,
    EsperandoJugador,
    Listo,
    Error,
}

public sealed class SessionStatus
{
    public EstadoSesion Estado { get; private set; }
    public string Mensaje { get; private set; }

    public void Marcar(EstadoSesion estado, string mensaje = "")
    {
        Estado = estado;
        Mensaje = mensaje;
    }

    public bool PuedeIniciarPartida => Estado == EstadoSesion.Listo;
}
