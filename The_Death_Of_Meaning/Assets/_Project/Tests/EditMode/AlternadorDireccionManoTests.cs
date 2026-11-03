using NUnit.Framework;
using TDOM.Gameplay.Combat;

namespace TDOM.Tests.EditMode
{
    public class AlternadorDireccionManoTests
    {
        private AlternadorDireccionMano alternador;

        [SetUp]
        public void Setup()
        {
            alternador = new AlternadorDireccionMano();
        }

        [Test]
        public void El_primer_ataque_usa_el_sentido_original()
        {
            Assert.AreEqual(1, alternador.Siguiente());
        }

        [Test]
        public void Cada_ataque_alterna_el_sentido()
        {
            Assert.AreEqual(1, alternador.Siguiente());
            Assert.AreEqual(-1, alternador.Siguiente());
            Assert.AreEqual(1, alternador.Siguiente());
            Assert.AreEqual(-1, alternador.Siguiente());
        }

        [Test]
        public void Proximo_no_consume_la_alternancia()
        {
            Assert.AreEqual(1, alternador.Proximo);
            Assert.AreEqual(1, alternador.Proximo);
            Assert.AreEqual(1, alternador.Siguiente());
            Assert.AreEqual(-1, alternador.Proximo);
        }

        [Test]
        public void Reiniciar_vuelve_al_sentido_original()
        {
            alternador.Siguiente();
            alternador.Reiniciar();

            Assert.AreEqual(1, alternador.Siguiente());
        }
    }
}
