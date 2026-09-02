using NUnit.Framework;
using TDOM.Contracts;
using TDOM.Gameplay.Locomotion;
using UnityEngine;

namespace TDOM.Tests.EditMode
{
    public sealed class GravityModelTests
    {
        private InputSnapshot SinInput() =>
            new InputSnapshot { JumpPressed = false, JumpHeld = false };

        [Test]
        public void Soltar_el_boton_subiendo_acorta_el_salto()
        {
            var gravity = new GravityModel(
                gravedad: -10f,
                velocidadTerminal: -50f,
                multiplicadorSaltoCorto: 2.0f
            );
            var estado = new LocomotionState
            {
                IsGrounded = false,
                Velocity = new Vector3(0, 10f, 0),
            };

            gravity.Aplicar(estado, SinInput(), 1f);

            Assert.AreEqual(-10f, estado.Velocity.y);
        }

        [Test]
        public void La_velocidad_no_pasa_de_la_terminal()
        {
            var gravity = new GravityModel(
                gravedad: -100f,
                velocidadTerminal: -50f,
                multiplicadorSaltoCorto: 1f
            );
            var estado = new LocomotionState
            {
                IsGrounded = false,
                Velocity = new Vector3(0, -45f, 0),
            };

            gravity.Aplicar(estado, SinInput(), 1f);

            Assert.AreEqual(-50f, estado.Velocity.y);
        }
    }
}
