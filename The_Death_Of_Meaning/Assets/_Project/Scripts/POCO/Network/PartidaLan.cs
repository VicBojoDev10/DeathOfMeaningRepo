using System;
using System.Collections.Generic;

namespace TDOM.Gameplay
{
    public readonly struct PartidaLan : IEquatable<PartidaLan>
    {
        public PartidaLan(string nombre, string ip, ushort puerto)
        {
            Nombre = nombre;
            Ip = ip;
            Puerto = puerto;
        }

        public string Nombre { get; }
        public string Ip { get; }
        public ushort Puerto { get; }

        public bool Equals(PartidaLan otra) =>
            Nombre == otra.Nombre && Ip == otra.Ip && Puerto == otra.Puerto;

        public override bool Equals(object obj) => obj is PartidaLan otra && Equals(otra);

        public override int GetHashCode() => HashCode.Combine(Nombre, Ip, Puerto);
    }

    public static class AnuncioPartidaLan
    {
        private const string Prefijo = "TDOM-LAN|1|";
        private const char Separador = '|';

        public static string Codificar(string nombre, ushort puerto) =>
            $"{Prefijo}{puerto}{Separador}{nombre.Replace(Separador, ' ')}";

        public static bool TryDecodificar(string mensaje, string ipOrigen, out PartidaLan partida)
        {
            partida = default;
            if (
                string.IsNullOrEmpty(mensaje)
                || string.IsNullOrEmpty(ipOrigen)
                || !mensaje.StartsWith(Prefijo, StringComparison.Ordinal)
            )
                return false;

            string datos = mensaje.Substring(Prefijo.Length);
            int separador = datos.IndexOf(Separador);
            if (
                separador <= 0
                || !ushort.TryParse(datos.Substring(0, separador), out ushort puerto)
            )
                return false;

            partida = new PartidaLan(datos.Substring(separador + 1), ipOrigen, puerto);
            return true;
        }
    }

    public sealed class ListaPartidasLan
    {
        private readonly float _caducidad;
        private readonly List<PartidaLan> _partidas = new();
        private readonly List<float> _tiempoSinVer = new();

        public ListaPartidasLan(float caducidad) => _caducidad = Math.Max(0f, caducidad);

        public IReadOnlyList<PartidaLan> Partidas => _partidas;

        public bool Registrar(PartidaLan partida)
        {
            int indice = Indice(partida.Ip, partida.Puerto);
            if (indice >= 0)
            {
                _tiempoSinVer[indice] = 0f;
                if (_partidas[indice].Equals(partida))
                    return false;
                _partidas[indice] = partida;
                return true;
            }

            _partidas.Add(partida);
            _tiempoSinVer.Add(0f);
            return true;
        }

        public bool Tick(float dt)
        {
            bool cambio = false;
            for (int i = _partidas.Count - 1; i >= 0; i--)
            {
                _tiempoSinVer[i] += Math.Max(0f, dt);
                if (_tiempoSinVer[i] <= _caducidad)
                    continue;
                _partidas.RemoveAt(i);
                _tiempoSinVer.RemoveAt(i);
                cambio = true;
            }
            return cambio;
        }

        public bool Limpiar()
        {
            bool habia = _partidas.Count > 0;
            _partidas.Clear();
            _tiempoSinVer.Clear();
            return habia;
        }

        private int Indice(string ip, ushort puerto)
        {
            for (int i = 0; i < _partidas.Count; i++)
                if (_partidas[i].Ip == ip && _partidas[i].Puerto == puerto)
                    return i;
            return -1;
        }
    }
}
