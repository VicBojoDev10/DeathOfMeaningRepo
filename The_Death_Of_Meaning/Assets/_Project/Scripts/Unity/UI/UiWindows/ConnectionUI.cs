using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TDOM.Unity
{
    public class ConnectionUI : UIWindow
    {
        public ConnectionManager connectionManager;
        public TextMeshProUGUI ipText;
        public Button startHostButton;
        public Button startClientButton;

        void Start()
        {
            Show();
        }


        private void OnEnable()
        {
            startHostButton.onClick.AddListener(StartHostButton_OnClick);
            startClientButton.onClick.AddListener(StartClientButton_OnClick);
        }

        private void OnDisable()
        {
            startHostButton.onClick.RemoveListener(StartHostButton_OnClick);
            startClientButton.onClick.RemoveListener(StartClientButton_OnClick);
        }

        private void StartHostButton_OnClick()
        {
            connectionManager.OnCrearPartida();
            startHostButton.onClick.RemoveAllListeners();
        }

        private void StartClientButton_OnClick()
        {
            connectionManager.OnUnirse(ipText.text);
            startClientButton.onClick.RemoveAllListeners();
        }

    }
}
