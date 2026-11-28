using System;

namespace TDOM.Gameplay
{
    [Serializable]
    public sealed class MetricasJugador
    {
        public string ClientId;
        public string Personaje;
        public float DanoHecho;
        public float DanoRecibido;
        public int VecesAbatido;
        public int Muertes;

        public MetricasJugador() { }

        public MetricasJugador(ulong clientId, string personaje)
        {
            ClientId = clientId.ToString(System.Globalization.CultureInfo.InvariantCulture);
            Personaje = personaje ?? string.Empty;
        }
    }
}
