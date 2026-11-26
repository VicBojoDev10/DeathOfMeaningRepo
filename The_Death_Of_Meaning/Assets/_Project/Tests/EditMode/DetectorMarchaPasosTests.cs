using NUnit.Framework;
using TDOM.Gameplay.Audio;

namespace TDOM.Tests.EditMode
{
    public class DetectorMarchaPasosTests
    {
        private const float RapidezMinima = 0.5f;
        private const float Lento = 0.2f;
        private const float Rapido = 4f;

        [Test]
        public void empieza_quieto_y_sin_cambios()
        {
            var detector = new DetectorMarchaPasos();

            detector.Actualizar(true, Lento, false, RapidezMinima);

            Assert.AreEqual(MarchaPasos.Quieto, detector.Actual);
            Assert.IsFalse(detector.Cambio);
            Assert.IsFalse(detector.Freno);
        }

        [Test]
        public void camina_en_el_suelo_por_encima_de_la_rapidez_minima()
        {
            var detector = new DetectorMarchaPasos();

            detector.Actualizar(true, Rapido, false, RapidezMinima);

            Assert.AreEqual(MarchaPasos.Caminando, detector.Actual);
            Assert.IsTrue(detector.Cambio);
        }

        [Test]
        public void corre_cuando_el_jugador_esta_corriendo()
        {
            var detector = new DetectorMarchaPasos();

            detector.Actualizar(true, Rapido, true, RapidezMinima);

            Assert.AreEqual(MarchaPasos.Corriendo, detector.Actual);
        }

        [Test]
        public void solo_marca_cambio_en_el_cuadro_en_que_cambia()
        {
            var detector = new DetectorMarchaPasos();

            detector.Actualizar(true, Rapido, false, RapidezMinima);
            detector.Actualizar(true, Rapido, false, RapidezMinima);

            Assert.IsFalse(detector.Cambio);
        }

        [Test]
        public void frena_al_detenerse_en_el_suelo_y_recuerda_la_marcha_anterior()
        {
            var detector = new DetectorMarchaPasos();

            detector.Actualizar(true, Rapido, true, RapidezMinima);
            detector.Actualizar(true, Lento, true, RapidezMinima);

            Assert.AreEqual(MarchaPasos.Quieto, detector.Actual);
            Assert.AreEqual(MarchaPasos.Corriendo, detector.Anterior);
            Assert.IsTrue(detector.Freno);
        }

        [Test]
        public void no_frena_al_dejar_el_suelo()
        {
            var detector = new DetectorMarchaPasos();

            detector.Actualizar(true, Rapido, false, RapidezMinima);
            detector.Actualizar(false, Rapido, false, RapidezMinima);

            Assert.AreEqual(MarchaPasos.Quieto, detector.Actual);
            Assert.IsTrue(detector.Cambio);
            Assert.IsFalse(detector.Freno);
        }

        [Test]
        public void pasa_de_caminar_a_correr_sin_frenar()
        {
            var detector = new DetectorMarchaPasos();

            detector.Actualizar(true, Rapido, false, RapidezMinima);
            detector.Actualizar(true, Rapido, true, RapidezMinima);

            Assert.AreEqual(MarchaPasos.Corriendo, detector.Actual);
            Assert.AreEqual(MarchaPasos.Caminando, detector.Anterior);
            Assert.IsTrue(detector.Cambio);
            Assert.IsFalse(detector.Freno);
        }

        [Test]
        public void reiniciar_deja_quieto_sin_frenar()
        {
            var detector = new DetectorMarchaPasos();

            detector.Actualizar(true, Rapido, false, RapidezMinima);
            detector.Reiniciar();

            Assert.AreEqual(MarchaPasos.Quieto, detector.Actual);
            Assert.AreEqual(MarchaPasos.Caminando, detector.Anterior);
            Assert.IsTrue(detector.Cambio);
            Assert.IsFalse(detector.Freno);
        }
    }
}
