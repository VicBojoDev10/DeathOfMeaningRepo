using NUnit.Framework;
using TDOM.Gameplay.Audio;

namespace TDOM.Tests.EditMode
{
    public class SeleccionVozSfxTests
    {
        [Test]
        public void sin_voces_pide_crear_una_nueva()
        {
            Assert.AreEqual(
                SeleccionVozSfx.CrearNueva,
                SeleccionVozSfx.Elegir(new bool[0], new float[0], 3)
            );
        }

        [Test]
        public void reutiliza_la_primera_voz_libre()
        {
            Assert.AreEqual(
                1,
                SeleccionVozSfx.Elegir(new[] { true, false, false }, new[] { 0f, 1f, 2f }, 3)
            );
        }

        [Test]
        public void si_todas_suenan_y_hay_espacio_pide_crear_una_nueva()
        {
            Assert.AreEqual(
                SeleccionVozSfx.CrearNueva,
                SeleccionVozSfx.Elegir(new[] { true, true }, new[] { 0f, 1f }, 3)
            );
        }

        [Test]
        public void si_todas_suenan_y_no_hay_espacio_corta_la_mas_vieja()
        {
            Assert.AreEqual(
                2,
                SeleccionVozSfx.Elegir(new[] { true, true, true }, new[] { 5f, 3f, 1f }, 3)
            );
        }
    }
}
