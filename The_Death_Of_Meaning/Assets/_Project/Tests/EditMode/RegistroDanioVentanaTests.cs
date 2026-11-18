using NUnit.Framework;
using TDOM.Gameplay.Combat;

namespace TDOM.Tests.EditMode
{
    public sealed class RegistroDanioVentanaTests
    {
        [Test]
        public void AcumulaDentroDeVentanaYConsumeAlLlegarAlUmbral()
        {
            var registro = new RegistroDanioVentana();
            Assert.IsFalse(registro.Registrar(40f, 0f, 5f, 1000f, 0.1f));
            Assert.IsTrue(registro.Registrar(60f, 4.9f, 5f, 1000f, 0.1f));
            Assert.AreEqual(0f, registro.Acumulado);
        }

        [Test]
        public void DescartaDanioFueraDeLaVentana()
        {
            var registro = new RegistroDanioVentana();
            Assert.IsFalse(registro.Registrar(60f, 0f, 5f, 1000f, 0.1f));
            Assert.IsFalse(registro.Registrar(40f, 5.01f, 5f, 1000f, 0.1f));
            Assert.AreEqual(40f, registro.Acumulado);
        }

        [Test]
        public void UmbralEsInclusivo()
        {
            var registro = new RegistroDanioVentana();
            Assert.IsTrue(registro.Registrar(100f, 1f, 5f, 1000f, 0.1f));
        }
    }
}
