using NUnit.Framework;
using TDOM.Gameplay.Audio;

namespace TDOM.Tests.EditMode
{
    public class DetectorFlancoSfxTests
    {
        [Test]
        public void suena_una_sola_vez_al_entrar_en_la_fase()
        {
            var detector = new DetectorFlancoSfx();

            Assert.IsTrue(detector.Actualizar(true));
            Assert.IsFalse(detector.Actualizar(true));
            Assert.IsFalse(detector.Actualizar(true));
        }

        [Test]
        public void no_suena_mientras_la_fase_no_empieza()
        {
            var detector = new DetectorFlancoSfx();

            Assert.IsFalse(detector.Actualizar(false));
            Assert.IsFalse(detector.Actualizar(false));
        }

        [Test]
        public void vuelve_a_sonar_si_la_fase_termina_y_empieza_otra_vez()
        {
            var detector = new DetectorFlancoSfx();

            detector.Actualizar(true);
            detector.Actualizar(false);

            Assert.IsTrue(detector.Actualizar(true));
        }
    }
}
