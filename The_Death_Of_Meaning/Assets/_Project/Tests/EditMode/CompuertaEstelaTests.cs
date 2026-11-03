using NUnit.Framework;
using TDOM.Gameplay.Vfx;

namespace TDOM.Tests.EditMode
{
    public class CompuertaEstelaTests
    {
        [Test]
        public void empieza_cerrada()
        {
            var compuerta = new CompuertaEstela(3f, 1.5f);

            Assert.IsFalse(compuerta.Abierta);
        }

        [Test]
        public void se_abre_al_superar_la_velocidad_de_encendido()
        {
            var compuerta = new CompuertaEstela(3f, 1.5f);

            Assert.IsFalse(compuerta.Evaluar(2.9f, true));
            Assert.IsTrue(compuerta.Evaluar(3f, true));
        }

        [Test]
        public void entre_los_dos_umbrales_conserva_su_estado()
        {
            var compuerta = new CompuertaEstela(3f, 1.5f);

            Assert.IsFalse(compuerta.Evaluar(2f, true));
            compuerta.Evaluar(5f, true);
            Assert.IsTrue(compuerta.Evaluar(2f, true));
        }

        [Test]
        public void se_cierra_al_bajar_de_la_velocidad_de_apagado()
        {
            var compuerta = new CompuertaEstela(3f, 1.5f);
            compuerta.Evaluar(5f, true);

            Assert.IsFalse(compuerta.Evaluar(1.4f, true));
        }

        [Test]
        public void no_se_abre_si_el_ataque_no_la_permite()
        {
            var compuerta = new CompuertaEstela(3f, 1.5f);

            Assert.IsFalse(compuerta.Evaluar(20f, false));
        }

        [Test]
        public void se_cierra_en_cuanto_deja_de_estar_permitida()
        {
            var compuerta = new CompuertaEstela(3f, 1.5f);
            compuerta.Evaluar(5f, true);

            Assert.IsFalse(compuerta.Evaluar(5f, false));
            Assert.IsTrue(compuerta.Evaluar(5f, true));
        }

        [Test]
        public void un_umbral_de_apagado_mayor_que_el_de_encendido_se_limita()
        {
            var compuerta = new CompuertaEstela(3f, 10f);
            compuerta.Evaluar(5f, true);

            Assert.IsTrue(compuerta.Evaluar(3f, true));
            Assert.IsFalse(compuerta.Evaluar(2.9f, true));
        }

        [Test]
        public void reiniciar_la_cierra()
        {
            var compuerta = new CompuertaEstela(3f, 1.5f);
            compuerta.Evaluar(5f, true);

            compuerta.Reiniciar();

            Assert.IsFalse(compuerta.Abierta);
        }
    }
}
