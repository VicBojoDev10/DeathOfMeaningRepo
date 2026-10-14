using NUnit.Framework;
using TDOM.Gameplay.Locomotion;
using UnityEngine;

namespace TDOM.Tests.EditMode
{
    public class FacingResolverTests
    {
        private const float Dt60 = 1f / 60f;
        private const float Dt30 = 1f / 30f;
        private const float VelocidadGiro = 720f;
        private const float Tolerancia = 0.01f;

        private static void Simular(
            FacingResolver resolver,
            Vector3 velocidad,
            float segundos,
            float dt,
            float yawCamara = 0f,
            bool apuntando = false
        )
        {
            int frames = Mathf.RoundToInt(segundos / dt);
            for (int i = 0; i < frames; i++)
                resolver.Tick(velocidad, yawCamara, apuntando, dt);
        }

        private static float DiferenciaAngular(float a, float b)
        {
            return Mathf.Abs(Mathf.DeltaAngle(a, b));
        }

        [Test]
        public void con_velocidad_hacia_x_positivo_el_yaw_converge_a_90()
        {
            var resolver = new FacingResolver(VelocidadGiro);

            Simular(resolver, Vector3.right * 5f, 1f, Dt60);

            Assert.Less(DiferenciaAngular(resolver.YawActual, 90f), Tolerancia);
        }

        [Test]
        public void al_invertir_de_z_positivo_a_negativo_gira_180_sin_saltos()
        {
            var resolver = new FacingResolver(VelocidadGiro);
            float giroMaximoPorFrame = VelocidadGiro * Dt60;
            float yawPrevio = resolver.YawActual;

            for (int i = 0; i < 60; i++)
            {
                resolver.Tick(Vector3.back * 5f, 0f, false, Dt60);
                Assert.LessOrEqual(
                    DiferenciaAngular(resolver.YawActual, yawPrevio),
                    giroMaximoPorFrame + Tolerancia
                );
                yawPrevio = resolver.YawActual;
            }

            Assert.Less(DiferenciaAngular(resolver.YawActual, 180f), Tolerancia);
        }

        [Test]
        public void el_giro_de_180_tarda_lo_que_marca_la_velocidad_configurada()
        {
            var resolver = new FacingResolver(VelocidadGiro);

            Simular(resolver, Vector3.back * 5f, 0.1f, Dt60);

            Assert.Less(DiferenciaAngular(resolver.YawActual, VelocidadGiro * 0.1f), Tolerancia);
        }

        [Test]
        public void quieto_no_cambia_de_yaw()
        {
            var resolver = new FacingResolver(VelocidadGiro, yawInicial: 45f);

            Simular(resolver, Vector3.zero, 1f, Dt60, yawCamara: 200f);

            Assert.AreEqual(45f, resolver.YawActual, Tolerancia);
        }

        [Test]
        public void una_velocidad_minima_no_hace_girar()
        {
            var resolver = new FacingResolver(VelocidadGiro, yawInicial: 45f);

            Simular(resolver, Vector3.right * 0.05f, 1f, Dt60);

            Assert.AreEqual(45f, resolver.YawActual, Tolerancia);
        }

        [Test]
        public void apuntando_sigue_el_yaw_de_camara_aunque_se_mueva_de_lado()
        {
            var resolver = new FacingResolver(VelocidadGiro);

            Simular(resolver, Vector3.right * 5f, 1f, Dt60, yawCamara: 30f, apuntando: true);

            Assert.Less(DiferenciaAngular(resolver.YawActual, 30f), Tolerancia);
        }

        [Test]
        public void alinear_con_un_yaw_lo_aplica_de_inmediato()
        {
            var resolver = new FacingResolver(VelocidadGiro);

            resolver.AlinearCon(135f);

            Assert.AreEqual(135f, resolver.YawActual, Tolerancia);
        }

        [Test]
        public void el_giro_es_independiente_del_framerate()
        {
            var a60 = new FacingResolver(VelocidadGiro);
            var a30 = new FacingResolver(VelocidadGiro);

            Simular(a60, Vector3.back * 5f, 0.1f, Dt60);
            Simular(a30, Vector3.back * 5f, 0.1f, Dt30);

            Assert.Less(DiferenciaAngular(a60.YawActual, a30.YawActual), Tolerancia);
        }

        [Test]
        public void el_giro_es_determinista_con_dt_fijo()
        {
            var primero = new FacingResolver(VelocidadGiro);
            var segundo = new FacingResolver(VelocidadGiro);

            for (int i = 0; i < 240; i++)
            {
                Vector3 velocidad = Quaternion.Euler(0f, i * 7f, 0f) * Vector3.forward * 4f;
                bool apuntando = i % 50 > 40;
                primero.Tick(velocidad, 15f, apuntando, Dt60);
                segundo.Tick(velocidad, 15f, apuntando, Dt60);
            }

            Assert.AreEqual(primero.YawActual, segundo.YawActual);
        }
    }
}
