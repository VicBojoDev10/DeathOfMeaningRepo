using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TDOM.Unity
{
    public class CancelHostUI : UIWindow
    {
        public Button cancelButton;
        public TextMeshProUGUI timerText;
        public TextMeshProUGUI hostIpText;
        public event Action OnCancelarSolicitado;

        private void Awake()
        {
            if (hostIpText != null)
            {
                hostIpText.raycastTarget = false;
                hostIpText.text = string.Empty;
            }
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

        public void ActualizarTiempo(int segundos)
        {
            if (timerText != null)
                timerText.text = segundos >= 0 ? $"{segundos}..." : string.Empty;
        }

        public void ConfigurarModoHost(bool esHost)
        {
            if (hostIpText == null)
                return;

            hostIpText.gameObject.SetActive(esHost);
            if (!esHost)
                ActualizarIpHost(string.Empty);
        }

        public void ActualizarIpHost(string ip)
        {
            if (hostIpText != null)
                hostIpText.text = string.IsNullOrWhiteSpace(ip) ? string.Empty : $"IP: {ip}";
        }

        public void CancelarConexion() => OnCancelarSolicitado?.Invoke();
    }
}
