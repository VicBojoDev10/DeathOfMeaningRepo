using NUnit.Framework;
using TDOM.Gameplay.Vfx;

namespace TDOM.Tests.EditMode
{
    public class EtapasCargaTests
    {
        private const float Delta = 0.001f;
        private const float Dt = 1f / 60f;
        private static readonly float[] Inicios = { 0f, 0.5f, 1f };

        [Test]
        public void al_empezar_la_carga_es_la_primera_etapa()
        {
            Assert.AreEqual(0, EtapasCarga.Indice(Inicios, 0f));
        }

        [Test]
        public void a_la_mitad_de_la_carga_es_la_segunda_etapa()
        {
            Assert.AreEqual(1, EtapasCarga.Indice(Inicios, 0.5f));
        }

        [Test]
        public void justo_antes_del_maximo_sigue_en_la_segunda_etapa()
        {
            Assert.AreEqual(1, EtapasCarga.Indice(Inicios, 1f - Dt));
        }

        [Test]
        public void con_la_carga_completa_es_la_ultima_etapa()
        {
            Assert.AreEqual(2, EtapasCarga.Indice(Inicios, 1f));
        }

        [Test]
        public void sin_etapas_no_hay_indice()
        {
            Assert.AreEqual(EtapasCarga.SinEtapa, EtapasCarga.Indice(new float[0], 0.5f));
        }

        [Test]
        public void el_progreso_suma_lo_ya_cargado()
        {
            Assert.AreEqual(0.5f, EtapasCarga.Progreso(0.2f, 0.3f, 1f), Delta);
        }

        [Test]
        public void el_progreso_no_pasa_de_uno()
        {
            Assert.AreEqual(1f, EtapasCarga.Progreso(0.2f, 5f, 1f), Delta);
        }

        [Test]
        public void el_progreso_avanza_igual_con_dt_fijo()
        {
            float transcurrido = 0f;
            for (int i = 0; i < 30; i++)
                transcurrido += Dt;

            Assert.AreEqual(0.7f, EtapasCarga.Progreso(0.2f, transcurrido, 1f), Delta);
        }
    }
}
