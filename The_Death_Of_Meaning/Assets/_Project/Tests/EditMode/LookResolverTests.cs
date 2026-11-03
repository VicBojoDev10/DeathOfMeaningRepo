using NUnit.Framework;
using TDOM.Gameplay.Camera;
using UnityEngine;

namespace TDOM.Tests.EditMode
{
    public class LookResolverTests
    {
        [Test]
        public void Pitch_NuncaSuperaElClamp()
        {
            var resolver = new LookResolver(sensibilidad: 100f, pitchMin: -85f, pitchMax: 85f);

            // Empujamos el pitch muy fuerte hacia arriba durante muchos frames
            for (int i = 0; i < 100; i++)
                resolver.Tick(new Vector2(0f, 10f), dt: 1f);

            Assert.LessOrEqual(resolver.Pitch, 85f);
            Assert.GreaterOrEqual(resolver.Pitch, -85f);
        }

        [Test]
        public void Yaw_EnvuelveCorrectamente()
        {
            var resolver = new LookResolver(sensibilidad: 1f);

            // Empujamos el yaw bastante más allá de 360°
            resolver.Tick(new Vector2(500f, 0f), dt: 1f);

            Assert.GreaterOrEqual(resolver.Yaw, 0f);
            Assert.Less(resolver.Yaw, 360f);
        }

        [Test]
        public void Sensibilidad_Escala()
        {
            var lento = new LookResolver(sensibilidad: 1f);
            var rapido = new LookResolver(sensibilidad: 5f);

            lento.Tick(new Vector2(1f, 0f), dt: 1f);
            rapido.Tick(new Vector2(1f, 0f), dt: 1f);

            Assert.Greater(rapido.Yaw, lento.Yaw);
        }

        [Test]
        public void sensibilidades_x_e_y_afectan_yaw_y_pitch_por_separado()
        {
            var resolver = LookResolver.CrearConSensibilidades(
                sensibilidadX: 2f,
                sensibilidadY: 5f
            );

            resolver.Tick(new Vector2(1f, 1f), dt: 0.5f);

            Assert.AreEqual(1f, resolver.Yaw, 0.001f);
            Assert.AreEqual(-2.5f, resolver.Pitch, 0.001f);
        }

        [Test]
        public void cambiar_sensibilidad_en_caliente_afecta_solo_los_ticks_siguientes()
        {
            var resolver = LookResolver.CrearConSensibilidades(
                sensibilidadX: 1f,
                sensibilidadY: 1f
            );

            resolver.Tick(new Vector2(1f, 1f), dt: 1f);
            resolver.EstablecerSensibilidad(3f, 4f);
            resolver.Tick(new Vector2(1f, 1f), dt: 1f);

            Assert.AreEqual(4f, resolver.Yaw, 0.001f);
            Assert.AreEqual(-5f, resolver.Pitch, 0.001f);
        }

        [Test]
        public void sensibilidades_independientes_son_deterministas_con_dt_fijo()
        {
            var primero = LookResolver.CrearConSensibilidades(
                sensibilidadX: 137f,
                sensibilidadY: 83f
            );
            var segundo = LookResolver.CrearConSensibilidades(
                sensibilidadX: 137f,
                sensibilidadY: 83f
            );
            Vector2 look = new Vector2(0.7f, -0.4f);

            for (int frame = 0; frame < 60; frame++)
            {
                primero.Tick(look, 1f / 60f);
                segundo.Tick(look, 1f / 60f);
            }

            Assert.AreEqual(primero.Yaw, segundo.Yaw, 0.001f);
            Assert.AreEqual(primero.Pitch, segundo.Pitch, 0.001f);
        }

        [Test]
        public void establecer_orientacion_envuelve_el_yaw_y_aplica_el_pitch_dado()
        {
            var resolver = new LookResolver(sensibilidad: 1f, pitchMin: -40f, pitchMax: 70f);

            resolver.EstablecerOrientacion(-30f, -55f);

            Assert.AreEqual(330f, resolver.Yaw, 0.001f);
            Assert.AreEqual(-55f, resolver.Pitch, 0.001f);
        }

        [Test]
        public void arranca_con_yaw_inicial_correcto()
        {
            var resolver = new LookResolver(sensibilidad: 1f, yawInicial: 90f);
            Vector3 result = resolver.YawRotation * Vector3.forward;
            Assert.AreEqual(Vector3.right.x, result.x, 0.001f);
            Assert.AreEqual(Vector3.right.y, result.y, 0.001f);
            Assert.AreEqual(Vector3.right.z, result.z, 0.001f);
        }
    }
}
