using TDOM.Contracts;
using TDOM.Unity.Character;
using UnityEngine;

namespace TDOM.Unity.Input
{
    public class PlayerInputReader : MonoBehaviour
    {
        private PlayerInputActions _actions;
        private CharacterId _personajeActivo;

        private void Awake() => _actions = new PlayerInputActions();

        private void OnDestroy() => _actions.Dispose();

        public void ActivarPersonaje(CharacterId id)
        {
            _personajeActivo = id;
            _actions.Zendre.Disable();
            _actions.Ayla.Disable();

            if (id == CharacterId.Zendre)
                _actions.Zendre.Enable();
            else
                _actions.Ayla.Enable();
        }

        public InputSnapshot Read()
        {
            if (_personajeActivo == CharacterId.Zendre)
            {
                var m = _actions.Zendre;
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
                    grapplePressed: m.Grapple.WasPressedThisFrame(),
                    firePressed: m.Fire.WasPressedThisFrame(),
                    fireHeld: m.Fire.IsPressed(),
                    fireReleased: m.Fire.WasReleasedThisFrame()
                );
            }
            else if (_personajeActivo == CharacterId.Ayla)
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
                    grapplePressed: false,
                    firePressed: false,
                    fireHeld: false,
                    fireReleased: false
                );
            }

            return new InputSnapshot(
                Vector2.zero,
                Vector2.zero,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false
            );
        }
    }
}
