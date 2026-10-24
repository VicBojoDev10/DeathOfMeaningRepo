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
        public event Action OnCancelarSolicitado;

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

        public void ActualizarTiempo(int segundos)
        {
            if (timerText != null)
                timerText.text = segundos >= 0 ? $"{segundos}..." : string.Empty;
        }

        public void CancelarConexion() => OnCancelarSolicitado?.Invoke();
    }
}
