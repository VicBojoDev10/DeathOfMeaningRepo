using System;
using TDOM.Unity.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace TDOM.Unity
{
    public class SplashUI : UIWindow
    {
        private IDisposable _anyButtonSubscription;
        private float _startTime;
        private bool _hasAdvanced;

        private void Awake()
        {
            if (firstSelectedObject == null)
            {
                firstSelectedObject = gameObject;
            }
        }

        private void Start()
        {
            _startTime = Time.unscaledTime;
            Show();
            if (UiManager.Instance != null)
            {
                UiManager.Instance.CloseWindow(WindowsIds.MainMenuUI);
                UiManager.Instance.CloseWindow(WindowsIds.ConnectionMenuUI);
            }
        }

        private void OnEnable()
        {
            _anyButtonSubscription?.Dispose();
            _anyButtonSubscription = InputSystem.onAnyButtonPress.Call(OnAnyButtonPressed);
        }

        private void OnDisable()
        {
            _anyButtonSubscription?.Dispose();
            _anyButtonSubscription = null;
        }

        private void Update()
        {
            if (_hasAdvanced || !IsOpen)
                return;

            if (Time.unscaledTime - _startTime < 0.15f)
                return;

            // Detección directa por dispositivo (teclado, mando y ratón)
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            {
                AvanzarAMenuPrincipal();
                return;
            }

            if (Gamepad.current != null)
            {
                foreach (var control in Gamepad.current.allControls)
                {
                    if (
                        control is UnityEngine.InputSystem.Controls.ButtonControl button
                        && button.wasPressedThisFrame
                    )
                    {
                        AvanzarAMenuPrincipal();
                        return;
                    }
                }
            }

            if (
                Mouse.current != null
                && (
                    Mouse.current.leftButton.wasPressedThisFrame
                    || Mouse.current.rightButton.wasPressedThisFrame
                )
            )
            {
                AvanzarAMenuPrincipal();
                return;
            }
        }

        private void OnAnyButtonPressed(InputControl control)
        {
            if (_hasAdvanced || !IsOpen)
                return;

            if (Time.unscaledTime - _startTime < 0.15f)
                return;

            AvanzarAMenuPrincipal();
        }

        public void AvanzarAMenuPrincipal()
        {
            if (_hasAdvanced)
                return;

            _hasAdvanced = true;

            if (UiManager.Instance != null)
            {
                UiManager.Instance.SwitchWindow(WindowsIds.SplashUI, WindowsIds.MainMenuUI);
            }
        }

        public override void Show()
        {
            base.Show();
            _hasAdvanced = false;
            _startTime = Time.unscaledTime;
        }
    }
}
