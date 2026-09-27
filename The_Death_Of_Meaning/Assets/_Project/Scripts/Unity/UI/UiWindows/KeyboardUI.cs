using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TDOM.Unity
{
    public class KeyboardUI : UIWindow
    {
        [Header("Teclas del Teclado")]
        public List<Button> kbButtonList = new List<Button>();

        public event Action OnKeyboardClosed;

        private TMP_InputField _targetInputField;
        private GameObject _previousSelectedObject;
        private UIWindow _callerWindow;
        private bool _isInitialized = false;

        public TMP_InputField TargetInputField => _targetInputField;

        private void Awake()
        {
            SetupKeyButtons();
        }

        public override void Initialize()
        {
            base.Initialize();
            SetupKeyButtons();
        }

        private void SetupKeyButtons()
        {
            if (_isInitialized)
                return;

            if (kbButtonList == null || kbButtonList.Count == 0)
            {
                var buttons = GetComponentsInChildren<Button>(true);
                kbButtonList = new List<Button>(buttons);
            }

            kbButtonList.RemoveAll(b => b == null);

            foreach (var btn in kbButtonList)
            {
                if (btn == null)
                    continue;

                string keyName = btn.gameObject.name;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => HandleKeyPress(keyName));
            }

            if (kbButtonList.Count > 0 && firstSelectedObject == null)
            {
                firstSelectedObject = kbButtonList[0].gameObject;
            }

            _isInitialized = true;
        }

        private void HandleKeyPress(string keyName)
        {
            if (keyName.Equals("KeyBakc", StringComparison.OrdinalIgnoreCase) ||
                keyName.Equals("KeyBack", StringComparison.OrdinalIgnoreCase) ||
                keyName.Equals("KeyBackspace", StringComparison.OrdinalIgnoreCase))
            {
                OnBackspacePressed();
                return;
            }

            if (keyName.Equals("Key Enter", StringComparison.OrdinalIgnoreCase) ||
                keyName.Equals("KeyEnter", StringComparison.OrdinalIgnoreCase))
            {
                OnSubmitPressed();
                return;
            }

            string character = ParseKeyCharacter(keyName);
            if (!string.IsNullOrEmpty(character))
            {
                OnCharacterPressed(character);
            }
        }

        private string ParseKeyCharacter(string keyName)
        {
            if (keyName.StartsWith("Key", StringComparison.OrdinalIgnoreCase) && keyName.Length > 3)
            {
                string suffix = keyName.Substring(3);

                if (suffix.Length == 1)
                    return suffix;

                return suffix.ToLowerInvariant() switch
                {
                    "point" or "dot" => ".",
                    "space" => " ",
                    "less" or "minus" => "-",
                    "more" or "plus" => "+",
                    "asterisk" => "*",
                    "slash" => "/",
                    "colon" => ":",
                    _ => suffix
                };
            }

            return "";
        }

        private void OnCharacterPressed(string character)
        {
            if (_targetInputField == null)
                return;

            _targetInputField.text += character;
            _targetInputField.onValueChanged?.Invoke(_targetInputField.text);
        }

        private void OnBackspacePressed()
        {
            if (_targetInputField == null || string.IsNullOrEmpty(_targetInputField.text))
                return;

            _targetInputField.text = _targetInputField.text.Substring(0, _targetInputField.text.Length - 1);
            _targetInputField.onValueChanged?.Invoke(_targetInputField.text);
        }

        private void OnSubmitPressed()
        {
            CerrarTeclado();
        }

        public void AbrirParaInputField(TMP_InputField target, UIWindow caller = null)
        {
            if (target == null)
                return;

            _targetInputField = target;
            _callerWindow = caller != null ? caller : target.GetComponentInParent<UIWindow>();

            if (_callerWindow != null)
            {
                _callerWindow.SetInteractable(false);
            }

            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
            {
                // Guardar solo si no es un botón del propio teclado
                if (!kbButtonList.Exists(b => b != null && b.gameObject == EventSystem.current.currentSelectedGameObject))
                {
                    _previousSelectedObject = EventSystem.current.currentSelectedGameObject;
                }
            }

            if (kbButtonList.Count > 0)
            {
                firstSelectedObject = kbButtonList[0].gameObject;
            }

            Show();
        }

        public void CerrarTeclado()
        {
            Hide();

            if (_targetInputField != null)
            {
                _targetInputField.interactable = true;
                _targetInputField.DeactivateInputField();
            }

            if (_callerWindow != null)
            {
                _callerWindow.SetInteractable(true);
                _callerWindow = null;
            }

            OnKeyboardClosed?.Invoke();

            GameObject targetToFocus = _previousSelectedObject != null 
                ? _previousSelectedObject 
                : (_targetInputField != null ? _targetInputField.gameObject : null);

            if (targetToFocus != null)
            {
                FocusElement(targetToFocus);
            }
        }

        private void Update()
        {
            if (IsOpen)
            {
                // Detectar botón Círculo (Gamepad buttonEast / Cancel / Escape)
                bool cancelPressed = false;

                if (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame)
                {
                    cancelPressed = true;
                }
                else if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    cancelPressed = true;
                }

                if (cancelPressed)
                {
                    CerrarTeclado();
                }
            }
        }
    }
}
