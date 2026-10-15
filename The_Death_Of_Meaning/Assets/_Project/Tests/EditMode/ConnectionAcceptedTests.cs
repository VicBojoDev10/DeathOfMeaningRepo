using NUnit.Framework;
using TDOM.Gameplay;

namespace TDOM.Tests.EditMode
{
    public class ConnectionAcceptedTests
    {
        [Test]
        public void con_cero_o_un_jugador_se_acepta_la_conexion()
        {
            Assert.That(ConnectionAccepted.PuedeAceptarConexion(0, false), Is.True);
            Assert.That(ConnectionAccepted.PuedeAceptarConexion(1, false), Is.True);
        }

        [Test]
        public void con_dos_jugadores_se_rechaza_la_conexion()
        {
            Assert.That(ConnectionAccepted.PuedeAceptarConexion(2, false), Is.False);
        }

        [Test]
        public void con_la_partida_iniciada_se_rechaza_la_conexion()
        {
            Assert.That(ConnectionAccepted.PuedeAceptarConexion(0, true), Is.False);
        }
    }
}
