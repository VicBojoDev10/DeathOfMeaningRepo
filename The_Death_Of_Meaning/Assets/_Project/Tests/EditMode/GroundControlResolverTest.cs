using NUnit.Framework;
using TDOM.Contracts;
using TDOM.Gameplay;
using UnityEngine;

namespace TDOM.Tests
{
    public sealed class GroundControlResolverTest
    {
        private const float Dt = 1f / 60f;

        [Test]
        public void Sin_input_el_personaje_frena_hasta_cero()
        {
            var resolver = new GroundControlResolver(4.5f, 6.5f, 25f, 30f, 0.3f);
            var estado = new LocomotionState
            {
                IsGrounded = true,
                Velocity = new Vector3(4.5f, 0, 0),
            };

            for (int i = 0; i < 60; i++)
            {
                resolver.Tick(estado, Vector2.zero, Quaternion.identity, false, Dt);
            }

            Assert.AreEqual(0f, estado.Velocity.x, 0.01f);
            Assert.AreEqual(0f, estado.Velocity.z, 0.01f);
        }

        [Test]
        public void Con_input_alcanza_la_velocidad_maxima()
        {
            var resolver = new GroundControlResolver(4.5f, 6.5f, 25f, 0f, 0.3f);
            var estado = new LocomotionState { IsGrounded = true, Velocity = Vector3.zero };
            var inputDir = new Vector2(0, 1);

            for (int i = 0; i < 60; i++)
            {
                resolver.Tick(estado, inputDir, Quaternion.identity, false, Dt);
            }

            Assert.AreEqual(4.5f, estado.Velocity.z, 0.01f);
        }

        [Test]
        public void Correr_llega_a_velocidad_mayor_que_caminar()
        {
            var resolver = new GroundControlResolver(4.5f, 10f, 60f, 0f, 0.7f);
            var estado = new LocomotionState { IsGrounded = true, Velocity = Vector3.zero };
            var inputDir = new Vector2(1, 0);

            for (int i = 0; i < 60; i++)
            {
                resolver.Tick(estado, inputDir, Quaternion.identity, true, Dt);
            }

            Assert.AreEqual(10f, estado.Velocity.x, 0.01f);
        }

        [Test]
        public void El_control_aereo_acelera_menos_que_en_suelo()
        {
            var resolver = new GroundControlResolver(5f, 10f, 10f, 0f, 0.1f);

            var estadoSuelo = new LocomotionState { IsGrounded = true, Velocity = Vector3.zero };
            var estadoAire = new LocomotionState { IsGrounded = false, Velocity = Vector3.zero };

            resolver.Tick(estadoSuelo, new Vector2(1, 0), Quaternion.identity, false, Dt);
            resolver.Tick(estadoAire, new Vector2(1, 0), Quaternion.identity, false, Dt);

            Assert.Less(estadoAire.Velocity.x, estadoSuelo.Velocity.x);
        }

        [Test]
        public void El_movimiento_horizontal_no_modifica_la_velocidad_vertical()
        {
            var resolver = new GroundControlResolver(5f, 10f, 10f, 0f, 1f);
            var estado = new LocomotionState
            {
                IsGrounded = false,
                Velocity = new Vector3(0, -15f, 0),
            };

            resolver.Tick(estado, new Vector2(1, 0), Quaternion.identity, false, Dt);

            Assert.AreEqual(-15f, estado.Velocity.y);
        }

        [Test]
        public void El_input_se_orienta_segun_el_yaw_de_la_camara()
        {
            var resolver = new GroundControlResolver(5f, 10f, 100f, 0f, 1f);
            var estado = new LocomotionState { IsGrounded = true, Velocity = Vector3.zero };

            var inputMove = new Vector2(0, 1);
            var cameraYaw = Quaternion.Euler(0, 90f, 0);

            for (int i = 0; i < 60; i++)
            {
                resolver.Tick(estado, inputMove, cameraYaw, false, Dt);
            }

            Assert.AreEqual(5f, estado.Velocity.x, 0.1f);
            Assert.AreEqual(0f, estado.Velocity.z, 0.1f);
        }
    }
}
