using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

namespace TDOM.Unity
{
    public class CancelHostUI : UIWindow
    {
        public Button cancelButton;
        public TextMeshProUGUI timerText;

        private void Awake()
        {
            if (timerText != null)
            {
                timerText.raycastTarget = false;
                timerText.text = string.Empty;
            }
        }

        private void OnEnable()
        {
            if (cancelButton != null)
                cancelButton.onClick.AddListener(CancelarConexion);
        }

        private void OnDisable()
        {
            if (cancelButton != null)
                cancelButton.onClick.RemoveListener(CancelarConexion);
        }

        private void Update()
        {
            if (!IsOpen)
                return;

            var manager = ConnectionManager.Instance;
            if (timerText != null)
            {
                timerText.text = manager != null && manager.ClienteEsperandoConexion
                    ? $"{manager.SegundosRestantesConexion}..."
                    : string.Empty;
            }

            bool cancelar = Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame;
            cancelar |= Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
            if (cancelar)
                CancelarConexion();
        }

        public void CancelarConexion()
        {
            var manager = ConnectionManager.Instance;
            if (manager == null)
                return;

            if (manager.EstadoActual == TDOM.Gameplay.Core.EstadoSesion.Conectando
                || manager.EstadoActual == TDOM.Gameplay.Core.EstadoSesion.Listo
                || manager.EstadoActual == TDOM.Gameplay.Core.EstadoSesion.EsperandoJugador)
                manager.CancelarConexion();
        }
    }
}
