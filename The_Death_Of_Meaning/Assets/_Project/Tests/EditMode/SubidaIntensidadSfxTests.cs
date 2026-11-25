using NUnit.Framework;
using TDOM.Gameplay.Audio;

namespace TDOM.Tests.EditMode
{
    public class SubidaIntensidadSfxTests
    {
        private const float Delta = 0.001f;
        private const float DtFijo = 1f / 60f;

        [Test]
        public void al_empezar_la_carga_la_intensidad_es_cero()
        {
            Assert.AreEqual(0f, SubidaIntensidadSfx.Factor(0f, 2f), Delta);
        }

        [Test]
        public void a_la_mitad_de_la_subida_la_intensidad_es_la_mitad()
        {
            Assert.AreEqual(0.5f, SubidaIntensidadSfx.Factor(1f, 2f), Delta);
        }

        [Test]
        public void despues_de_la_subida_la_intensidad_se_queda_al_maximo()
        {
            Assert.AreEqual(1f, SubidaIntensidadSfx.Factor(5f, 2f), Delta);
        }

        [Test]
        public void sin_subida_suena_al_maximo_desde_el_inicio()
        {
            Assert.AreEqual(1f, SubidaIntensidadSfx.Factor(0f, 0f), Delta);
        }

        [Test]
        public void la_intensidad_sube_igual_con_dt_fijo()
        {
            float tiempo = 0f;
            for (int i = 0; i < 60; i++)
                tiempo += DtFijo;

            Assert.AreEqual(0.5f, SubidaIntensidadSfx.Factor(tiempo, 2f), Delta);
        }

        [Test]
        public void interpola_entre_el_valor_inicial_y_el_final()
        {
            Assert.AreEqual(0.3f, SubidaIntensidadSfx.Interpolar(0.3f, 1f, 0f), Delta);
            Assert.AreEqual(0.65f, SubidaIntensidadSfx.Interpolar(0.3f, 1f, 0.5f), Delta);
            Assert.AreEqual(1f, SubidaIntensidadSfx.Interpolar(0.3f, 1f, 1f), Delta);
        }
    }
}
