using NUnit.Framework;
using TDOM.Gameplay.Vfx;
using UnityEngine;

namespace TDOM.Tests.EditMode
{
    public class DireccionEmbestidaTests
    {
        private const float Dt = 1f / 60f;
        private const float VelocidadMinima = 1f;
        private const float Suavizado = 12f;
        private const float Tolerancia = 0.001f;

        private static Vector3 Paso(
            Vector3 velocidad,
            Vector3 direccion,
            float suavizado = Suavizado
        ) => DireccionEmbestida.Calcular(velocidad * Dt, direccion, VelocidadMinima, suavizado, Dt);

        [Test]
        public void sin_moverse_conserva_la_direccion()
        {
            Vector3 resultado = Paso(Vector3.zero, Vector3.forward);

            Assert.AreEqual(0f, Vector3.Distance(Vector3.forward, resultado), Tolerancia);
        }

        [Test]
        public void bajo_la_velocidad_minima_conserva_la_direccion()
        {
            Vector3 resultado = Paso(Vector3.right * 0.5f, Vector3.forward);

            Assert.AreEqual(0f, Vector3.Distance(Vector3.forward, resultado), Tolerancia);
        }

        [Test]
        public void sin_suavizado_toma_la_direccion_del_movimiento()
        {
            Vector3 resultado = Paso(Vector3.right * 12f, Vector3.forward, 0f);

            Assert.AreEqual(0f, Vector3.Distance(Vector3.right, resultado), Tolerancia);
        }

        [Test]
        public void con_suavizado_gira_de_forma_gradual_hacia_el_movimiento()
        {
            Vector3 resultado = Paso(Vector3.right * 12f, Vector3.forward);

            Assert.Greater(resultado.x, 0f);
            Assert.Greater(resultado.z, resultado.x);
        }

        [Test]
        public void ignora_el_movimiento_vertical()
        {
            Vector3 resultado = Paso(new Vector3(0f, 20f, 12f), Vector3.forward, 0f);

            Assert.AreEqual(0f, resultado.y, Tolerancia);
        }

        [Test]
        public void con_dt_fijo_termina_alineada_con_el_movimiento()
        {
            Vector3 direccion = Vector3.forward;
            for (int cuadro = 0; cuadro < 60; cuadro++)
                direccion = Paso(Vector3.right * 12f, direccion);

            Assert.AreEqual(0f, Vector3.Distance(Vector3.right, direccion), 0.01f);
        }

        [Test]
        public void dt_cero_conserva_la_direccion()
        {
            Vector3 resultado = DireccionEmbestida.Calcular(
                Vector3.right,
                Vector3.forward,
                VelocidadMinima,
                Suavizado,
                0f
            );

            Assert.AreEqual(0f, Vector3.Distance(Vector3.forward, resultado), Tolerancia);
        }
    }
}
