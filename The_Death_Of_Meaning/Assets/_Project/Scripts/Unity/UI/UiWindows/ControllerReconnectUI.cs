using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TDOM.Unity
{
    public sealed class ControllerReconnectUI : UIWindow
    {
        private const string MensajeControlDesconectado =
            "Control desconectado. Conéctalo para continuar.";
        private const string MensajeControlConectado =
            "Control conectado. Pulsa Continuar para seguir.";
        private const string TextoContinuar = "Continuar";

        [SerializeField]
        private Button _botonContinuar;

        [SerializeField]
        private TMP_Text _mensaje;

        [SerializeField]
        private TMP_Text _textoContinuar;

        private Action _continuar;

        private void OnEnable()
        {
            if (_botonContinuar != null)
                _botonContinuar.onClick.AddListener(OnContinuarPresionado);
        }

        private void OnDisable()
        {
            if (_botonContinuar != null)
                _botonContinuar.onClick.RemoveListener(OnContinuarPresionado);

            _continuar = null;
        }

        public void Configurar(Action continuar)
        {
            _continuar = continuar;

            if (_textoContinuar != null)
                _textoContinuar.text = TextoContinuar;

            EstablecerPuedeContinuar(false);
        }

        public void EstablecerPuedeContinuar(bool puedeContinuar)
        {
            if (_botonContinuar != null)
                _botonContinuar.interactable = puedeContinuar;

            if (_mensaje != null)
                _mensaje.text = puedeContinuar
                    ? MensajeControlConectado
                    : MensajeControlDesconectado;
        }

        private void OnContinuarPresionado()
        {
            _continuar?.Invoke();
        }
    }
}
