using NUnit.Framework;
using TDOM.Gameplay.Camera;
using UnityEngine;

namespace TDOM.Tests.EditMode
{
    public class CameraAutoRotationResolverTests
    {
        private const float Dt60 = 1f / 60f;
        private const float Dt30 = 1f / 30f;
        private const float Corriendo = 8f;
        private const float GraceDelay = 0.3f;
        private const float LookCooldown = 1f;

        private static CameraAutoRotationResolver CrearResolver()
        {
            return new CameraAutoRotationResolver(
                new CameraAutoRotationSettings(
                    minSpeed: 1f,
                    lookCooldown: LookCooldown,
                    lookDeadzone: 0.1f,
                    graceDelay: GraceDelay,
                    rotationSpeed: 90f,
                    easeAngle: 45f,
                    minAngle: 5f,
                    maxAngle: 135f
                )
            );
        }

        private static float Simular(
            CameraAutoRotationResolver resolver,
            float yawCamara,
            float yawPersonaje,
            float segundos,
            float dt,
            float velocidad = Corriendo,
            bool apuntando = false
        )
        {
            int frames = Mathf.RoundToInt(segundos / dt);
            for (int i = 0; i < frames; i++)
                yawCamara += resolver.Tick(
                    yawCamara,
                    yawPersonaje,
                    velocidad,
                    Vector2.zero,
                    apuntando,
                    dt
                );
            return yawCamara;
        }

        [Test]
        public void corriendo_de_lado_la_camara_vuelve_detras_del_personaje()
        {
            var resolver = CrearResolver();

            float yawCamara = Simular(resolver, 0f, 90f, 6f, Dt60);

            Assert.Less(Mathf.Abs(Mathf.DeltaAngle(yawCamara, 90f)), 5.5f);
            Assert.Greater(yawCamara, 80f);
        }

        [Test]
        public void no_reacciona_antes_del_tiempo_de_gracia()
        {
            var resolver = CrearResolver();

            float yawCamara = Simular(resolver, 0f, 90f, GraceDelay - 0.05f, Dt60);

            Assert.AreEqual(0f, yawCamara);
        }

        [Test]
        public void reacciona_al_cumplirse_el_tiempo_de_gracia()
        {
            var resolver = CrearResolver();

            float yawCamara = Simular(resolver, 0f, 90f, GraceDelay + 0.1f, Dt60);

            Assert.Greater(yawCamara, 0f);
        }

        [Test]
        public void no_actua_durante_la_pausa_tras_mover_el_stick_derecho()
        {
            var resolver = CrearResolver();
            resolver.Tick(0f, 90f, Corriendo, Vector2.right, false, Dt60);

            float yawCamara = Simular(resolver, 0f, 90f, LookCooldown - 0.1f, Dt60);

            Assert.AreEqual(0f, yawCamara);
        }

        [Test]
        public void no_actua_cuando_el_personaje_camina_hacia_la_camara()
        {
            var resolver = CrearResolver();

            float yawCamara = Simular(resolver, 0f, 180f, 3f, Dt60);

            Assert.AreEqual(0f, yawCamara);
        }

        [Test]
        public void no_actua_con_el_personaje_quieto()
        {
            var resolver = CrearResolver();

            float yawCamara = Simular(resolver, 0f, 90f, 3f, Dt60, velocidad: 0f);

            Assert.AreEqual(0f, yawCamara);
        }

        [Test]
        public void no_actua_mientras_se_apunta()
        {
            var resolver = CrearResolver();

            float yawCamara = Simular(resolver, 0f, 90f, 3f, Dt60, apuntando: true);

            Assert.AreEqual(0f, yawCamara);
        }

        [Test]
        public void gira_hacia_el_lado_mas_corto()
        {
            var resolver = CrearResolver();

            float yawCamara = Simular(resolver, 0f, -90f, 2f, Dt60);

            Assert.Less(yawCamara, 0f);
        }

        [Test]
        public void la_autorrotacion_es_independiente_del_framerate()
        {
            float a60 = Simular(CrearResolver(), 0f, 90f, 2f, Dt60);
            float a30 = Simular(CrearResolver(), 0f, 90f, 2f, Dt30);

            Assert.AreEqual(a60, a30, 1.5f);
        }

        [Test]
        public void la_autorrotacion_es_determinista_con_dt_fijo()
        {
            float primero = Simular(CrearResolver(), 10f, 100f, 2f, Dt60);
            float segundo = Simular(CrearResolver(), 10f, 100f, 2f, Dt60);

            Assert.AreEqual(primero, segundo);
        }
    }
}
