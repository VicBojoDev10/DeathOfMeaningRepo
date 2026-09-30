using NUnit.Framework;
using TDOM.Contracts;
using TDOM.Data;
using TDOM.Gameplay;
using UnityEngine;

namespace TDOM.Tests.EditMode
{
    public class GrappleResolverTests
    {
        private GrappleResolver CrearGrapple(
            float alcance = 30f,
            float velocidadTraccion = 25f,
            float cooldown = 3f,
            float distanciaLlegada = 1.5f,
            float duracionMax = 2f
        )
        {
            return new GrappleResolver(alcance, velocidadTraccion, cooldown, distanciaLlegada, duracionMax);
        }

        [Test]
        public void TryIniciar_falla_si_la_distancia_supera_el_alcance()
        {
            var grapple = CrearGrapple(alcance: 30f);
            Vector3 origen = Vector3.zero;
            Vector3 punto = new Vector3(0f, 0f, 31f);

            bool iniciado = grapple.TryIniciar(origen, punto);

            Assert.IsFalse(iniciado);
            Assert.IsFalse(grapple.Activo);
        }

        [Test]
        public void TryIniciar_falla_si_esta_en_cooldown()
        {
            var grapple = CrearGrapple(cooldown: 3f);
            Vector3 origen = Vector3.zero;
            Vector3 punto = new Vector3(0f, 0f, 10f);

            Assert.IsTrue(grapple.TryIniciar(origen, punto));
            grapple.Cancelar();

            // Intento inmediato dentro del cooldown
            Assert.IsFalse(grapple.TryIniciar(origen, punto));

            // Avanzar el cooldown hasta completarlo
            var estado = new LocomotionState();
            grapple.Tick(estado, origen, 3.1f);

            Assert.IsTrue(grapple.TryIniciar(origen, punto));
        }

        [Test]
        public void Tick_orienta_la_velocidad_hacia_el_punto_y_le_da_modulo_PullSpeed()
        {
            var grapple = CrearGrapple(velocidadTraccion: 25f);
            Vector3 origen = Vector3.zero;
            Vector3 punto = new Vector3(10f, 20f, 0f);

            grapple.TryIniciar(origen, punto);
            var estado = new LocomotionState();
            grapple.Tick(estado, origen, 0.016f);

            Vector3 direccionEsperada = (punto - origen).normalized;

            Assert.AreEqual(LocomotionPhase.Grappling, estado.Phase);
            Assert.AreEqual(25f, estado.Velocity.magnitude, 0.001f);
            Assert.AreEqual(direccionEsperada.x, estado.Velocity.normalized.x, 0.001f);
            Assert.AreEqual(direccionEsperada.y, estado.Velocity.normalized.y, 0.001f);
            Assert.AreEqual(direccionEsperada.z, estado.Velocity.normalized.z, 0.001f);
        }

        [Test]
        public void Tick_termina_cuando_la_distancia_es_menor_o_igual_a_ArrivalDistance()
        {
            var grapple = CrearGrapple(distanciaLlegada: 1.5f);
            Vector3 punto = new Vector3(0f, 0f, 10f);

            grapple.TryIniciar(Vector3.zero, punto);
            var estado = new LocomotionState();

            // Posición a distancia 1.0f (menor a 1.5f)
            Vector3 posicionLlegada = new Vector3(0f, 0f, 9f);
            grapple.Tick(estado, posicionLlegada, 0.016f);

            Assert.IsFalse(grapple.Activo);
            Assert.AreEqual(LocomotionPhase.Airborne, estado.Phase);
        }

        [Test]
        public void Tick_termina_si_se_supera_MaxDuration()
        {
            var grapple = CrearGrapple(duracionMax: 2f);
            Vector3 punto = new Vector3(0f, 0f, 25f);

            grapple.TryIniciar(Vector3.zero, punto);
            var estado = new LocomotionState();

            grapple.Tick(estado, Vector3.zero, 1.5f);
            Assert.IsTrue(grapple.Activo);

            grapple.Tick(estado, Vector3.zero, 0.6f);
            Assert.IsFalse(grapple.Activo);
            Assert.AreEqual(LocomotionPhase.Airborne, estado.Phase);
        }

        [Test]
        public void Cancelar_desactiva_el_gancho()
        {
            var grapple = CrearGrapple();
            grapple.TryIniciar(Vector3.zero, new Vector3(0f, 0f, 10f));

            Assert.IsTrue(grapple.Activo);
            grapple.Cancelar();
            Assert.IsFalse(grapple.Activo);
        }

        [Test]
        public void Constructor_desde_GrappleProfile_copia_valores_correctamente()
        {
            var perfil = ScriptableObject.CreateInstance<GrappleProfile>();
            perfil.Range = 30f;
            perfil.PullSpeed = 25f;
            perfil.Cooldown = 3f;
            perfil.ArrivalDistance = 1.5f;
            perfil.MaxDuration = 2f;

            var grapple = new GrappleResolver(perfil);
            Assert.IsTrue(grapple.TryIniciar(Vector3.zero, new Vector3(0f, 0f, 29f)));
            Assert.AreEqual(new Vector3(0f, 0f, 29f), grapple.Punto);
        }
    }
}
