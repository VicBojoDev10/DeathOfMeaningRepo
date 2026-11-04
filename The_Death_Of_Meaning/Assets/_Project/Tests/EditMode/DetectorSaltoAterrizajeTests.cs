using NUnit.Framework;
using TDOM.Gameplay.Vfx;

namespace TDOM.Tests.EditMode
{
    public class DetectorSaltoAterrizajeTests
    {
        private const float Dt = 1f / 60f;
        private const float Gravedad = -30f;
        private const float VelocidadMinima = 6f;
        private const float VelocidadSalto = 10f;

        private DetectorSaltoAterrizaje _detector;

        [SetUp]
        public void Preparar()
        {
            _detector = new DetectorSaltoAterrizaje(VelocidadMinima);
            _detector.Actualizar(true, false, false, 0f);
        }

        [Test]
        public void quieto_en_el_suelo_no_lanza_nada()
        {
            _detector.Actualizar(true, false, false, 0f);

            Assert.IsFalse(_detector.SaltoDesdeSuelo);
            Assert.IsFalse(_detector.Aterrizaje);
        }

        [Test]
        public void al_empezar_un_salto_desde_el_suelo_lo_detecta_un_solo_cuadro()
        {
            _detector.Actualizar(true, true, false, VelocidadSalto);
            Assert.IsTrue(_detector.SaltoDesdeSuelo);

            _detector.Actualizar(false, true, false, VelocidadSalto + Gravedad * Dt);
            Assert.IsFalse(_detector.SaltoDesdeSuelo);
        }

        [Test]
        public void el_salto_aereo_al_caer_de_un_borde_no_cuenta_como_salto_desde_suelo()
        {
            _detector.Actualizar(false, false, false, -2f);
            _detector.Actualizar(false, true, true, VelocidadSalto);

            Assert.IsFalse(_detector.SaltoDesdeSuelo);
        }

        [Test]
        public void el_doble_salto_no_repite_el_salto_desde_suelo()
        {
            _detector.Actualizar(true, true, false, VelocidadSalto);
            _detector.Actualizar(false, true, false, 2f);
            _detector.Actualizar(false, true, true, VelocidadSalto);

            Assert.IsFalse(_detector.SaltoDesdeSuelo);
        }

        [Test]
        public void tras_un_salto_completo_detecta_el_aterrizaje_al_tocar_el_suelo()
        {
            float velocidad = VelocidadSalto;
            _detector.Actualizar(true, true, false, velocidad);
            bool aterrizo = false;
            bool enSuelo = false;
            int cuadros = 0;
            while (!enSuelo && cuadros < 600)
            {
                velocidad += Gravedad * Dt;
                enSuelo = velocidad < -VelocidadSalto;
                _detector.Actualizar(enSuelo, !enSuelo, false, enSuelo ? 0f : velocidad);
                aterrizo |= _detector.Aterrizaje;
                cuadros++;
            }

            Assert.IsTrue(aterrizo);
        }

        [Test]
        public void un_escalon_lento_no_cuenta_como_aterrizaje()
        {
            _detector.Actualizar(false, false, false, -2f);
            _detector.Actualizar(true, false, false, 0f);

            Assert.IsFalse(_detector.Aterrizaje);
        }

        [Test]
        public void el_aterrizaje_solo_dura_un_cuadro()
        {
            _detector.Actualizar(false, false, false, -VelocidadMinima - 1f);
            _detector.Actualizar(true, false, false, 0f);
            Assert.IsTrue(_detector.Aterrizaje);

            _detector.Actualizar(true, false, false, 0f);
            Assert.IsFalse(_detector.Aterrizaje);
        }
    }
}
