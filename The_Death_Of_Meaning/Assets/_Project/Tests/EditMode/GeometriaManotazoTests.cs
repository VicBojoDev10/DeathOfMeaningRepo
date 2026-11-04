using NUnit.Framework;
using TDOM.Gameplay.Combat;

namespace TDOM.Tests.EditMode
{
    public class GeometriaManotazoTests
    {
        private const float Tolerancia = 0.001f;

        [Test]
        public void El_grosor_es_proporcional_al_largo_hasta_el_maximo()
        {
            float mitad = GeometriaManotazo.Grosor(10f, 0.2f, 1f, 100f);
            float doble = GeometriaManotazo.Grosor(20f, 0.2f, 1f, 100f);

            Assert.AreEqual(2f, mitad, Tolerancia);
            Assert.AreEqual(4f, doble, Tolerancia);
        }

        [Test]
        public void El_multiplicador_escala_el_grosor()
        {
            Assert.AreEqual(3f, GeometriaManotazo.Grosor(10f, 0.2f, 1.5f, 100f), Tolerancia);
        }

        [Test]
        public void El_grosor_no_pasa_del_maximo()
        {
            Assert.AreEqual(4f, GeometriaManotazo.Grosor(30f, 0.35f, 1f, 4f), Tolerancia);
        }

        [Test]
        public void El_grosor_crece_desde_cero_sin_saltos()
        {
            Assert.AreEqual(0f, GeometriaManotazo.Grosor(0f, 0.2f, 1f, 4f), Tolerancia);

            float anterior = 0f;
            for (float largo = 0f; largo <= 30f; largo += 0.25f)
            {
                float grosor = GeometriaManotazo.Grosor(largo, 0.35f, 1f, 4f);
                Assert.GreaterOrEqual(grosor, anterior);
                Assert.LessOrEqual(grosor - anterior, 0.35f * 0.25f + Tolerancia);
                anterior = grosor;
            }
        }

        [Test]
        public void Un_largo_o_multiplicador_negativos_dan_grosor_cero()
        {
            Assert.AreEqual(0f, GeometriaManotazo.Grosor(-5f, 0.2f, 1f, 4f), Tolerancia);
            Assert.AreEqual(0f, GeometriaManotazo.Grosor(10f, 0.2f, -1f, 4f), Tolerancia);
        }

        [Test]
        public void La_banda_de_dano_sin_altura_es_la_de_siempre()
        {
            (float centro, float mitad) = GeometriaManotazo.BandaDano(2f, 0f, true);

            Assert.AreEqual(1f, centro, Tolerancia);
            Assert.AreEqual(1f, mitad, Tolerancia);
        }

        [Test]
        public void La_banda_a_ras_del_piso_no_crece_con_la_altura_del_origen()
        {
            (float centro, float mitad) = GeometriaManotazo.BandaDano(2f, 30f, true);

            Assert.AreEqual(-29f, centro, Tolerancia);
            Assert.AreEqual(1f, mitad, Tolerancia);
        }

        [Test]
        public void La_pared_va_del_piso_hasta_lo_alto_de_la_columna()
        {
            (float centro, float mitad) = GeometriaManotazo.BandaDano(2f, 30f, false);

            Assert.AreEqual(-14f, centro, Tolerancia);
            Assert.AreEqual(16f, mitad, Tolerancia);
        }
    }
}
