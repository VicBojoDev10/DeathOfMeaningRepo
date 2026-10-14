using NUnit.Framework;
using TDOM.Gameplay.Camera;
using UnityEngine;

namespace TDOM.Tests.EditMode
{
    public class CameraStanceResolverTests
    {
        private const float Dt = 1f / 60f;
        private const float Tolerancia = 0.001f;
        private const int FramesParaEstabilizar = 600;
        private const float PermanenciaCombate = 3f;

        private static readonly StanceFraming Exploracion = new StanceFraming(4f, 0.8f, 0.6f, 40f);
        private static readonly StanceFraming Sprint = new StanceFraming(5f, 0.8f, 0.5f, 50f);
        private static readonly StanceFraming Combate = new StanceFraming(3.3f, 0.8f, 0.6f, 37f);
        private static readonly StanceFraming Apuntado = new StanceFraming(2f, 0.5f, 0.6f, 27f);

        private static CameraStanceResolver CrearResolver()
        {
            return new CameraStanceResolver(
                new CameraStanceSettings(
                    Exploracion,
                    Sprint,
                    Combate,
                    Apuntado,
                    transitionSpeed: 10f,
                    combatLingerSeconds: PermanenciaCombate,
                    shoulderSwapSpeed: 8f,
                    strafeSwingThreshold: 0.5f
                )
            );
        }

        private static void Simular(
            CameraStanceResolver resolver,
            int frames,
            bool apuntando = false,
            bool atacando = false,
            bool corriendo = false
        )
        {
            for (int i = 0; i < frames; i++)
                resolver.Tick(apuntando, atacando, corriendo, Dt);
        }

        private static void AssertEncuadre(StanceFraming esperado, StanceFraming actual)
        {
            Assert.AreEqual(esperado.Distance, actual.Distance, Tolerancia);
            Assert.AreEqual(esperado.ShoulderOffset, actual.ShoulderOffset, Tolerancia);
            Assert.AreEqual(esperado.PivotHeight, actual.PivotHeight, Tolerancia);
            Assert.AreEqual(esperado.FieldOfView, actual.FieldOfView, Tolerancia);
        }

        private static void EntrarEnCombate(CameraStanceResolver resolver)
        {
            resolver.Tick(apuntando: false, atacando: true, corriendo: false, Dt);
        }

        [Test]
        public void arranca_en_exploracion_con_su_encuadre()
        {
            var resolver = CrearResolver();

            Assert.AreEqual(CameraStance.Exploration, resolver.Stance);
            AssertEncuadre(Exploracion, resolver.Framing);
            Assert.AreEqual(CameraStanceResolver.LadoDerecho, resolver.CameraSide);
        }

        [Test]
        public void correr_lleva_la_camara_al_encuadre_de_sprint()
        {
            var resolver = CrearResolver();

            Simular(resolver, FramesParaEstabilizar, corriendo: true);

            Assert.AreEqual(CameraStance.Sprint, resolver.Stance);
            AssertEncuadre(Sprint, resolver.Framing);
        }

        [Test]
        public void atacar_tiene_prioridad_sobre_correr()
        {
            var resolver = CrearResolver();

            Simular(resolver, FramesParaEstabilizar, atacando: true, corriendo: true);

            Assert.AreEqual(CameraStance.Combat, resolver.Stance);
            AssertEncuadre(Combate, resolver.Framing);
        }

        [Test]
        public void apuntar_tiene_prioridad_sobre_todo()
        {
            var resolver = CrearResolver();

            Simular(
                resolver,
                FramesParaEstabilizar,
                apuntando: true,
                atacando: true,
                corriendo: true
            );

            Assert.AreEqual(CameraStance.Aim, resolver.Stance);
            AssertEncuadre(Apuntado, resolver.Framing);
        }

        [Test]
        public void el_combate_se_mantiene_durante_la_permanencia_tras_el_ultimo_ataque()
        {
            var resolver = CrearResolver();
            EntrarEnCombate(resolver);

            Simular(resolver, Mathf.FloorToInt((PermanenciaCombate - 0.1f) / Dt));

            Assert.AreEqual(CameraStance.Combat, resolver.Stance);
        }

        [Test]
        public void el_combate_termina_al_cumplirse_la_permanencia()
        {
            var resolver = CrearResolver();
            EntrarEnCombate(resolver);

            Simular(resolver, Mathf.CeilToInt((PermanenciaCombate + 0.1f) / Dt));

            Assert.AreEqual(CameraStance.Exploration, resolver.Stance);
        }

        [Test]
        public void la_transicion_entre_encuadres_es_gradual()
        {
            var resolver = CrearResolver();

            resolver.Tick(apuntando: true, atacando: false, corriendo: false, Dt);

            Assert.Less(resolver.Framing.Distance, Exploracion.Distance);
            Assert.Greater(resolver.Framing.Distance, Apuntado.Distance);
            Assert.Less(resolver.Framing.FieldOfView, Exploracion.FieldOfView);
            Assert.Greater(resolver.Framing.FieldOfView, Apuntado.FieldOfView);
        }

        [Test]
        public void dash_hacia_la_izquierda_en_combate_cambia_al_hombro_izquierdo()
        {
            var resolver = CrearResolver();
            EntrarEnCombate(resolver);

            resolver.RegistrarDesplazamientoLateral(Vector2.left);
            Simular(resolver, 60, atacando: true);

            Assert.AreEqual(CameraStanceResolver.LadoIzquierdo, resolver.CameraSide, Tolerancia);
        }

        [Test]
        public void dash_fuera_de_combate_no_cambia_de_hombro()
        {
            var resolver = CrearResolver();

            resolver.RegistrarDesplazamientoLateral(Vector2.left);
            Simular(resolver, 60);

            Assert.AreEqual(CameraStance.Exploration, resolver.Stance);
            Assert.AreEqual(CameraStanceResolver.LadoDerecho, resolver.CameraSide, Tolerancia);
        }

        [Test]
        public void dash_casi_de_frente_no_cambia_de_hombro()
        {
            var resolver = CrearResolver();
            EntrarEnCombate(resolver);

            resolver.RegistrarDesplazamientoLateral(new Vector2(-0.2f, 1f));
            Simular(resolver, 60, atacando: true);

            Assert.AreEqual(CameraStanceResolver.LadoDerecho, resolver.CameraSide, Tolerancia);
        }

        [Test]
        public void el_cambio_de_hombro_es_gradual()
        {
            var resolver = CrearResolver();
            EntrarEnCombate(resolver);

            resolver.RegistrarDesplazamientoLateral(Vector2.left);
            resolver.Tick(apuntando: false, atacando: true, corriendo: false, Dt);

            Assert.Less(resolver.CameraSide, CameraStanceResolver.LadoDerecho);
            Assert.Greater(resolver.CameraSide, CameraStanceResolver.LadoIzquierdo);
        }

        [Test]
        public void al_salir_de_combate_regresa_al_hombro_derecho()
        {
            var resolver = CrearResolver();
            EntrarEnCombate(resolver);
            resolver.RegistrarDesplazamientoLateral(Vector2.left);
            Simular(resolver, 60, atacando: true);

            Simular(resolver, FramesParaEstabilizar);

            Assert.AreEqual(CameraStance.Exploration, resolver.Stance);
            Assert.AreEqual(CameraStanceResolver.LadoDerecho, resolver.CameraSide, Tolerancia);
        }

        [Test]
        public void el_resolver_es_determinista_con_dt_fijo()
        {
            var primero = CrearResolver();
            var segundo = CrearResolver();

            for (int i = 0; i < 300; i++)
            {
                bool apuntando = i % 90 > 70;
                bool atacando = i % 120 < 10;
                bool corriendo = i % 50 < 30;
                if (i % 40 == 5)
                {
                    primero.RegistrarDesplazamientoLateral(Vector2.left);
                    segundo.RegistrarDesplazamientoLateral(Vector2.left);
                }
                primero.Tick(apuntando, atacando, corriendo, Dt);
                segundo.Tick(apuntando, atacando, corriendo, Dt);
            }

            Assert.AreEqual(primero.Stance, segundo.Stance);
            Assert.AreEqual(primero.Framing.Distance, segundo.Framing.Distance);
            Assert.AreEqual(primero.Framing.ShoulderOffset, segundo.Framing.ShoulderOffset);
            Assert.AreEqual(primero.Framing.PivotHeight, segundo.Framing.PivotHeight);
            Assert.AreEqual(primero.Framing.FieldOfView, segundo.Framing.FieldOfView);
            Assert.AreEqual(primero.CameraSide, segundo.CameraSide);
        }
    }
}
