using NUnit.Framework;
using TDOM.Gameplay.Vfx;

namespace TDOM.Tests.EditMode
{
    public class IndiceVfxComboTests
    {
        [TestCase(0)]
        [TestCase(1)]
        public void golpe_ligero_usa_su_propio_indice(int comboIndex)
        {
            Assert.AreEqual(comboIndex, IndiceVfxCombo.Resolver(false, comboIndex, comboIndex + 1));
        }

        [TestCase(0)]
        [TestCase(1)]
        public void cargado_tras_un_ligero_usa_el_cierre_sin_importar_el_indice(int comboIndex)
        {
            Assert.AreEqual(2, IndiceVfxCombo.Resolver(true, comboIndex, 1));
        }

        [Test]
        public void cargado_directo_no_tiene_efecto_de_combo()
        {
            Assert.AreEqual(IndiceVfxCombo.SinEfecto, IndiceVfxCombo.Resolver(true, 0, 0));
        }

        [Test]
        public void cargado_tras_dos_ligeros_no_tiene_efecto_de_combo()
        {
            Assert.AreEqual(IndiceVfxCombo.SinEfecto, IndiceVfxCombo.Resolver(true, 0, 2));
        }
    }
}
