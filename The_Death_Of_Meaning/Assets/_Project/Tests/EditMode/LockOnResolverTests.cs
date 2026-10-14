using System.Collections.Generic;
using NUnit.Framework;
using TDOM.Gameplay.Camera;
using UnityEngine;

namespace TDOM.Tests.EditMode
{
    public class LockOnResolverTests
    {
        private const float Dt60 = 1f / 60f;
        private const float Dt30 = 1f / 30f;
        private const float Alcance = 60f;

        private static readonly Vector3 Origen = Vector3.zero;
        private static readonly Vector3 Frente = Vector3.forward;

        private static LockOnResolver CrearResolver()
        {
            return new LockOnResolver(
                new LockOnSettings(range: Alcance, smoothTime: 0.15f, pitchMin: -60f, pitchMax: 60f)
            );
        }

        private static List<LockOnCandidate> Candidatos(params LockOnCandidate[] candidatos)
        {
            return new List<LockOnCandidate>(candidatos);
        }

        private static void Pulsar(
            LockOnResolver resolver,
            List<LockOnCandidate> candidatos,
            float yawCamara = 0f,
            float pitchCamara = 0f
        )
        {
            resolver.Tick(true, Origen, Frente, yawCamara, pitchCamara, candidatos, Dt60);
        }

        private static void Simular(
            LockOnResolver resolver,
            List<LockOnCandidate> candidatos,
            float segundos,
            float dt
        )
        {
            int frames = Mathf.RoundToInt(segundos / dt);
            for (int i = 0; i < frames; i++)
                resolver.Tick(false, Origen, Frente, 0f, 0f, candidatos, dt);
        }

        [Test]
        public void r3_fija_un_objetivo_dentro_del_alcance()
        {
            var resolver = CrearResolver();

            Pulsar(resolver, Candidatos(new LockOnCandidate(7, new Vector3(0f, 0f, 20f))));

            Assert.IsTrue(resolver.Activo);
            Assert.AreEqual(7, resolver.ObjetivoId);
        }

        [Test]
        public void r3_de_nuevo_libera_el_objetivo()
        {
            var resolver = CrearResolver();
            var candidatos = Candidatos(new LockOnCandidate(7, new Vector3(0f, 0f, 20f)));
            Pulsar(resolver, candidatos);

            Pulsar(resolver, candidatos);

            Assert.IsFalse(resolver.Activo);
        }

        [Test]
        public void sin_objetivos_en_alcance_no_fija()
        {
            var resolver = CrearResolver();

            Pulsar(resolver, Candidatos(new LockOnCandidate(7, new Vector3(0f, 0f, 80f))));

            Assert.IsFalse(resolver.Activo);
        }

        [Test]
        public void elige_el_objetivo_mas_cercano_al_centro_de_la_pantalla()
        {
            var resolver = CrearResolver();

            Pulsar(
                resolver,
                Candidatos(
                    new LockOnCandidate(1, new Vector3(10f, 0f, 10f)),
                    new LockOnCandidate(2, new Vector3(1f, 0f, 40f)),
                    new LockOnCandidate(3, new Vector3(-10f, 0f, 5f))
                )
            );

            Assert.AreEqual(2, resolver.ObjetivoId);
        }

        [Test]
        public void con_el_mismo_angulo_elige_el_mas_cercano()
        {
            var resolver = CrearResolver();

            Pulsar(
                resolver,
                Candidatos(
                    new LockOnCandidate(1, new Vector3(0f, 0f, 40f)),
                    new LockOnCandidate(2, new Vector3(0f, 0f, 15f))
                )
            );

            Assert.AreEqual(2, resolver.ObjetivoId);
        }

        [Test]
        public void ignora_los_objetivos_fuera_de_alcance_aunque_esten_al_centro()
        {
            var resolver = CrearResolver();

            Pulsar(
                resolver,
                Candidatos(
                    new LockOnCandidate(1, new Vector3(0f, 0f, 90f)),
                    new LockOnCandidate(2, new Vector3(20f, 0f, 20f))
                )
            );

            Assert.AreEqual(2, resolver.ObjetivoId);
        }

        [Test]
        public void libera_cuando_el_objetivo_sale_del_alcance()
        {
            var resolver = CrearResolver();
            Pulsar(resolver, Candidatos(new LockOnCandidate(7, new Vector3(0f, 0f, 20f))));

            Simular(
                resolver,
                Candidatos(new LockOnCandidate(7, new Vector3(0f, 0f, 70f))),
                Dt60,
                Dt60
            );

            Assert.IsFalse(resolver.Activo);
        }

        [Test]
        public void libera_cuando_el_objetivo_desaparece_aunque_queden_otros()
        {
            var resolver = CrearResolver();
            Pulsar(
                resolver,
                Candidatos(
                    new LockOnCandidate(1, new Vector3(0f, 0f, 20f)),
                    new LockOnCandidate(2, new Vector3(10f, 0f, 20f))
                )
            );

            Simular(
                resolver,
                Candidatos(new LockOnCandidate(2, new Vector3(10f, 0f, 20f))),
                Dt60,
                Dt60
            );

            Assert.IsFalse(resolver.Activo);
        }

        [Test]
        public void converge_hacia_el_objetivo_sin_saltos()
        {
            var resolver = CrearResolver();
            var candidatos = Candidatos(new LockOnCandidate(7, new Vector3(20f, 0f, 20f)));
            Pulsar(resolver, candidatos);
            float yawPrevio = resolver.Yaw;

            for (int i = 0; i < 120; i++)
            {
                resolver.Tick(false, Origen, Frente, 0f, 0f, candidatos, Dt60);
                Assert.Less(Mathf.Abs(Mathf.DeltaAngle(resolver.Yaw, yawPrevio)), 10f);
                yawPrevio = resolver.Yaw;
            }

            Assert.AreEqual(45f, resolver.Yaw, 0.5f);
        }

        [Test]
        public void el_pitch_mira_hacia_arriba_a_un_objetivo_alto_y_respeta_el_limite()
        {
            var resolver = CrearResolver();
            var candidatos = Candidatos(new LockOnCandidate(7, new Vector3(0f, 50f, 5f)));
            Pulsar(resolver, candidatos);

            Simular(resolver, candidatos, 2f, Dt60);

            Assert.AreEqual(-60f, resolver.Pitch, 0.5f);
        }

        [Test]
        public void yaw_hacia_el_objetivo_desde_otra_posicion()
        {
            var resolver = CrearResolver();
            Pulsar(resolver, Candidatos(new LockOnCandidate(7, new Vector3(10f, 0f, 0f))));

            Assert.AreEqual(90f, resolver.YawHacia(Vector3.zero), 0.01f);
        }

        [Test]
        public void el_seguimiento_es_independiente_del_framerate()
        {
            var candidatos = Candidatos(new LockOnCandidate(7, new Vector3(20f, 5f, 20f)));
            var a60 = CrearResolver();
            var a30 = CrearResolver();
            Pulsar(a60, candidatos);
            Pulsar(a30, candidatos);

            Simular(a60, candidatos, 0.3f, Dt60);
            Simular(a30, candidatos, 0.3f, Dt30);

            Assert.AreEqual(a60.Yaw, a30.Yaw, 1.5f);
            Assert.AreEqual(a60.Pitch, a30.Pitch, 1.5f);
        }

        [Test]
        public void el_seguimiento_es_determinista_con_dt_fijo()
        {
            var candidatos = Candidatos(new LockOnCandidate(7, new Vector3(-15f, 8f, 25f)));
            var primero = CrearResolver();
            var segundo = CrearResolver();
            Pulsar(primero, candidatos);
            Pulsar(segundo, candidatos);

            Simular(primero, candidatos, 1f, Dt60);
            Simular(segundo, candidatos, 1f, Dt60);

            Assert.AreEqual(primero.Yaw, segundo.Yaw);
            Assert.AreEqual(primero.Pitch, segundo.Pitch);
        }
    }
}
