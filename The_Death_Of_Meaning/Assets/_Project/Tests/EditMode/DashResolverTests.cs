using NUnit.Framework;
using TDOM.Contracts;
using TDOM.Data;
using TDOM.Gameplay;
using TDOM.Gameplay.Core;
using UnityEngine;

namespace TDOM.Tests.EditMode
{
    public class DashResolverTests
    {
        private DashResolver CrearAylaDash()
        {
            var perfil = ScriptableObject.CreateInstance<DashProfile>();
            perfil.Distance = 8f;
            perfil.Duration = 0.18f;
            perfil.Cooldown = 4f;
            perfil.MaxTurnRate = 0f;
            perfil.Easing = AnimationCurve.Constant(0f, 1f, 1f);
            return new DashResolver(perfil);
        }

        private DashResolver CrearZendreDash()
        {
            var perfil = ScriptableObject.CreateInstance<DashProfile>();
            perfil.Distance = 10f;
            perfil.Duration = 0.8f;
            perfil.Cooldown = 7f;
            perfil.MaxTurnRate = 30f;
            perfil.Easing = AnimationCurve.Constant(0f, 1f, 1f);
            return new DashResolver(perfil);
        }

        private float SimularDashCompleto(DashResolver dash, float dt)
        {
            var estado = new LocomotionState();
            dash.TryIniciar(Vector3.forward);

            float distanciaTotal = 0f;
            int maxFrames = 1000;
            int frame = 0;

            while (dash.Activo && frame < maxFrames)
            {
                dash.Tick(estado, Vector2.zero, dt);

                distanciaTotal += estado.Velocity.magnitude * dt;

                frame++;
            }
            return distanciaTotal;
        }

        [Test]
        public void El_dash_recorre_la_distancia_configurada()
        {
            var dash = CrearAylaDash();
            float distanciaRecorrida = SimularDashCompleto(dash, 0.016f);
            Assert.AreEqual(8f, distanciaRecorrida, 0.01f);
        }

        [Test]
        public void No_se_puede_iniciar_durante_el_cooldown()
        {
            var dash = CrearAylaDash();
            Assert.IsTrue(dash.TryIniciar(Vector3.forward));
            dash.Cancelar();
            Assert.IsFalse(dash.TryIniciar(Vector3.right));
        }

        [Test]
        public void No_se_puede_iniciar_si_ya_esta_activo()
        {
            var dash = CrearAylaDash();
            Assert.IsTrue(dash.TryIniciar(Vector3.forward));
            Assert.IsFalse(dash.TryIniciar(Vector3.right));
        }

        [Test]
        public void Termina_exactamente_al_cumplir_la_duracion()
        {
            var dash = CrearAylaDash();
            var estado = new LocomotionState();

            dash.TryIniciar(Vector3.forward);
            dash.Tick(estado, Vector2.zero, 0.17f);
            Assert.IsTrue(dash.Activo);

            dash.Tick(estado, Vector2.zero, 0.01f);
            dash.Tick(estado, Vector2.zero, 0.001f);
            Assert.IsFalse(dash.Activo);
        }

        [Test]
        public void La_gravedad_queda_suspendida_durante_el_dash()
        {
            var dash = CrearAylaDash();
            var estado = new LocomotionState { Velocity = new Vector3(0, -9.81f, 0) };

            dash.TryIniciar(Vector3.forward);
            dash.Tick(estado, Vector2.zero, 0.016f);

            Assert.AreEqual(0f, estado.Velocity.y);
            Assert.AreEqual(LocomotionPhase.Dashing, estado.Phase);
        }

        [Test]
        public void Ayla_no_puede_corregir_la_direccion_a_media_ejecucion()
        {
            var dash = CrearAylaDash();
            var estado = new LocomotionState();

            dash.TryIniciar(Vector3.forward);
            dash.Tick(estado, new Vector2(1, 0), 0.1f);

            Assert.AreEqual(Vector3.forward, estado.Velocity.normalized);
        }

        [Test]
        public void Zendre_puede_girar_dentro_del_limite_configurado()
        {
            var dash = CrearZendreDash();
            var estado = new LocomotionState();

            dash.TryIniciar(Vector3.forward);

            float dt = 0.1f;
            dash.Tick(estado, new Vector2(1, 0), dt);

            Vector3 direccionEsperada = Vector3.RotateTowards(
                Vector3.forward,
                Vector3.right,
                30f * Mathf.Deg2Rad * dt,
                0f
            );
            float diferenciaAngular = Vector3.Angle(direccionEsperada, estado.Velocity.normalized);

            Assert.IsTrue(diferenciaAngular < 0.01f);
        }

        [Test]
        public void La_distancia_es_igual_con_dt_grande_y_dt_pequeno()
        {
            float dtFino = 0.0166f;
            float dtTosco = 0.0333f;

            float distanciaFina = SimularDashCompleto(CrearAylaDash(), dtFino);
            float distanciaTosca = SimularDashCompleto(CrearAylaDash(), dtTosco);

            Assert.AreEqual(distanciaFina, distanciaTosca, 0.001f);
        }
    }
}
