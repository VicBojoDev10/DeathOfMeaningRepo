using NUnit.Framework;
using TDOM.Gameplay.Combat;

namespace TDOM.Tests.EditMode
{
    public class ReglaDropPowerUpTest
    {
        private static readonly float[] DosPesos = { 70f, 30f };

        [Test]
        public void probabilidad_cero_nunca_suelta_antes_de_la_garantia()
        {
            var regla = new ReglaDropPowerUp(0f, 4, 4, DosPesos);

            for (int i = 0; i < 3; i++)
                Assert.IsNull(regla.Elegir(0f, 0f, 0));
        }

        [Test]
        public void probabilidad_uno_siempre_suelta()
        {
            var regla = new ReglaDropPowerUp(1f, 4, 4, DosPesos);

            for (int i = 0; i < 10; i++)
                Assert.IsTrue(regla.Elegir(1f, 0f, 0).HasValue);
        }

        [Test]
        public void suelta_solo_si_el_azar_es_menor_que_la_probabilidad()
        {
            var regla = new ReglaDropPowerUp(0.3f, 4, 4, DosPesos);

            Assert.IsTrue(regla.Elegir(0.29f, 0f, 0).HasValue);
            Assert.IsNull(regla.Elegir(0.3f, 0f, 0));
            Assert.IsNull(regla.Elegir(0.99f, 0f, 0));
        }

        [Test]
        public void la_garantia_suelta_en_la_muerte_numero_n_sin_drop()
        {
            var regla = new ReglaDropPowerUp(0f, 4, 4, DosPesos);

            Assert.IsNull(regla.Elegir(0.9f, 0f, 0));
            Assert.IsNull(regla.Elegir(0.9f, 0f, 0));
            Assert.IsNull(regla.Elegir(0.9f, 0f, 0));
            Assert.AreEqual(3, regla.MuertesSinDrop);

            Assert.IsTrue(regla.Elegir(0.9f, 0f, 0).HasValue);
        }

        [Test]
        public void el_contador_se_reinicia_tras_un_drop()
        {
            var regla = new ReglaDropPowerUp(0.3f, 4, 4, DosPesos);
            regla.Elegir(0.9f, 0f, 0);
            regla.Elegir(0.9f, 0f, 0);
            Assert.AreEqual(2, regla.MuertesSinDrop);

            Assert.IsTrue(regla.Elegir(0.1f, 0f, 0).HasValue);
            Assert.AreEqual(0, regla.MuertesSinDrop);

            Assert.IsNull(regla.Elegir(0.9f, 0f, 0));
            Assert.AreEqual(1, regla.MuertesSinDrop);
        }

        [Test]
        public void no_suelta_si_ya_hay_el_maximo_de_vivos()
        {
            var regla = new ReglaDropPowerUp(1f, 4, 4, DosPesos);

            Assert.IsNull(regla.Elegir(0f, 0f, 4));
            Assert.IsNull(regla.Elegir(0f, 0f, 7));
            Assert.IsTrue(regla.Elegir(0f, 0f, 3).HasValue);
        }

        [Test]
        public void la_garantia_tampoco_rompe_el_maximo_de_vivos()
        {
            var regla = new ReglaDropPowerUp(0f, 2, 1, DosPesos);

            Assert.IsNull(regla.Elegir(0.9f, 0f, 1));
            Assert.IsNull(regla.Elegir(0.9f, 0f, 1));
            Assert.IsTrue(regla.Elegir(0.9f, 0f, 0).HasValue);
        }

        [Test]
        public void el_peso_elige_la_entrada_segun_el_azar()
        {
            var regla = new ReglaDropPowerUp(1f, 4, 99, DosPesos);

            Assert.AreEqual(0, regla.Elegir(0f, 0f, 0));
            Assert.AreEqual(0, regla.Elegir(0f, 0.69f, 0));
            Assert.AreEqual(1, regla.Elegir(0f, 0.71f, 0));
            Assert.AreEqual(1, regla.Elegir(0f, 1f, 0));
        }

        [Test]
        public void el_peso_reparte_con_semilla_fija_cerca_de_la_proporcion()
        {
            var regla = new ReglaDropPowerUp(1f, 4, 100000, DosPesos);
            var azar = new System.Random(1234);
            int individual = 0;
            int total = 10000;

            for (int i = 0; i < total; i++)
            {
                int? elegido = regla.Elegir(0f, (float)azar.NextDouble(), 0);
                if (elegido == 0)
                    individual++;
            }

            Assert.That(individual / (float)total, Is.EqualTo(0.7f).Within(0.03f));
        }

        [Test]
        public void una_entrada_con_peso_cero_nunca_se_elige()
        {
            var regla = new ReglaDropPowerUp(1f, 4, 99, new[] { 0f, 5f, 0f });

            Assert.AreEqual(1, regla.Elegir(0f, 0f, 0));
            Assert.AreEqual(1, regla.Elegir(0f, 0.5f, 0));
            Assert.AreEqual(1, regla.Elegir(0f, 1f, 0));
        }

        [Test]
        public void sin_entradas_o_sin_peso_no_suelta()
        {
            var vacia = new ReglaDropPowerUp(1f, 1, 4, new float[0]);
            var sinPeso = new ReglaDropPowerUp(1f, 1, 4, new[] { 0f, 0f });

            Assert.IsNull(vacia.Elegir(0f, 0f, 0));
            Assert.IsNull(sinPeso.Elegir(0f, 0f, 0));
        }
    }
}
