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
            return new GrappleResolver(
                alcance,
                velocidadTraccion,
                cooldown,
                distanciaLlegada,
                duracionMax
            );
        }

        [Test]
        public void TryIniciar_falla_si_la_distancia_supera_el_alcance()
        {
            var grapple = CrearGrapple(alcance: 30f, cooldown: 3f);
            Vector3 origen = Vector3.zero;
            Vector3 punto = new Vector3(0f, 0f, 31f);

            bool iniciado = grapple.TryIniciar(origen, punto);

            Assert.IsFalse(iniciado);
            Assert.IsFalse(grapple.Activo);

            // Fuera de alcance no gasta cooldown
            Vector3 puntoValido = new Vector3(0f, 0f, 10f);
            Assert.IsTrue(grapple.TryIniciar(origen, puntoValido));
        }

        [Test]
        public void TryIniciar_falla_si_esta_en_cooldown()
        {
            var grapple = CrearGrapple(cooldown: 3f);
            Vector3 origen = Vector3.zero;
            Vector3 punto = new Vector3(0f, 0f, 10f);

            Assert.IsTrue(grapple.TryIniciar(origen, punto));
            grapple.Cancelar();

            Assert.IsFalse(grapple.TryIniciar(origen, punto));

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

            Vector3 posicionLlegada = new Vector3(0f, 0f, 9f);
            grapple.Tick(estado, posicionLlegada, 0.016f);

            Assert.IsFalse(grapple.Activo);
            Assert.AreEqual(LocomotionPhase.Airborne, estado.Phase);
        }

        [Test]
        public void Traccionar_y_llegar_en_tiempo_aproximado_a_distancia_entre_velocidad_mas_menos_un_frame()
        {
            float alcance = 30f;
            float velocidadTraccion = 25f;
            float distanciaLlegada = 1.5f;
            var grapple = CrearGrapple(
                alcance: alcance,
                velocidadTraccion: velocidadTraccion,
                distanciaLlegada: distanciaLlegada
            );

            Vector3 origen = Vector3.zero;
            Vector3 punto = new Vector3(0f, 0f, 21.5f);
            float distanciaARecorrer = Vector3.Distance(origen, punto) - distanciaLlegada;
            float tiempoTeorico = distanciaARecorrer / velocidadTraccion; // 20 / 25 = 0.8s

            grapple.TryIniciar(origen, punto);
            var estado = new LocomotionState();
            Vector3 posicion = origen;
            float dt = 1f / 60f;
            float tiempoTranscurrido = 0f;

            while (grapple.Activo)
            {
                grapple.Tick(estado, posicion, dt);
                if (grapple.Activo)
                {
                    posicion += estado.Velocity * dt;
                    tiempoTranscurrido += dt;
                }
            }

            Assert.IsFalse(grapple.Activo);
            Assert.AreEqual(LocomotionPhase.Airborne, estado.Phase);
            Assert.LessOrEqual(Vector3.Distance(posicion, punto), distanciaLlegada);
            Assert.AreEqual(tiempoTeorico, tiempoTranscurrido, dt);
        }

        [Test]
        public void Llegada_es_independiente_del_framerate_entre_60fps_y_30fps()
        {
            float alcance = 30f;
            float velocidadTraccion = 25f;
            float distanciaLlegada = 1.5f;
            Vector3 origen = Vector3.zero;
            Vector3 punto = new Vector3(0f, 0f, 20.5f);

            // Simulación a 60 fps (dt = 1/60)
            var grapple60 = CrearGrapple(
                alcance: alcance,
                velocidadTraccion: velocidadTraccion,
                distanciaLlegada: distanciaLlegada
            );
            grapple60.TryIniciar(origen, punto);
            var estado60 = new LocomotionState();
            Vector3 pos60 = origen;
            float dt60 = 1f / 60f;

            while (grapple60.Activo)
            {
                grapple60.Tick(estado60, pos60, dt60);
                if (grapple60.Activo)
                    pos60 += estado60.Velocity * dt60;
            }

            // Simulación a 30 fps (dt = 1/30)
            var grapple30 = CrearGrapple(
                alcance: alcance,
                velocidadTraccion: velocidadTraccion,
                distanciaLlegada: distanciaLlegada
            );
            grapple30.TryIniciar(origen, punto);
            var estado30 = new LocomotionState();
            Vector3 pos30 = origen;
            float dt30 = 1f / 30f;

            while (grapple30.Activo)
            {
                grapple30.Tick(estado30, pos30, dt30);
                if (grapple30.Activo)
                    pos30 += estado30.Velocity * dt30;
            }

            Assert.IsFalse(grapple60.Activo);
            Assert.IsFalse(grapple30.Activo);
            Assert.LessOrEqual(Vector3.Distance(pos60, punto), distanciaLlegada);
            Assert.LessOrEqual(Vector3.Distance(pos30, punto), distanciaLlegada);

            // Independiente del framerate: con dt=1/60 y dt=1/30 llegar al mismo punto (±0.05 m)
            float diferenciaPosiciones = Vector3.Distance(pos60, pos30);
            Assert.AreEqual(0f, diferenciaPosiciones, 0.05f);
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
