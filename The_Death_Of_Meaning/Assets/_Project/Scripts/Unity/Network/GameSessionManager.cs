using System;
using TDOM.Unity.UI;
using Unity.Netcode;
using UnityEngine;

namespace TDOM.Unity
{
    public class GameSessionManager : NetworkBehaviour
    {

    public static GameSessionManager Instance { get; private set; }

    [SerializeField] private Camera _camara;
    [SerializeField] private GameObject _prefabZendre;
    [SerializeField] private GameObject _prefabAyla;
    [SerializeField] private Transform[] _puntosDeSpawn;
    public NetworkList<PlayerSelectionState> Jugadores { get; private set; }

    public event Action OnPartidaIniciada;

    private void Awake()
    {
        Jugadores = new NetworkList<PlayerSelectionState>();
    }

    public override void OnNetworkSpawn()
    {

        Instance = this;
    }
    public override void OnNetworkDespawn()
    {
        if (Instance == this) Instance = null;
    }
    [Rpc(SendTo.Server)]
    public void ElegirPersonajeRpc(CharacterIds id, RpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        for (int i = 0; i < Jugadores.Count; i++)
        {
            if (Jugadores[i].Character == id && Jugadores[i].ClientId != clientId)
            {
                PersonajeRechazadoRpc(RpcTarget.Single(clientId, RpcTargetUse.Temp));
                return;
            }
        }

        int idx = BuscarIndice(clientId);
        var estado = new PlayerSelectionState { ClientId = clientId, Character = id, Ready = false };

        if (idx >= 0) Jugadores[idx] = estado;
        else Jugadores.Add(estado);
    }

    [Rpc(SendTo.Server)]
    public void MarcarListoRpc(RpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        int idx = BuscarIndice(clientId);
        if (idx < 0 || Jugadores[idx].Character == CharacterIds.None) return;
        var estado = Jugadores[idx];
        estado.Ready = true;
        Jugadores[idx] = estado;
        VerificarInicioPartida();
    }
    [Rpc(SendTo.SpecifiedInParams)]
    private void PersonajeRechazadoRpc(RpcParams rpcParams) {  }
    private int BuscarIndice(ulong clientId)
    {
        for (int i = 0; i < Jugadores.Count; i++)
            if (Jugadores[i].ClientId == clientId) return i;
        return -1;
    }

    private void VerificarInicioPartida()
    {
        if (Jugadores.Count != 2) return;
        foreach (var j in Jugadores)
            if (!j.Ready) return;

        for (int i = 0; i < Jugadores.Count; i++)
        {
            var prefab = Jugadores[i].Character == CharacterIds.Zendre ? _prefabZendre : _prefabAyla;
            var go = Instantiate(prefab, _puntosDeSpawn[i].position, Quaternion.identity);
            go.GetComponent<NetworkObject>().SpawnWithOwnership(Jugadores[i].ClientId);
        }
        _camara.transform.position = _puntosDeSpawn[0].position;
        IniciarPartidaClientRpc();
    }

    [ClientRpc]
    private void IniciarPartidaClientRpc()
    {
        OnPartidaIniciada?.Invoke();
        UiManager.Instance.CloseWindow(WindowsIds.ChSelectionUI);
    }
    }
}
