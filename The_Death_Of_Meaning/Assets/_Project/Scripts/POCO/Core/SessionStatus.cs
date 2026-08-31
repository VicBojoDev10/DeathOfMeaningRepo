public sealed class SessionStatus
{
    public EstadoSession Estado { get; private set; }
    public string Mensaje { get; private set; }

    public void Marcar(EstadoSession estado, string mensaje = "")
    {
        Estado = estado;
        Mensaje = mensaje;
    }

    public bool PuedeIniciarPartida => Estado == EstadoSession.Listo;
}

public enum EstadoSession
{
    Desconectado,
    Conectando,
    EsperandoJugador,
    Listo,
    Error,
}
