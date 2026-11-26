using System.Collections.Generic;
using NUnit.Framework;
using TDOM.Gameplay;

namespace TDOM.Tests.EditMode
{
    public class SelloSaltoAereoTests
    {
        private const float DuracionSellado = 4f;
        private const float DuracionRuptura = 1f;
        private const float Lentitud = 0.4f;
        private const float LentitudSellado = 0.8f;
        private const float Dt60 = 1f / 60f;

        private static SelloSaltoAereo Crear() =>
            new SelloSaltoAereo(DuracionSellado, DuracionRuptura, Lentitud, LentitudSellado);

        [Test]
        public void libre_permite_el_doble_salto_a_velocidad_normal()
        {
            var sello = Crear();

            Assert.IsTrue(sello.PermiteSaltoAereo);
            Assert.AreEqual(1f, sello.MultiplicadorVelocidad);
        }

        [Test]
        public void al_sellarse_pierde_el_doble_salto_y_va_lento()
        {
            var sello = Crear();

            Assert.IsTrue(sello.Sellar());

            Assert.IsFalse(sello.PermiteSaltoAereo);
            Assert.AreEqual(Lentitud, sello.MultiplicadorVelocidad);
        }

        [TestCase(DuracionSellado - 0.01f, EstadoSelloSalto.Sellando)]
        [TestCase(DuracionSellado, EstadoSelloSalto.Sellado)]
        public void la_lentitud_dura_lo_que_la_animacion_del_sello(
            float tiempo,
            EstadoSelloSalto esperado
        )
        {
            var sello = Crear();
            sello.Sellar();

            sello.Tick(tiempo);

            Assert.AreEqual(esperado, sello.Estado);
        }

        [Test]
        public void sellado_sigue_sin_doble_salto_y_con_su_propia_lentitud()
        {
            var sello = Crear();
            sello.Sellar();
            sello.Tick(DuracionSellado);

            Assert.IsFalse(sello.PermiteSaltoAereo);
            Assert.AreEqual(LentitudSellado, sello.MultiplicadorVelocidad);
        }

        [Test]
        public void romper_devuelve_el_doble_salto_de_inmediato_y_termina_libre()
        {
            var sello = Crear();
            sello.Sellar();
            sello.Tick(DuracionSellado);

            Assert.IsTrue(sello.Romper());
            Assert.IsTrue(sello.PermiteSaltoAereo);
            Assert.AreEqual(1f, sello.MultiplicadorVelocidad);

            sello.Tick(DuracionRuptura);
            Assert.AreEqual(EstadoSelloSalto.Libre, sello.Estado);
        }

        [Test]
        public void sin_sello_no_hay_nada_que_romper()
        {
            Assert.IsFalse(Crear().Romper());
        }

        [Test]
        public void tras_romperlo_no_se_vuelve_a_sellar_hasta_reiniciar()
        {
            var sello = Crear();
            sello.Sellar();
            sello.Romper();
            sello.Tick(DuracionRuptura);

            Assert.IsFalse(sello.Sellar());

            sello.Reiniciar();
            Assert.IsTrue(sello.Sellar());
        }

        [Test]
        public void con_dt_fijo_el_progreso_crece_y_es_igual_en_dos_simulaciones()
        {
            List<float> Simular()
            {
                var sello = Crear();
                sello.Sellar();
                var progresos = new List<float>();
                for (int i = 0; i < 300; i++)
                {
                    sello.Tick(Dt60);
                    progresos.Add(sello.Progreso);
                }
                return progresos;
            }

            var primera = Simular();
            CollectionAssert.AreEqual(primera, Simular());
            for (int i = 1; i < primera.Count; i++)
                Assert.GreaterOrEqual(primera[i], primera[i - 1]);
            Assert.AreEqual(1f, primera[primera.Count - 1]);
        }
    }
}
