using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using TDOM.Data;
using TDOM.Gameplay;
using UnityEngine;

namespace TDOM.Unity
{
    public sealed class DescubrimientoLan : MonoBehaviour
    {
        [SerializeField]
        private DescubrimientoLanProfile _perfil;

        private readonly ConcurrentQueue<(string mensaje, string ip)> _recibidos = new();
        private ListaPartidasLan _lista;
        private UdpClient _emisor;
        private UdpClient _receptor;
        private byte[] _anuncio;
        private float _siguienteAnuncio;

        public event Action<IReadOnlyList<PartidaLan>> PartidasCambiaron;

        public DescubrimientoLanProfile Perfil => _perfil;

        public IReadOnlyList<PartidaLan> Partidas =>
            _lista != null ? _lista.Partidas : Array.Empty<PartidaLan>();

        public bool Buscando => _receptor != null;

        private void Awake() => _lista = new ListaPartidasLan(_perfil.Caducidad);

        public void EstablecerAnuncio(bool anunciar, ushort puertoPartida)
        {
            if (!anunciar)
            {
                CerrarEmisor();
                return;
            }
            if (_emisor != null)
                return;
            _anuncio = Encoding.UTF8.GetBytes(
                AnuncioPartidaLan.Codificar(Environment.MachineName, puertoPartida)
            );
            _emisor = new UdpClient { EnableBroadcast = true };
            _siguienteAnuncio = 0f;
        }

        public void IniciarBusqueda()
        {
            if (_receptor != null)
                return;
            try
            {
                _receptor = new UdpClient { EnableBroadcast = true };
                _receptor.Client.SetSocketOption(
                    SocketOptionLevel.Socket,
                    SocketOptionName.ReuseAddress,
                    true
                );
                _receptor.Client.Bind(new IPEndPoint(IPAddress.Any, _perfil.PuertoDescubrimiento));
                _receptor.BeginReceive(AlRecibir, _receptor);
            }
            catch (SocketException e)
            {
                Debug.LogWarning($"[DescubrimientoLan] No se pudo escuchar partidas: {e.Message}");
                CerrarReceptor();
            }
        }

        public void DetenerBusqueda()
        {
            CerrarReceptor();
            if (_lista.Limpiar())
                PartidasCambiaron?.Invoke(_lista.Partidas);
        }

        private void AlRecibir(IAsyncResult resultado)
        {
            var receptor = (UdpClient)resultado.AsyncState;
            try
            {
                var origen = new IPEndPoint(IPAddress.Any, 0);
                byte[] datos = receptor.EndReceive(resultado, ref origen);
                _recibidos.Enqueue((Encoding.UTF8.GetString(datos), origen.Address.ToString()));
                receptor.BeginReceive(AlRecibir, receptor);
            }
            catch (ObjectDisposedException) { }
            catch (SocketException) { }
        }

        private void Update()
        {
            Anunciar();
            bool cambio = _lista.Tick(Time.unscaledDeltaTime);
            while (_recibidos.TryDequeue(out var recibido))
                if (
                    AnuncioPartidaLan.TryDecodificar(recibido.mensaje, recibido.ip, out var partida)
                )
                    cambio |= _lista.Registrar(partida);
            if (cambio)
                PartidasCambiaron?.Invoke(_lista.Partidas);
        }

        private void Anunciar()
        {
            if (_emisor == null || Time.unscaledTime < _siguienteAnuncio)
                return;
            _siguienteAnuncio = Time.unscaledTime + _perfil.IntervaloAnuncio;
            try
            {
                _emisor.Send(
                    _anuncio,
                    _anuncio.Length,
                    new IPEndPoint(IPAddress.Broadcast, _perfil.PuertoDescubrimiento)
                );
            }
            catch (SocketException e)
            {
                Debug.LogWarning(
                    $"[DescubrimientoLan] No se pudo anunciar la partida: {e.Message}"
                );
            }
        }

        private void CerrarEmisor()
        {
            _emisor?.Close();
            _emisor = null;
        }

        private void CerrarReceptor()
        {
            _receptor?.Close();
            _receptor = null;
        }

        private void OnDestroy()
        {
            CerrarEmisor();
            CerrarReceptor();
        }
    }
}
