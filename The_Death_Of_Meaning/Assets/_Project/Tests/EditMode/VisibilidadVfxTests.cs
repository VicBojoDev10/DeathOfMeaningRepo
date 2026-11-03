using NUnit.Framework;
using TDOM.Gameplay.Vfx;

namespace TDOM.Tests.EditMode
{
    public class VisibilidadVfxTests
    {
        private const float Delta = 0.001f;

        [Test]
        public void al_empezar_con_aparicion_el_efecto_es_invisible()
        {
            Assert.AreEqual(0f, VisibilidadVfx.Factor(0f, 1f, 0.2f, 0.2f), Delta);
        }

        [Test]
        public void a_la_mitad_de_la_aparicion_se_ve_a_la_mitad()
        {
            Assert.AreEqual(0.5f, VisibilidadVfx.Factor(0.1f, 1f, 0.2f, 0.2f), Delta);
        }

        [Test]
        public void entre_aparicion_y_desaparicion_se_ve_completo()
        {
            Assert.AreEqual(1f, VisibilidadVfx.Factor(0.5f, 1f, 0.2f, 0.2f), Delta);
        }

        [Test]
        public void a_la_mitad_de_la_desaparicion_se_ve_a_la_mitad()
        {
            Assert.AreEqual(0.5f, VisibilidadVfx.Factor(0.9f, 1f, 0.2f, 0.2f), Delta);
        }

        [Test]
        public void al_terminar_la_duracion_el_efecto_es_invisible()
        {
            Assert.AreEqual(0f, VisibilidadVfx.Factor(1f, 1f, 0.2f, 0.2f), Delta);
        }

        [Test]
        public void sin_aparicion_ni_desaparicion_siempre_se_ve_completo()
        {
            Assert.AreEqual(1f, VisibilidadVfx.Factor(0f, 1f, 0f, 0f), Delta);
            Assert.AreEqual(1f, VisibilidadVfx.Factor(1f, 1f, 0f, 0f), Delta);
        }

        [Test]
        public void si_se_cruzan_gana_el_valor_mas_bajo()
        {
            Assert.AreEqual(0.5f, VisibilidadVfx.Factor(0.25f, 0.5f, 0.5f, 0.5f), Delta);
        }
    }
}
