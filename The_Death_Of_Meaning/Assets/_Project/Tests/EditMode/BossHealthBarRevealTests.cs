using NUnit.Framework;
using TDOM.Gameplay.Core;

namespace TDOM.Tests.EditMode
{
    public class BossHealthBarRevealTests
    {
        [Test]
        public void Sin_jefe_la_barra_no_se_revela()
        {
            var barra = new BossHealthBarReveal(true);
            int revelaciones = 0;
            barra.Revelar += () => revelaciones++;

            barra.CambiarVisibilidadHud(true);

            Assert.AreEqual(0, revelaciones);
            Assert.IsFalse(barra.Revelada);
        }

        [Test]
        public void Si_el_hud_esta_visible_la_barra_se_revela_al_aparecer_el_jefe()
        {
            var barra = new BossHealthBarReveal(true);
            int revelaciones = 0;
            barra.Revelar += () => revelaciones++;

            barra.RegistrarJefe();

            Assert.AreEqual(1, revelaciones);
            Assert.IsTrue(barra.Revelada);
        }

        [Test]
        public void Durante_la_cinematica_la_barra_espera_a_que_vuelva_el_hud()
        {
            var barra = new BossHealthBarReveal(true);
            int revelaciones = 0;
            barra.Revelar += () => revelaciones++;

            barra.CambiarVisibilidadHud(false);
            barra.RegistrarJefe();
            Assert.AreEqual(0, revelaciones);

            barra.CambiarVisibilidadHud(true);
            Assert.AreEqual(1, revelaciones);
        }

        [Test]
        public void La_barra_se_revela_una_sola_vez_aunque_el_hud_se_oculte_y_vuelva()
        {
            var barra = new BossHealthBarReveal(true);
            int revelaciones = 0;
            barra.Revelar += () => revelaciones++;

            barra.RegistrarJefe();
            barra.CambiarVisibilidadHud(false);
            barra.CambiarVisibilidadHud(true);

            Assert.AreEqual(1, revelaciones);
        }

        [Test]
        public void Retirar_el_jefe_oculta_la_barra_y_permite_revelarla_con_el_siguiente()
        {
            var barra = new BossHealthBarReveal(true);
            int revelaciones = 0;
            int ocultaciones = 0;
            barra.Revelar += () => revelaciones++;
            barra.Ocultar += () => ocultaciones++;

            barra.RegistrarJefe();
            barra.RetirarJefe();
            barra.RegistrarJefe();

            Assert.AreEqual(1, ocultaciones);
            Assert.AreEqual(2, revelaciones);
        }

        [Test]
        public void Retirar_el_jefe_sin_barra_revelada_no_emite_ocultar()
        {
            var barra = new BossHealthBarReveal(false);
            int ocultaciones = 0;
            barra.Ocultar += () => ocultaciones++;

            barra.RegistrarJefe();
            barra.RetirarJefe();

            Assert.AreEqual(0, ocultaciones);
        }
    }
}
