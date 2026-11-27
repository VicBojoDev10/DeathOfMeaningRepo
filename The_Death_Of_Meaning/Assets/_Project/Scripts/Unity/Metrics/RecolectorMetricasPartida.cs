using System.Collections.Generic;
using TDOM.Gameplay;
using TDOM.Gameplay.Core;
using TDOM.Unity.Combat;
using TDOM.Unity.Player;
using Unity.Netcode;
using UnityEngine;

namespace TDOM.Unity
{
    public sealed class RecolectorMetricasPartida : MonoBehaviour
    {
        private readonly HashSet<HitZone> _zonasObservadas = new();
        private readonly HashSet<MinionController> _minionsObservados = new();
        private readonly HashSet<SniperMinionController> _snipersObservados = new();
        private readonly Dictionary<ulong, float> _vidaAnterior = new();
        private readonly Dictionary<ulong, PlayerLifeState> _estadoVidaAnterior = new();
        private NetworkManager _red;
        private int _faseMaxima = 1;

        public RegistroPartida RegistroActual { get; private set; }

        public void Iniciar(NetworkManager red)
        {
            DesconectarEventos();
            _red = red;
            _faseMaxima = 1;
            _vidaAnterior.Clear();
            _estadoVidaAnterior.Clear();
            _zonasObservadas.Clear();
            _minionsObservados.Clear();
            _snipersObservados.Clear();
            RegistroActual = new RegistroPartida();
            ActualizarObservadores();
        }

        public RegistroPartida Finalizar(string resultado, float duracion, int fase)
        {
            if (RegistroActual == null)
                return null;

            ActualizarObservadores();
            if (!RegistroActual.Cerrar(resultado, duracion, Mathf.Max(_faseMaxima, fase)))
                return RegistroActual;

            AlmacenMetricasPartida.Guardar(RegistroActual);
            DesconectarEventos();
            return RegistroActual;
        }

        public static string Serializar(RegistroPartida registro) =>
            registro != null ? JsonUtility.ToJson(registro) : string.Empty;

        public static RegistroPartida Deserializar(string json) =>
            string.IsNullOrWhiteSpace(json) ? null : JsonUtility.FromJson<RegistroPartida>(json);

        private void Update()
        {
            if (RegistroActual == null || RegistroActual.Cerrada || _red == null || !_red.IsServer)
                return;
            ActualizarObservadores();
        }

        private void ActualizarObservadores()
        {
            if (_red == null || _red.SpawnManager == null || RegistroActual == null)
                return;

            foreach (var objeto in _red.SpawnManager.SpawnedObjectsList)
            {
                if (objeto == null || !objeto.IsSpawned)
                    continue;

                if (objeto.TryGetComponent(out PlayerRoot jugador))
                    ObservarJugador(jugador);

                if (objeto.TryGetComponent(out HitZone zona) && _zonasObservadas.Add(zona))
                    zona.DanioRecibido += AlRegistrarDanioHecho;

                if (
                    objeto.TryGetComponent(out MinionController minion)
                    && _minionsObservados.Add(minion)
                )
                    minion.Murio += AlMorirMinion;

                if (
                    objeto.TryGetComponent(out SniperMinionController sniper)
                    && _snipersObservados.Add(sniper)
                )
                    sniper.Murio += AlMorirMinion;

                if (objeto.TryGetComponent(out BossController jefe))
                    _faseMaxima = Mathf.Max(_faseMaxima, jefe.Fase.Value);
            }
        }

        private void ObservarJugador(PlayerRoot jugador)
        {
            ulong clientId = jugador.OwnerClientId;
            RegistroActual.ObtenerOCrearJugador(clientId, jugador.CharacterId.ToString());
            if (!_vidaAnterior.TryGetValue(clientId, out float vidaPrev))
            {
                _vidaAnterior[clientId] = jugador.Vida.Value;
                _estadoVidaAnterior[clientId] = jugador.EstadoVida.Value;
                return;
            }

            float vidaActual = jugador.Vida.Value;
            if (vidaActual < vidaPrev)
                RegistroActual.RegistrarDanoRecibido(clientId, vidaPrev - vidaActual);
            _vidaAnterior[clientId] = vidaActual;

            PlayerLifeState estadoActual = jugador.EstadoVida.Value;
            if (!_estadoVidaAnterior.TryGetValue(clientId, out var estadoPrev))
                estadoPrev = estadoActual;
            if (estadoPrev != PlayerLifeState.Abatido && estadoActual == PlayerLifeState.Abatido)
                RegistroActual.RegistrarCaida(clientId);
            if (estadoPrev != PlayerLifeState.Muerto && estadoActual == PlayerLifeState.Muerto)
                RegistroActual.RegistrarMuerte(clientId);
            _estadoVidaAnterior[clientId] = estadoActual;
        }

        private void AlRegistrarDanioHecho(ulong atacanteId, float cantidad) =>
            RegistroActual?.RegistrarDanoHecho(atacanteId, cantidad);

        private void AlMorirMinion(MinionController minion) =>
            RegistroActual?.RegistrarMinionDerrotado();

        private void AlMorirMinion(SniperMinionController minion) =>
            RegistroActual?.RegistrarMinionDerrotado();

        private void OnDestroy() => DesconectarEventos();

        private void DesconectarEventos()
        {
            foreach (var zona in _zonasObservadas)
                if (zona != null)
                    zona.DanioRecibido -= AlRegistrarDanioHecho;
            foreach (var minion in _minionsObservados)
                if (minion != null)
                    minion.Murio -= AlMorirMinion;
            foreach (var sniper in _snipersObservados)
                if (sniper != null)
                    sniper.Murio -= AlMorirMinion;
            _zonasObservadas.Clear();
            _minionsObservados.Clear();
            _snipersObservados.Clear();
        }
    }
}
