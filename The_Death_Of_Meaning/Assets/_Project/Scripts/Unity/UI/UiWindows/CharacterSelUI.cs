using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace TDOM.Unity
{
    public class CharacterSelUI : UIWindow
    {
        [Header("Botones")]
        public Button zendreButton;
        public Button aylaButton;
        public Button ready;

        [Header("Textos de feedback (opcional)")]
        public TextMeshProUGUI readyButtonText;
        public TextMeshProUGUI statusText;

        [Header("Colores de estado")]
        public Color colorDisponible = Color.white;
        public Color colorElegidoPorMi = new Color(0.3f, 0.7f, 1f);
        public Color colorBloqueado = new Color(0.5f, 0.5f, 0.5f);

        private Image _zendreImg;
        private Image _aylaImg;

        private void Awake()
        {
            _zendreImg = zendreButton.GetComponent<Image>();
            _aylaImg = aylaButton.GetComponent<Image>();
        }

        private void OnEnable()
        {
            zendreButton.onClick.AddListener(() => ElegirPersonaje(CharacterIds.Zendre));
            aylaButton.onClick.AddListener(() => ElegirPersonaje(CharacterIds.Ayla));
            ready.onClick.AddListener(MarcarListo);

            if (GameSessionManager.Instance != null)
            {
                GameSessionManager.Instance.Jugadores.OnListChanged += ActualizarUI;
                ActualizarUI(default);
            }
        }

        private void OnDisable()
        {
            zendreButton.onClick.RemoveAllListeners();
            aylaButton.onClick.RemoveAllListeners();
            ready.onClick.RemoveAllListeners();

            if (GameSessionManager.Instance != null)
                GameSessionManager.Instance.Jugadores.OnListChanged -= ActualizarUI;
        }

        private void ElegirPersonaje(CharacterIds id)
        {
            AplicarColorTemporal(id);
            GameSessionManager.Instance.ElegirPersonajeRpc(id);
        }

        private void AplicarColorTemporal(CharacterIds id)
        {
            _zendreImg.color = colorDisponible;
            _aylaImg.color = colorDisponible;

            if (id == CharacterIds.Zendre)
                _zendreImg.color = colorElegidoPorMi;
            if (id == CharacterIds.Ayla)
                _aylaImg.color = colorElegidoPorMi;
        }

        private void MarcarListo()
        {
            GameSessionManager.Instance.MarcarListoRpc();

            ready.interactable = false;
            if (readyButtonText != null)
                readyButtonText.text = "¡Listo!";
            if (statusText != null)
                statusText.text = "Esperando al otro jugador...";
        }

        private void ActualizarUI(NetworkListEvent<PlayerSelectionState> _)
        {
            ulong miId = NetworkManager.Singleton.LocalClientId;

            CharacterIds miPersonaje = CharacterIds.None;
            bool yaListo = false;
            bool zendreTomadoPorOtro = false;
            bool aylaTomadoPorOtro = false;

            foreach (var j in GameSessionManager.Instance.Jugadores)
            {
                if (j.ClientId == miId)
                {
                    miPersonaje = j.Character;
                    yaListo = j.Ready;
                    continue;
                }
                if (j.Character == CharacterIds.Zendre)
                    zendreTomadoPorOtro = true;
                if (j.Character == CharacterIds.Ayla)
                    aylaTomadoPorOtro = true;
            }

            PintarBoton(
                zendreButton,
                _zendreImg,
                CharacterIds.Zendre,
                miPersonaje,
                zendreTomadoPorOtro,
                yaListo
            );
            PintarBoton(
                aylaButton,
                _aylaImg,
                CharacterIds.Ayla,
                miPersonaje,
                aylaTomadoPorOtro,
                yaListo
            );

            ready.interactable = miPersonaje != CharacterIds.None && !yaListo;

            if (readyButtonText != null && !yaListo)
                readyButtonText.text = "Listo";

            if (statusText != null && !yaListo)
                statusText.text =
                    miPersonaje == CharacterIds.None
                        ? "Elige tu personaje"
                        : "Presiona Listo para confirmar";
        }

        private void PintarBoton(
            Button btn,
            Image img,
            CharacterIds idDeEsteBoton,
            CharacterIds miPersonaje,
            bool tomadoPorOtro,
            bool yaListo
        )
        {
            bool esElMio = miPersonaje == idDeEsteBoton;

            if (esElMio)
            {
                img.color = colorElegidoPorMi;
                btn.interactable = false;
            }
            else if (tomadoPorOtro || yaListo)
            {
                img.color = colorBloqueado;
                btn.interactable = false;
            }
            else
            {
                img.color = colorDisponible;
                btn.interactable = true;
            }
        }
    }
}
