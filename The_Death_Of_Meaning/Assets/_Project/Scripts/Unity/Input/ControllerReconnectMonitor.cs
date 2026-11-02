using TDOM.Gameplay.Core;
using TDOM.Unity.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TDOM.Unity.Input
{
    public sealed class ControllerReconnectMonitor : MonoBehaviour
    {
        private const int SinGamepadActivo = -1;

        private readonly ControlReconnectFlow _flujo = new ControlReconnectFlow();
        private UiManager _uiManager;
        private ControllerReconnectUI _ventana;
        private int _gamepadActivoId = SinGamepadActivo;
        private int _gamepadDesconectadoId = SinGamepadActivo;

        private void Awake()
        {
            _uiManager = GetComponent<UiManager>();
        }

        private void OnEnable()
        {
            InputSystem.onDeviceChange += OnDeviceChange;
            ActualizarGamepadActivo();
        }

        private void OnDisable()
        {
            InputSystem.onDeviceChange -= OnDeviceChange;
        }

        private void Update()
        {
            if (Gamepad.current != null)
                _gamepadActivoId = Gamepad.current.deviceId;

            if (!_flujo.PopupVisible || _flujo.PuedeContinuar)
                return;

            foreach (var gamepad in Gamepad.all)
            {
                if (gamepad.deviceId != _gamepadDesconectadoId)
                {
                    _gamepadActivoId = gamepad.deviceId;
                    RegistrarReconexion();
                    return;
                }
            }
        }

        private void ActualizarGamepadActivo()
        {
            if (Gamepad.current != null)
            {
                _gamepadActivoId = Gamepad.current.deviceId;
                return;
            }

            if (Gamepad.all.Count > 0)
                _gamepadActivoId = Gamepad.all[0].deviceId;
        }

        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (device is not Gamepad gamepad)
                return;

            if (change == InputDeviceChange.Disconnected)
            {
                if (gamepad.deviceId != _gamepadActivoId)
                    return;

                _gamepadDesconectadoId = gamepad.deviceId;
                _flujo.RegistrarDesconexion();
                MostrarVentana();
                return;
            }

            if (change != InputDeviceChange.Reconnected && change != InputDeviceChange.Added)
                return;

            if (!_flujo.PopupVisible)
                return;

            if (change != InputDeviceChange.Added && gamepad.deviceId != _gamepadDesconectadoId)
                return;

            _gamepadActivoId = gamepad.deviceId;
            RegistrarReconexion();
        }

        private void RegistrarReconexion()
        {
            _flujo.RegistrarReconexion();
            ObtenerVentana()?.EstablecerPuedeContinuar(_flujo.PuedeContinuar);
        }

        private void MostrarVentana()
        {
            var ventana = ObtenerVentana();
            if (ventana == null)
            {
                Debug.LogError($"No se encontró la ventana {WindowsIds.ControllerReconnectUI}");
                return;
            }

            ventana.Configurar(ConfirmarContinuacion);
            ventana.EstablecerPuedeContinuar(_flujo.PuedeContinuar);
            _uiManager.ShowWindow(WindowsIds.ControllerReconnectUI);
        }

        private ControllerReconnectUI ObtenerVentana()
        {
            if (_ventana == null && _uiManager != null)
            {
                _ventana =
                    _uiManager.GetWindow(WindowsIds.ControllerReconnectUI) as ControllerReconnectUI;
            }

            return _ventana;
        }

        private void ConfirmarContinuacion()
        {
            if (!_flujo.ConfirmarContinuacion())
                return;

            _ventana?.EstablecerPuedeContinuar(false);
            _uiManager?.CloseWindow(WindowsIds.ControllerReconnectUI);
            _gamepadActivoId =
                Gamepad.current != null ? Gamepad.current.deviceId : SinGamepadActivo;
            _gamepadDesconectadoId = SinGamepadActivo;
        }
    }
}
