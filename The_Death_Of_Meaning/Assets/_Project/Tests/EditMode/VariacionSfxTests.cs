using NUnit.Framework;
using TDOM.Gameplay.Audio;

namespace TDOM.Tests.EditMode
{
    public class VariacionSfxTests
    {
        private const float Delta = 0.001f;

        [Test]
        public void con_aleatorio_a_la_mitad_el_volumen_no_cambia()
        {
            Assert.AreEqual(0.8f, VariacionSfx.Volumen(0.8f, 0.1f, 0.5f), Delta);
        }

        [Test]
        public void el_volumen_varia_hacia_abajo_y_hacia_arriba()
        {
            Assert.AreEqual(0.7f, VariacionSfx.Volumen(0.8f, 0.1f, 0f), Delta);
            Assert.AreEqual(0.9f, VariacionSfx.Volumen(0.8f, 0.1f, 1f), Delta);
        }

        [Test]
        public void el_volumen_nunca_pasa_de_uno_ni_baja_de_cero()
        {
            Assert.AreEqual(1f, VariacionSfx.Volumen(0.95f, 0.2f, 1f), Delta);
            Assert.AreEqual(0f, VariacionSfx.Volumen(0.05f, 0.2f, 0f), Delta);
        }

        [Test]
        public void el_pitch_varia_alrededor_del_base()
        {
            Assert.AreEqual(0.9f, VariacionSfx.Pitch(1f, 0.1f, 0f), Delta);
            Assert.AreEqual(1.1f, VariacionSfx.Pitch(1f, 0.1f, 1f), Delta);
        }

        [Test]
        public void el_pitch_nunca_baja_del_minimo()
        {
            Assert.AreEqual(VariacionSfx.PitchMinimo, VariacionSfx.Pitch(0.1f, 0.5f, 0f), Delta);
        }

        [Test]
        public void sin_variacion_los_valores_quedan_iguales()
        {
            Assert.AreEqual(0.6f, VariacionSfx.Volumen(0.6f, 0f, 0f), Delta);
            Assert.AreEqual(1.2f, VariacionSfx.Pitch(1.2f, 0f, 1f), Delta);
        }

        [Test]
        public void sin_clips_no_hay_indice()
        {
            Assert.AreEqual(VariacionSfx.SinClip, VariacionSfx.IndiceClip(0, -1, 0.5f));
        }

        [Test]
        public void con_un_solo_clip_siempre_suena_ese()
        {
            Assert.AreEqual(0, VariacionSfx.IndiceClip(1, 0, 0.9f));
        }

        [Test]
        public void nunca_repite_el_clip_anterior()
        {
            for (float aleatorio = 0f; aleatorio <= 1f; aleatorio += 0.05f)
                Assert.AreNotEqual(1, VariacionSfx.IndiceClip(3, 1, aleatorio));
        }

        [Test]
        public void sin_anterior_puede_elegir_cualquier_clip()
        {
            Assert.AreEqual(0, VariacionSfx.IndiceClip(3, -1, 0f));
            Assert.AreEqual(2, VariacionSfx.IndiceClip(3, -1, 1f));
        }
    }
}
