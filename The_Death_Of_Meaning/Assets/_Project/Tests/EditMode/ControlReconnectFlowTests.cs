using NUnit.Framework;
using TDOM.Gameplay.Core;

namespace TDOM.Tests.EditMode
{
    public class ControlReconnectFlowTests
    {
        [Test]
        public void desconexion_muestra_popup_y_bloquea_continuar()
        {
            var flujo = new ControlReconnectFlow();

            flujo.RegistrarDesconexion();

            Assert.IsTrue(flujo.PopupVisible);
            Assert.IsFalse(flujo.PuedeContinuar);
            Assert.IsFalse(flujo.ConfirmarContinuacion());
        }

        [Test]
        public void reconexion_habilita_continuar_y_espera_confirmacion()
        {
            var flujo = new ControlReconnectFlow();
            flujo.RegistrarDesconexion();

            flujo.RegistrarReconexion();

            Assert.IsTrue(flujo.PopupVisible);
            Assert.IsTrue(flujo.PuedeContinuar);
            Assert.IsTrue(flujo.ConfirmarContinuacion());
            Assert.IsFalse(flujo.PopupVisible);
            Assert.IsFalse(flujo.PuedeContinuar);
        }

        [Test]
        public void reconexion_sin_desconexion_no_habilita_ni_muestra_popup()
        {
            var flujo = new ControlReconnectFlow();

            flujo.RegistrarReconexion();

            Assert.IsFalse(flujo.PopupVisible);
            Assert.IsFalse(flujo.PuedeContinuar);
            Assert.IsFalse(flujo.ConfirmarContinuacion());
        }
    }
}
