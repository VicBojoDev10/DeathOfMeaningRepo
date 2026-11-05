using System;
using System.Collections.Generic;
using TDOM.Gameplay.Core;
using TDOM.Unity.Player;
using Unity.Netcode;
using UnityEngine;

namespace TDOM.Unity
{
    public enum ArenaEncounterPhase : byte
    {
        Esperando = 0,
        Presentacion = 1,
        Combate = 2,
    }

    public sealed class ArenaEncounterNetwork : NetworkBehaviour
    {
        public static ArenaEncounterNetwork Instance { get; private set; }
        public static event Action<bool> PuertasSolicitadas;
        public static event Action<bool> TriggersArenaHabilitados;
        public static event Action<bool> TriggerEntradaHabilitado;

        public readonly NetworkVariable<ArenaEncounterPhase> Fase = new(
            ArenaEncounterPhase.Esperando,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public readonly NetworkVariable<bool> ArenaSellada = new(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public readonly NetworkVariable<bool> EntradaConsumida = new(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public event Action JefeRevelado;
        private readonly Dictionary<ulong, HashSet<int>> _presencia = new();
        private readonly Dictionary<ulong, PlayerRoot> _jugadores = new();
        private readonly HashSet<ulong> _abatidos = new();
        private bool _reveladoEnEstaPartida;

        public override void OnNetworkSpawn()
        {
            Instance = this;
            ArenaSellada.OnValueChanged += AlCambiarSelloArena;
            EntradaConsumida.OnValueChanged += AlCambiarEntradaConsumida;
            AplicarTriggersArena(!ArenaSellada.Value);
            AplicarTriggerEntrada(!EntradaConsumida.Value);
            if (IsServer && NetworkManager != null)
                NetworkManager.OnClientDisconnectCallback += AlDesconectar;
        }

        public override void OnNetworkDespawn()
        {
            ArenaSellada.OnValueChanged -= AlCambiarSelloArena;
            EntradaConsumida.OnValueChanged -= AlCambiarEntradaConsumida;
            if (NetworkManager != null)
                NetworkManager.OnClientDisconnectCallback -= AlDesconectar;
            if (Instance == this) Instance = null;
            _presencia.Clear();
            _jugadores.Clear();
            _abatidos.Clear();
        }

        public void Registrar(PlayerRoot jugador, int volumen, bool entro)
        {
            if (jugador == null || !jugador.IsSpawned) return;
            if (IsServer) ActualizarPresencia(jugador.OwnerClientId, jugador, volumen, entro);
            else if (jugador.IsOwner) RegistrarRpc(jugador.OwnerClientId, volumen, entro);
            if (entro && volumen == 1)
            {
                if (IsServer) AbrirAcceso();
                else if (jugador.IsOwner) SolicitarPuertasServidorRpc(jugador.OwnerClientId);
            }
        }

        [Rpc(SendTo.Server)]
        private void SolicitarPuertasServidorRpc(ulong clientId, RpcParams rpc = default)
        {
            if (rpc.Receive.SenderClientId != clientId) return;
            AbrirAcceso();
        }

        private void AbrirAcceso()
        {
            if (!IsServer || EntradaConsumida.Value || ArenaSellada.Value) return;
            EntradaConsumida.Value = true;
            SolicitarPuertasRpc(true);
            AplicarTriggerEntrada(false);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void SolicitarPuertasRpc(bool abrir) => PuertasSolicitadas?.Invoke(abrir);

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void RegistrarRpc(ulong clientId, int volumen, bool entro, RpcParams rpc = default)
        {
            if (rpc.Receive.SenderClientId != clientId || NetworkManager == null ||
                !NetworkManager.ConnectedClients.TryGetValue(clientId, out var cliente) ||
                !cliente.PlayerObject || !cliente.PlayerObject.TryGetComponent(out PlayerRoot jugador)) return;
            ActualizarPresencia(clientId, jugador, volumen, entro);
        }

        private void ActualizarPresencia(ulong id, PlayerRoot jugador, int volumen, bool entro)
        {
            _jugadores[id] = jugador;
            if (!_presencia.TryGetValue(id, out var volumenes))
                _presencia[id] = volumenes = new HashSet<int>();
            if (entro) volumenes.Add(volumen);
            else volumenes.Remove(volumen);
            if (volumenes.Count == 0) _presencia.Remove(id);
            ComprobarInicio();
            if (entro && volumen == 2) ComprobarSelladoArena();
        }

        private void ComprobarSelladoArena()
        {
            if (!IsServer || ArenaSellada.Value || ContarVivosEnVolumen(2) < 2) return;
            ArenaSellada.Value = true;
            SolicitarPuertasRpc(false);
            AplicarTriggersArena(false);
        }

        private int ContarVivosEnVolumen(int volumen)
        {
            int total = 0;
            foreach (var (id, volumenes) in _presencia)
                if (volumenes.Contains(volumen) && !_abatidos.Contains(id) &&
                    _jugadores.TryGetValue(id, out var jugador) && jugador != null && jugador.IsSpawned &&
                    jugador.EstadoVida.Value == PlayerLifeState.Vivo)
                    total++;
            return total;
        }

        private void AlCambiarSelloArena(bool anterior, bool actual) => AplicarTriggersArena(!actual);

        private void AlCambiarEntradaConsumida(bool anterior, bool actual) => AplicarTriggerEntrada(!actual);

        private static void AplicarTriggersArena(bool habilitados) =>
            TriggersArenaHabilitados?.Invoke(habilitados);

        private static void AplicarTriggerEntrada(bool habilitado) =>
            TriggerEntradaHabilitado?.Invoke(habilitado);

        public void NotificarEstadoVida(PlayerRoot jugador, PlayerLifeState estado)
        {
            if (!IsServer || jugador == null) return;
            if (estado == PlayerLifeState.Vivo) _abatidos.Remove(jugador.OwnerClientId);
            else _abatidos.Add(jugador.OwnerClientId);
            ComprobarInicio();
            if (estado == PlayerLifeState.Vivo) ComprobarSelladoArena();
        }

        public void DesregistrarJugador(PlayerRoot jugador)
        {
            if (jugador == null) return;
            if (IsServer) Retirar(jugador.OwnerClientId);
            else if (jugador.IsOwner) RetirarRpc(jugador.OwnerClientId);
        }

        [Rpc(SendTo.Server)]
        private void RetirarRpc(ulong clientId, RpcParams rpc = default)
        {
            if (rpc.Receive.SenderClientId == clientId) Retirar(clientId);
        }

        private void Retirar(ulong id)
        {
            _presencia.Remove(id);
            ComprobarInicio();
        }

        private void ComprobarInicio()
        {
            if (!IsServer || Fase.Value != ArenaEncounterPhase.Esperando) return;
            int presentesVivos = 0;
            foreach (var (id, vol) in _presencia)
                if (vol.Count > 0 && !_abatidos.Contains(id) && _jugadores.TryGetValue(id, out var p) &&
                    p != null && p.IsSpawned && p.EstadoVida.Value == PlayerLifeState.Vivo)
                    presentesVivos++;
            if (presentesVivos >= 2) IniciarPresentacion();
        }

        private void IniciarPresentacion()
        {
            if (!IsServer || _reveladoEnEstaPartida || Fase.Value != ArenaEncounterPhase.Esperando) return;
            _reveladoEnEstaPartida = true;
            Fase.Value = ArenaEncounterPhase.Presentacion;
            RevelarJefeRpc();
        }
        public void IniciarCombate()
        {
            if (IsServer && Fase.Value == ArenaEncounterPhase.Presentacion)
                Fase.Value = ArenaEncounterPhase.Combate;
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void RevelarJefeRpc() => JefeRevelado?.Invoke();

        private void AlDesconectar(ulong id)
        {
            _presencia.Remove(id);
            _jugadores.Remove(id);
            _abatidos.Remove(id);
            ComprobarInicio();
        }

        public void ReiniciarServidor()
        {
            if (!IsServer) return;
            _presencia.Clear();
            _jugadores.Clear();
            _abatidos.Clear();
            _reveladoEnEstaPartida = false;
            ArenaSellada.Value = false;
            EntradaConsumida.Value = false;
            AplicarTriggerEntrada(true);
            AplicarTriggersArena(true);
            Fase.Value = ArenaEncounterPhase.Esperando;
        }

#if UNITY_EDITOR
        private void OnGUI()
        {
            if (!IsSpawned || !IsServer) return;
            GUILayout.BeginArea(new Rect(10, 10, 230, 90), "Encuentro AREK (F8)", GUI.skin.window);
            GUILayout.Label($"Fase: {Fase.Value}");
            if (GUILayout.Button("Forzar Presentación + JefeRevelado") ||
                (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.F8))
            {
                IniciarPresentacion();
                Event.current.Use();
            }
            if (GUILayout.Button("Forzar Combate")) IniciarCombate();
            GUILayout.EndArea();
        }
#endif
    }
}
