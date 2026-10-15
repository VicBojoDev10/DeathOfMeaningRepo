using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
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
        public Color colorHover = new Color(0.9608f, 0.9608f, 0.9608f, 1f);
        public Color colorEnFoco = new Color(0.9451f, 0.8902f, 0.1569f, 1f);
        public Color colorElegidoPorMi = new Color(0.3f, 0.7f, 1f);
        public Color colorBloqueado = new Color(0.5f, 0.5f, 0.5f);

        private Image _zendreImg;
        private Image _aylaImg;
        private GameSessionManager _manager;

        private void Awake()
        {
            if (zendreButton != null)
                _zendreImg = zendreButton.GetComponent<Image>();
            if (aylaButton != null)
                _aylaImg = aylaButton.GetComponent<Image>();
            if (firstSelectedObject == null && zendreButton != null)
                firstSelectedObject = zendreButton.gameObject;
            if (ready != null)
                ready.interactable = false;
            ConfigurarNavegacion();
        }

        private void OnEnable()
        {
            if (ready != null)
                ready.interactable = false;
            if (zendreButton != null)
                zendreButton.onClick.AddListener(SeleccionarZendre);
            if (aylaButton != null)
                aylaButton.onClick.AddListener(SeleccionarAyla);
            if (ready != null)
                ready.onClick.AddListener(MarcarListo);
            GameSessionManager.Spawned += EnlazarManager;
            GameSessionManager.Despawned += DesenlazarManager;
            EnlazarManager();
        }

        public override void Show()
        {
            base.Show();
            EnlazarManager();
        }

        private void OnDisable()
        {
            if (zendreButton != null)
                zendreButton.onClick.RemoveListener(SeleccionarZendre);
            if (aylaButton != null)
                aylaButton.onClick.RemoveListener(SeleccionarAyla);
            if (ready != null)
                ready.onClick.RemoveListener(MarcarListo);
            GameSessionManager.Spawned -= EnlazarManager;
            GameSessionManager.Despawned -= DesenlazarManager;
            DesenlazarManager();
        }

        private void EnlazarManager()
        {
            var manager = GameSessionManager.Instance;
            if (manager == null || !manager.IsSpawned || _manager == manager)
                return;
            DesenlazarManager();
            _manager = manager;
            _manager.Jugadores.OnListChanged += ActualizarUI;
            _manager.OnJugadorDesconectado += MostrarMensajeDesconexion;
            _manager.PersonajeRechazado += MostrarRechazo;
            ActualizarUI(default);
        }

        private void DesenlazarManager()
        {
            if (_manager == null)
                return;
            _manager.Jugadores.OnListChanged -= ActualizarUI;
            _manager.OnJugadorDesconectado -= MostrarMensajeDesconexion;
            _manager.PersonajeRechazado -= MostrarRechazo;
            _manager = null;
            if (ready != null)
                ready.interactable = false;
        }

        private void SeleccionarZendre() => ElegirPersonaje(CharacterIds.Zendre);

        private void SeleccionarAyla() => ElegirPersonaje(CharacterIds.Ayla);

        private void ElegirPersonaje(CharacterIds id)
        {
            if (_manager == null)
                return;
            AplicarColorTemporal(id);
            _manager.ElegirPersonajeRpc(id);
        }

        private void AplicarColorTemporal(CharacterIds id)
        {
            if (_zendreImg != null)
                _zendreImg.color = id == CharacterIds.Zendre ? colorElegidoPorMi : colorDisponible;
            if (_aylaImg != null)
                _aylaImg.color = id == CharacterIds.Ayla ? colorElegidoPorMi : colorDisponible;
        }

        private void MarcarListo()
        {
            if (_manager != null)
                _manager.MarcarListoRpc();
        }

        private void MostrarMensajeDesconexion()
        {
            if (statusText != null)
                statusText.text = "El otro jugador se desconectó";
        }

        private void MostrarRechazo(CharacterIds character)
        {
            ActualizarUI(default);
            if (statusText != null)
                statusText.text =
                    character == CharacterIds.Ayla
                        ? "Ayla ya fue elegida. Elige otro personaje."
                        : "Zendre ya fue elegido. Elige otro personaje.";
        }

        private void ActualizarUI(NetworkListEvent<PlayerSelectionState> _)
        {
            if (_manager == null || NetworkManager.Singleton == null)
                return;

            ulong miId = NetworkManager.Singleton.LocalClientId;
            CharacterIds miPersonaje = CharacterIds.None;
            bool yaListo = false;
            bool zendreTomadoPorOtro = false;
            bool aylaTomadoPorOtro = false;
            foreach (var jugador in _manager.Jugadores)
            {
                if (jugador.ClientId == miId)
                {
                    miPersonaje = jugador.Character;
                    yaListo = jugador.Ready;
                }
                else if (jugador.Character == CharacterIds.Zendre)
                    zendreTomadoPorOtro = true;
                else if (jugador.Character == CharacterIds.Ayla)
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
            if (ready != null)
                ready.interactable = miPersonaje != CharacterIds.None && !yaListo;
            if (readyButtonText != null)
                readyButtonText.text = yaListo ? "¡Listo!" : "Listo";
            if (statusText != null)
                statusText.text =
                    yaListo ? "Esperando al otro jugador..."
                    : miPersonaje == CharacterIds.None ? "Elige tu personaje"
                    : "Presiona Listo para confirmar";
            ConfigurarNavegacion();
            RecuperarFocoSiHaceFalta();
        }

        private void ConfigurarNavegacion()
        {
            bool zendreAvailable = zendreButton != null && zendreButton.interactable;
            bool aylaAvailable = aylaButton != null && aylaButton.interactable;
            bool readyAvailable = ready != null && ready.interactable;

            ConfigurarBotonNavegacion(
                zendreButton,
                aylaAvailable ? aylaButton : null,
                null,
                readyAvailable ? ready : null,
                null
            );
            ConfigurarBotonNavegacion(
                aylaButton,
                null,
                zendreAvailable ? zendreButton : null,
                readyAvailable ? ready : null,
                null
            );

            Button personajeDisponible =
                zendreAvailable ? zendreButton
                : aylaAvailable ? aylaButton
                : null;
            ConfigurarBotonNavegacion(
                ready,
                personajeDisponible,
                personajeDisponible,
                null,
                personajeDisponible
            );
        }

        private static void ConfigurarBotonNavegacion(
            Button button,
            Selectable right,
            Selectable left,
            Selectable down,
            Selectable up
        )
        {
            if (button == null)
                return;
            var navigation = button.navigation;
            navigation.mode = Navigation.Mode.Explicit;
            navigation.selectOnRight = right;
            navigation.selectOnLeft = left;
            navigation.selectOnDown = down;
            navigation.selectOnUp = up;
            button.navigation = navigation;
        }

        private void RecuperarFocoSiHaceFalta()
        {
            if (EventSystem.current == null)
                return;
            var selected = EventSystem.current.currentSelectedGameObject;
            bool focusLost =
                selected == null
                || (
                    zendreButton != null
                    && selected == zendreButton.gameObject
                    && !zendreButton.interactable
                )
                || (
                    aylaButton != null
                    && selected == aylaButton.gameObject
                    && !aylaButton.interactable
                )
                || (ready != null && selected == ready.gameObject && !ready.interactable);
            if (!focusLost)
                return;

            Button target =
                ready != null && ready.interactable ? ready
                : zendreButton != null && zendreButton.interactable ? zendreButton
                : aylaButton != null && aylaButton.interactable ? aylaButton
                : null;
            if (target != null)
                EventSystem.current.SetSelectedGameObject(target.gameObject);
        }

        private void PintarBoton(
            Button button,
            Image image,
            CharacterIds character,
            CharacterIds mine,
            bool takenByOther,
            bool readyState
        )
        {
            if (button == null)
                return;
            bool mineSelected = mine == character;
            bool blocked = !mineSelected && (takenByOther || readyState);
            Color color =
                mineSelected ? colorElegidoPorMi
                : blocked ? colorBloqueado
                : colorDisponible;
            if (image != null)
                image.color = color;
            button.interactable = !mineSelected && !blocked;

            var colors = button.colors;
            colors.normalColor = colorDisponible;
            colors.highlightedColor = blocked || mineSelected ? color : colorHover;
            colors.pressedColor = blocked || mineSelected ? color : colorDisponible;
            colors.selectedColor = blocked || mineSelected ? color : colorEnFoco;
            colors.disabledColor = color;
            button.colors = colors;
        }
    }
}
