using NUnit.Framework;
using TDOM.Gameplay.Vfx;

namespace TDOM.Tests.EditMode
{
    public class VientoGanchoTests
    {
        private const float Delta = 0.001f;

        [Test]
        public void empieza_apagado()
        {
            var viento = new VientoGancho(25f, 0.2f, 0.4f);

            Assert.AreEqual(0f, viento.Intensidad, Delta);
            Assert.IsFalse(viento.Visible);
        }

        [Test]
        public void con_el_jalon_activo_sube_poco_a_poco()
        {
            var viento = new VientoGancho(25f, 0.2f, 0.4f);

            viento.Actualizar(true, 25f, 0.1f);

            Assert.AreEqual(0.5f, viento.Intensidad, Delta);
        }

        [Test]
        public void a_velocidad_de_referencia_llega_a_uno_y_no_pasa()
        {
            var viento = new VientoGancho(25f, 0.2f, 0.4f);

            viento.Actualizar(true, 25f, 1f);

            Assert.AreEqual(1f, viento.Intensidad, Delta);
        }

        [Test]
        public void a_mas_velocidad_que_la_referencia_se_queda_en_uno()
        {
            var viento = new VientoGancho(25f, 0.2f, 0.4f);

            viento.Actualizar(true, 80f, 1f);

            Assert.AreEqual(1f, viento.Intensidad, Delta);
        }

        [Test]
        public void a_media_velocidad_el_objetivo_es_la_mitad()
        {
            var viento = new VientoGancho(25f, 0.2f, 0.4f);

            viento.Actualizar(true, 12.5f, 1f);

            Assert.AreEqual(0.5f, viento.Intensidad, Delta);
        }

        [Test]
        public void al_soltar_baja_gradualmente_y_no_se_corta()
        {
            var viento = new VientoGancho(25f, 0.2f, 0.4f);
            viento.Actualizar(true, 25f, 1f);

            viento.Actualizar(false, 25f, 0.2f);

            Assert.AreEqual(0.5f, viento.Intensidad, Delta);
            Assert.IsTrue(viento.Visible);
        }

        [Test]
        public void tras_el_tiempo_de_bajada_queda_apagado()
        {
            var viento = new VientoGancho(25f, 0.2f, 0.4f);
            viento.Actualizar(true, 25f, 1f);

            viento.Actualizar(false, 25f, 0.4f);

            Assert.AreEqual(0f, viento.Intensidad, Delta);
            Assert.IsFalse(viento.Visible);
        }

        [Test]
        public void si_baja_la_velocidad_con_el_jalon_activo_la_intensidad_baja()
        {
            var viento = new VientoGancho(25f, 0.2f, 0.4f);
            viento.Actualizar(true, 25f, 1f);

            viento.Actualizar(true, 12.5f, 1f);

            Assert.AreEqual(0.5f, viento.Intensidad, Delta);
        }

        [Test]
        public void sin_tiempos_de_rampa_cambia_de_golpe()
        {
            var viento = new VientoGancho(25f, 0f, 0f);

            viento.Actualizar(true, 25f, 0.016f);
            Assert.AreEqual(1f, viento.Intensidad, Delta);

            viento.Actualizar(false, 25f, 0.016f);
            Assert.AreEqual(0f, viento.Intensidad, Delta);
        }

        [Test]
        public void sin_velocidad_de_referencia_el_jalon_activo_vale_uno()
        {
            var viento = new VientoGancho(0f, 0f, 0f);

            viento.Actualizar(true, 0f, 0.016f);

            Assert.AreEqual(1f, viento.Intensidad, Delta);
        }

        [Test]
        public void un_dt_negativo_no_cambia_la_intensidad()
        {
            var viento = new VientoGancho(25f, 0.2f, 0.4f);
            viento.Actualizar(true, 25f, 0.1f);

            viento.Actualizar(true, 25f, -1f);

            Assert.AreEqual(0.5f, viento.Intensidad, Delta);
        }

        [Test]
        public void reiniciar_apaga_de_golpe()
        {
            var viento = new VientoGancho(25f, 0.2f, 0.4f);
            viento.Actualizar(true, 25f, 1f);

            viento.Reiniciar();

            Assert.AreEqual(0f, viento.Intensidad, Delta);
        }
    }
}
