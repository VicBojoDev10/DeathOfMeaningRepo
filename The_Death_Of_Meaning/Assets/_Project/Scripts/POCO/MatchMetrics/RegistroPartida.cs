using System;
using System.Collections.Generic;
using System.Globalization;

namespace TDOM.Gameplay
{
    [Serializable]
    public sealed class RegistroPartida
    {
        public string IdPartida;
        public string FechaUtc;
        public string Resultado;
        public float DuracionSegundos;
        public int FaseAlcanzada;
        public int MinionsDerrotados;
        public bool Cerrada;
        public List<MetricasJugador> Jugadores = new();
        public float DanoRecibidoTotal
        {
            get
            {
                float total = 0f;
                foreach (var jugador in Jugadores)
                    total += jugador.DanoRecibido;
                return total;
            }
        }

        public RegistroPartida()
        {
            IdPartida = Guid.NewGuid().ToString("N");
            FechaUtc = DateTime.UtcNow.ToString("O");
        }

        public RegistroPartida(string fechaUtc)
        {
            IdPartida = Guid.NewGuid().ToString("N");
            FechaUtc = string.IsNullOrWhiteSpace(fechaUtc)
                ? DateTime.UtcNow.ToString("O")
                : fechaUtc;
        }

        public MetricasJugador ObtenerOCrearJugador(ulong clientId, string personaje)
        {
            foreach (var jugador in Jugadores)
            {
                if (
                    !ulong.TryParse(
                        jugador.ClientId,
                        NumberStyles.None,
                        CultureInfo.InvariantCulture,
                        out ulong idExistente
                    )
                    || idExistente != clientId
                )
                    continue;
                if (!string.IsNullOrWhiteSpace(personaje))
                    jugador.Personaje = personaje;
                return jugador;
            }

            var nuevo = new MetricasJugador(clientId, personaje);
            Jugadores.Add(nuevo);
            return nuevo;
        }

        public void RegistrarDanoHecho(ulong clientId, float cantidad)
        {
            if (Cerrada || cantidad <= 0f)
                return;
            ObtenerOCrearJugador(clientId, null).DanoHecho += cantidad;
        }

        public void RegistrarDanoRecibido(ulong clientId, float cantidad)
        {
            if (Cerrada || cantidad <= 0f)
                return;
            ObtenerOCrearJugador(clientId, null).DanoRecibido += cantidad;
        }

        public void RegistrarCaida(ulong clientId)
        {
            if (!Cerrada)
                ObtenerOCrearJugador(clientId, null).VecesAbatido++;
        }

        public void RegistrarMuerte(ulong clientId)
        {
            if (!Cerrada)
                ObtenerOCrearJugador(clientId, null).Muertes++;
        }

        public void RegistrarMinionDerrotado()
        {
            if (!Cerrada)
                MinionsDerrotados++;
        }

        public bool Cerrar(string resultado, float duracionSegundos, int faseAlcanzada)
        {
            if (Cerrada)
                return false;

            Resultado = resultado ?? string.Empty;
            DuracionSegundos = Math.Max(0f, duracionSegundos);
            FaseAlcanzada = Math.Max(1, faseAlcanzada);
            Cerrada = true;
            return true;
        }
    }
}
