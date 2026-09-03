using NUnit.Framework;
using TDOM.Contracts;
using TDOM.Gameplay;
using UnityEngine;

namespace TDOM.Tests
{
    public sealed class JumpResolverTests
    {
        private const float Dt = 1f / 60f;

        private InputSnapshot ConSalto() => new InputSnapshot(true, true);

        private InputSnapshot SinInput() => new InputSnapshot(false, true);

        [Test]
        public void Ayla_puede_saltar_dos_veces_en_el_aire()
        {
            var resolver = new JumpResolver(
                maxSaltos: 2,
                velocidadSalto: 15f,
                coyoteTime: 0.12f,
                bufferTime: 0.15f
            );
            var estado = new LocomotionState
            {
                IsGrounded = false,
                JumpsUsed = 0,
                Velocity = Vector3.zero,
            };

            resolver.Tick(estado, ConSalto(), Dt);
            estado.Velocity.y = 5f;
            resolver.Tick(estado, ConSalto(), Dt);

            Assert.AreEqual(15f, estado.Velocity.y);
            Assert.AreEqual(2, estado.JumpsUsed);
        }

        [Test]
        public void Zendre_solo_puede_saltar_una_vez()
        {
            var resolver = new JumpResolver(
                maxSaltos: 1,
                velocidadSalto: 15f,
                coyoteTime: 0.12f,
                bufferTime: 0.15f
            );
            var estado = new LocomotionState { IsGrounded = true, JumpsUsed = 0 };

            resolver.Tick(estado, ConSalto(), Dt);
            estado.IsGrounded = false;
            resolver.Tick(estado, ConSalto(), Dt);

            Assert.AreEqual(1, estado.JumpsUsed);
        }

        [Test]
        public void Al_tocar_suelo_se_reinicia_el_contador_de_saltos()
        {
            var resolver = new JumpResolver(
                maxSaltos: 1,
                velocidadSalto: 15f,
                coyoteTime: 0.12f,
                bufferTime: 0.15f
            );
            var estado = new LocomotionState { IsGrounded = false, JumpsUsed = 1 };

            estado.IsGrounded = true;
            resolver.Tick(estado, SinInput(), Dt);

            Assert.AreEqual(0, estado.JumpsUsed);
        }

        [Test]
        public void Coyote_time_permite_saltar_justo_despues_del_borde()
        {
            var resolver = new JumpResolver(
                maxSaltos: 1,
                velocidadSalto: 15f,
                coyoteTime: 0.12f,
                bufferTime: 0.15f
            );
            var estado = new LocomotionState
            {
                IsGrounded = false,
                JumpsUsed = 0,
                CoyoteTimer = 0.12f,
            };

            resolver.Tick(estado, SinInput(), Dt);
            resolver.Tick(estado, ConSalto(), Dt);

            Assert.AreEqual(1, estado.JumpsUsed);
            Assert.AreEqual(15f, estado.Velocity.y);
        }

        [Test]
        public void Coyote_time_se_consume_al_saltar()
        {
            var resolver = new JumpResolver(
                maxSaltos: 1,
                velocidadSalto: 15f,
                coyoteTime: 0.12f,
                bufferTime: 0.15f
            );
            var estado = new LocomotionState
            {
                IsGrounded = false,
                JumpsUsed = 0,
                CoyoteTimer = 0.10f,
            };

            resolver.Tick(estado, ConSalto(), Dt);

            Assert.AreEqual(0f, estado.CoyoteTimer);
        }

        [Test]
        public void Buffer_ejecuta_el_salto_al_aterrizar()
        {
            var resolver = new JumpResolver(
                maxSaltos: 1,
                velocidadSalto: 15f,
                coyoteTime: 0.12f,
                bufferTime: 0.15f
            );
            var estado = new LocomotionState
            {
                IsGrounded = false,
                JumpsUsed = 1,
                BufferTimer = 0f,
            };

            resolver.Tick(estado, ConSalto(), Dt);
            estado.IsGrounded = true;
            resolver.Tick(estado, SinInput(), Dt);

            Assert.AreEqual(15f, estado.Velocity.y);
            Assert.AreEqual(0f, estado.BufferTimer);
        }
    }
}
