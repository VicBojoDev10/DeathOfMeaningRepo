using TDOM.Contracts;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TDOM.Unity.Input
{
    public class PlayerInputReader : MonoBehaviour
    {
        private PlayerInputActions _actions;
        private CharacterIds _personajeActivo;
        private InputAction _barrier;

        private void Awake()
        {
            _actions = new PlayerInputActions();
            _barrier = new InputAction("Barrier", InputActionType.Button);
            _barrier.AddBinding("<Gamepad>/leftShoulder");
        }

        public bool PausaPresionada => _actions.UI.Pause.WasPressedThisFrame();

        private void OnDestroy() { _actions.Dispose(); _barrier?.Dispose(); }

        public void ActivarPersonaje(CharacterIds id)
        {
            _personajeActivo = id;
            _actions.Zendre.Disable();
            _actions.Ayla.Disable();
            _barrier.Disable();
            _actions.UI.Pause.Enable();

            if (id == CharacterIds.Zendre)
            {
                _actions.Zendre.Enable();
                _barrier.Enable();
            }
            else
                _actions.Ayla.Enable();
        }

        public void SetEntradaHabilitada(bool habilitada)
        {
            if (!habilitada)
            {
                _actions.Zendre.Disable();
                _actions.Ayla.Disable();
                _barrier.Disable();
                // Pause queda activa para permitir reanudar desde el menú.
                return;
            }

            if (_personajeActivo != CharacterIds.None)
                ActivarPersonaje(_personajeActivo);
        }

        public InputSnapshot Read()
        {
            if (_personajeActivo == CharacterIds.Zendre)
            {
                var m = _actions.Zendre;
                bool dashPressed = m.Dash.WasPressedThisFrame();
                return new InputSnapshot(
                    move: m.Move.ReadValue<Vector2>(),
                    look: m.Look.ReadValue<Vector2>(),
                    jumpPressed: m.Jump.WasPressedThisFrame(),
                    jumpHeld: m.Jump.IsPressed(),
                    dashPressed: dashPressed,
                    sprintPressed: m.Sprint.WasPressedThisFrame(),
                    attackPressed: m.Attack.WasPressedThisFrame(),
                    attackHeld: m.Attack.IsPressed(),
                    attackReleased: m.Attack.WasReleasedThisFrame(),
                    aimHeld: m.Aim.IsPressed(),
                    firePressed: m.Fire.WasPressedThisFrame(),
                    fireHeld: m.Fire.IsPressed(),
                    fireReleased: m.Fire.WasReleasedThisFrame(),
                    grapplePressed: m.Grapple.WasPressedThisFrame(),
                    grappleHeld: m.Grapple.IsPressed()
                )
                {
                    LockOnPressed = m.LockOn.WasPressedThisFrame(),
                    BarrierHeld = _barrier.IsPressed(),
                };
            }
            else if (_personajeActivo == CharacterIds.Ayla)
            {
                var m = _actions.Ayla;
                return new InputSnapshot(
                    move: m.Move.ReadValue<Vector2>(),
                    look: m.Look.ReadValue<Vector2>(),
                    jumpPressed: m.Jump.WasPressedThisFrame(),
                    jumpHeld: m.Jump.IsPressed(),
                    dashPressed: m.Dash.WasPressedThisFrame(),
                    sprintPressed: m.Sprint.WasPressedThisFrame(),
                    attackPressed: m.Attack.WasPressedThisFrame(),
                    attackHeld: m.Attack.IsPressed(),
                    attackReleased: m.Attack.WasReleasedThisFrame(),
                    aimHeld: m.Aim.IsPressed(),
                    firePressed: false,
                    fireHeld: false,
                    fireReleased: false,
                    grapplePressed: m.Grapple.WasPressedThisFrame(),
                    grappleHeld: m.Grapple.IsPressed()
                )
                {
                    LockOnPressed = m.LockOn.WasPressedThisFrame(),
                };
            }

            return new InputSnapshot();
        }
    }
}
