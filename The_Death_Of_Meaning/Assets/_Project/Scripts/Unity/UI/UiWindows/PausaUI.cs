using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace TDOM.Unity
{
    public class PausaUI : UIWindow
    {
        [SerializeField]
        private Button continueButton;

        [SerializeField]
        private Button retryButton;

        [SerializeField]
        private Button exitButton;

        [SerializeField]
        private TMP_Text titulo;

        private void Start()
        {
            continueButton?.onClick.AddListener(() => PauseManager.Instance?.SolicitarPausa(false));
            retryButton?.onClick.AddListener(() => PauseManager.Instance?.PedirConfirmacion(false));
            exitButton?.onClick.AddListener(() => PauseManager.Instance?.PedirConfirmacion(true));
        }

        public void ActualizarTitulo(ulong id)
        {
            if (titulo == null)
                titulo = GetComponentInChildren<TMP_Text>(true);
            string nombre = "Ayla";
            if (GameSessionManager.Instance != null)
                foreach (var jugador in GameSessionManager.Instance.Jugadores)
                    if (jugador.ClientId == id)
                        nombre = jugador.Character.ToString();
            if (titulo != null)
                titulo.text = $"Pausa {nombre}";
        }

        public void SetPopupActivo(bool activo)
        {
            SetInteractable(!activo);
            if (!activo && IsOpen)
                FocusFirstElement();
        }
    }
}
