using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace TDOM.Unity
{
    public class CharacterSelUI : UIWindow
    {
        public Button zendreButton;
        public Button aylaButton;
        public Button ready;
        private void OnEnable()
    {
        zendreButton.onClick.AddListener(() => ElegirPersonaje(CharacterIds.Zendre));
        aylaButton.onClick.AddListener(() => ElegirPersonaje(CharacterIds.Ayla));
        ready.onClick.AddListener(MarcarListo);

        if (GameSessionManager.Instance != null)
        {
            GameSessionManager.Instance.Jugadores.OnListChanged += ActualizarUI;
            GameSessionManager.Instance.OnPartidaIniciada += PartidaIniciada;
            ActualizarUI(default);
        }
    }

    private void OnDisable()
    {
        zendreButton.onClick.RemoveAllListeners();
        aylaButton.onClick.RemoveAllListeners();
        ready.onClick.RemoveAllListeners();

        if (GameSessionManager.Instance != null)
        {
            GameSessionManager.Instance.Jugadores.OnListChanged -= ActualizarUI;
            GameSessionManager.Instance.OnPartidaIniciada -= PartidaIniciada;
        }
    }

    private void ElegirPersonaje(CharacterIds id)
    {
        GameSessionManager.Instance.ElegirPersonajeRpc(id);
    }

    private void MarcarListo()
    {
        GameSessionManager.Instance.MarcarListoRpc();
        ready.interactable = false;
    }

    private void ActualizarUI(NetworkListEvent<PlayerSelectionState> _)
    {
        ulong miId = NetworkManager.Singleton.LocalClientId;
        bool zendreTomado = false, aylaTomado = false;
        CharacterIds miPersonaje = CharacterIds.None;
        bool yaListo = false;

        foreach (var j in GameSessionManager.Instance.Jugadores)
        {
            if (j.ClientId == miId)
            {
                miPersonaje = j.Character;
                yaListo = j.Ready;
                continue;
            }
            if (j.Character == CharacterIds.Zendre) zendreTomado = true;
            if (j.Character == CharacterIds.Ayla) aylaTomado = true;
        }

        zendreButton.interactable = !zendreTomado && miPersonaje != CharacterIds.Zendre && !yaListo;
        aylaButton.interactable = !aylaTomado && miPersonaje != CharacterIds.Ayla && !yaListo;
        ready.interactable = miPersonaje != CharacterIds.None && !yaListo;
    }

    private void PartidaIniciada()
    {
        Hide();
    }

    }
}
