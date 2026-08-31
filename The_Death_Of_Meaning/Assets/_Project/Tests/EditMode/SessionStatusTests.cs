using NUnit.Framework;

namespace TDOM.Tests.EditMode
{
    public sealed class SessionStatusTests
    {
        public EstadoSessionTest EstadoTest{ get; private set; }
        public string MensajeTest { get; private set; }
        public void SessionStatusTest(EstadoSessionTest estadoTest, string mensajeTest = "")
        {
            EstadoTest = estadoTest;
            MensajeTest = mensajeTest;
        }
        [Test]
        public void OncrearPartida()
        {
            SessionStatusTest((EstadoSessionTest.ConectandoTest));
        }
        [Test]
        public void OnDesconectadoTest()
        {
            SessionStatusTest((EstadoSessionTest.DesconectadoTest));
        }
        [Test]
        public void EsperandoJugadorTest()
        {
            SessionStatusTest((EstadoSessionTest.EsperandoJugadorTest));
        }
        [Test]
        public void ErrorTest()
        {
            SessionStatusTest((EstadoSessionTest.ErrorTest));
        }
    }

    public enum EstadoSessionTest
    {
        DesconectadoTest,
        ConectandoTest,
        EsperandoJugadorTest,
        ListoTest,
        ErrorTest,
    }
}
