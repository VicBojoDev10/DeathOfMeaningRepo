using NUnit.Framework;
using TDOM.Gameplay.Vfx;

namespace TDOM.Tests.EditMode
{
    public class BrilloCargaTests
    {
        private const float Delta = 0.0001f;

        [Test]
        public void con_velocidad_cero_salta_directo_al_objetivo()
        {
            Assert.AreEqual(1f, BrilloCarga.Suavizar(0f, 1f, 0.016f, 0f), Delta);
        }

        [Test]
        public void sin_tiempo_transcurrido_no_cambia()
        {
            Assert.AreEqual(0.3f, BrilloCarga.Suavizar(0.3f, 1f, 0f, 10f), Delta);
        }

        [Test]
        public void se_acerca_al_objetivo_sin_pasarse()
        {
            float valor = 0f;
            for (int i = 0; i < 600; i++)
            {
                float siguiente = BrilloCarga.Suavizar(valor, 0.8f, 0.016f, 12f);
                Assert.GreaterOrEqual(siguiente, valor);
                Assert.LessOrEqual(siguiente, 0.8f + Delta);
                valor = siguiente;
            }

            Assert.AreEqual(0.8f, valor, 0.001f);
        }

        [Test]
        public void baja_al_soltar_la_carga()
        {
            Assert.Less(BrilloCarga.Suavizar(1f, 0f, 0.05f, 12f), 1f);
        }

        [Test]
        public void el_objetivo_fuera_de_rango_se_limita()
        {
            Assert.AreEqual(1f, BrilloCarga.Suavizar(0.5f, 5f, 10f, 100f), Delta);
            Assert.AreEqual(0f, BrilloCarga.Suavizar(0.5f, -5f, 10f, 100f), Delta);
        }

        [Test]
        public void mezclar_va_del_reposo_al_maximo()
        {
            Assert.AreEqual(0.15f, BrilloCarga.Mezclar(0.15f, 1f, 0f), Delta);
            Assert.AreEqual(1f, BrilloCarga.Mezclar(0.15f, 1f, 1f), Delta);
            Assert.AreEqual(1f, BrilloCarga.Mezclar(0.15f, 1f, 7f), Delta);
        }
    }
}
