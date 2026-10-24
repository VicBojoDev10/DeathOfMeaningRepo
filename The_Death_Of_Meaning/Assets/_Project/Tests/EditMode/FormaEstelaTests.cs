using NUnit.Framework;
using TDOM.Gameplay.Vfx;

namespace TDOM.Tests.EditMode
{
    public class FormaEstelaTests
    {
        private const float Delta = 0.0001f;

        [Test]
        public void con_escala_uno_el_ancho_es_el_valor_de_la_curva()
        {
            Assert.AreEqual(0.4f, FormaEstela.Ancho(0.4f, 1f), Delta);
        }

        [Test]
        public void la_escala_multiplica_el_ancho()
        {
            Assert.AreEqual(2f, FormaEstela.Ancho(1f, 2f), Delta);
        }

        [Test]
        public void una_curva_fuera_de_rango_se_limita_antes_de_escalar()
        {
            Assert.AreEqual(2f, FormaEstela.Ancho(5f, 2f), Delta);
            Assert.AreEqual(0f, FormaEstela.Ancho(-1f, 2f), Delta);
        }

        [Test]
        public void una_escala_negativa_no_invierte_el_ancho()
        {
            Assert.AreEqual(0f, FormaEstela.Ancho(1f, -3f), Delta);
        }

        [Test]
        public void la_opacidad_no_baja_del_minimo()
        {
            Assert.AreEqual(0.35f, FormaEstela.Opacidad(0f, 0.35f), Delta);
            Assert.AreEqual(0.35f, FormaEstela.Opacidad(0.2f, 0.35f), Delta);
        }

        [Test]
        public void la_opacidad_mayor_al_minimo_se_conserva()
        {
            Assert.AreEqual(0.9f, FormaEstela.Opacidad(0.9f, 0.35f), Delta);
        }

        [Test]
        public void con_minimo_cero_la_opacidad_llega_a_cero()
        {
            Assert.AreEqual(0f, FormaEstela.Opacidad(0f, 0f), Delta);
        }
    }
}
