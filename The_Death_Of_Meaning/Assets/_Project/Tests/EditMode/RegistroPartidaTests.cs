using NUnit.Framework;
using TDOM.Gameplay;

namespace TDOM.Tests.EditMode
{
    public class RegistroPartidaTests
    {
        [Test]
        public void AcumulaMetricasPorJugadorSinDuplicarRegistros()
        {
            var registro = new RegistroPartida("2026-01-02T03:04:05.0000000Z");
            registro.ObtenerOCrearJugador(17, "Zendre");
            registro.RegistrarDanoHecho(17, 125.5f);
            registro.RegistrarDanoHecho(17, 20f);
            registro.RegistrarDanoRecibido(17, 33f);
            registro.RegistrarCaida(17);
            registro.RegistrarMuerte(17);
            registro.RegistrarMinionDerrotado();

            Assert.That(registro.Jugadores, Has.Count.EqualTo(1));
            Assert.That(registro.Jugadores[0].DanoHecho, Is.EqualTo(145.5f));
            Assert.That(registro.Jugadores[0].DanoRecibido, Is.EqualTo(33f));
            Assert.That(registro.Jugadores[0].VecesAbatido, Is.EqualTo(1));
            Assert.That(registro.Jugadores[0].Muertes, Is.EqualTo(1));
            Assert.That(registro.MinionsDerrotados, Is.EqualTo(1));
        }

        [Test]
        public void CerrarFijaResultadoYEvitaCambiosPosteriores()
        {
            var registro = new RegistroPartida();
            registro.ObtenerOCrearJugador(4, "Ayla");

            Assert.That(registro.Cerrar("Derrota", 91.7f, 3), Is.True);
            registro.RegistrarDanoHecho(4, 10f);
            registro.RegistrarMinionDerrotado();

            Assert.That(registro.Cerrada, Is.True);
            Assert.That(registro.Resultado, Is.EqualTo("Derrota"));
            Assert.That(registro.DuracionSegundos, Is.EqualTo(91.7f));
            Assert.That(registro.FaseAlcanzada, Is.EqualTo(3));
            Assert.That(registro.Jugadores[0].DanoHecho, Is.Zero);
            Assert.That(registro.MinionsDerrotados, Is.Zero);
            Assert.That(registro.Cerrar("Victoria", 100f, 5), Is.False);
            Assert.That(registro.Resultado, Is.EqualTo("Derrota"));
        }

        [Test]
        public void CadaPartidaTieneIdentidadYMetricasSeparadas()
        {
            var primera = new RegistroPartida();
            var segunda = new RegistroPartida();
            primera.RegistrarDanoHecho(8, 40f);

            Assert.That(primera.IdPartida, Is.Not.EqualTo(segunda.IdPartida));
            Assert.That(segunda.Jugadores, Is.Empty);
        }
    }
}
