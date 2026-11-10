using NUnit.Framework;
using TDOM.Gameplay.Audio;

namespace TDOM.Tests.EditMode
{
    public class SfxPorIndiceTests
    {
        [Test]
        public void cada_golpe_del_combo_usa_su_propio_sonido()
        {
            Assert.AreEqual(0, SfxPorIndice.Elegir(0, 3));
            Assert.AreEqual(1, SfxPorIndice.Elegir(1, 3));
            Assert.AreEqual(2, SfxPorIndice.Elegir(2, 3));
        }

        [Test]
        public void si_el_combo_tiene_mas_golpes_que_sonidos_repite_el_ultimo()
        {
            Assert.AreEqual(2, SfxPorIndice.Elegir(4, 3));
        }

        [Test]
        public void sin_sonidos_no_suena_nada()
        {
            Assert.AreEqual(SfxPorIndice.SinSfx, SfxPorIndice.Elegir(0, 0));
        }

        [Test]
        public void un_indice_negativo_no_suena()
        {
            Assert.AreEqual(SfxPorIndice.SinSfx, SfxPorIndice.Elegir(-1, 3));
        }
    }
}
