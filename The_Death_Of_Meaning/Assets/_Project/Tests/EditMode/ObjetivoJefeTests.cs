using NUnit.Framework;
using TDOM.Gameplay.Combat;

namespace TDOM.Tests.EditMode
{
    public class ObjetivoJefeTests
    {
        private static (EstadoOjo, ColorEscudo) Abierto(ColorEscudo escudo = ColorEscudo.Ninguno) =>
            (EstadoOjo.Abierto, escudo);

        private static (EstadoOjo, ColorEscudo) Cerrado(ColorEscudo escudo = ColorEscudo.Ninguno) =>
            (EstadoOjo.Cerrado, escudo);

        [Test]
        public void ojos_abiertos_sin_escudo_piden_destruir_los_ojos()
        {
            Assert.AreEqual(
                ObjetivoJefe.DestruirOjos,
                ResolvedorObjetivoJefe.Resolver(new[] { Abierto(), Cerrado() })
            );
        }

        [Test]
        public void escudos_azules_piden_a_zendre()
        {
            Assert.AreEqual(
                ObjetivoJefe.RomperEscudosAzules,
                ResolvedorObjetivoJefe.Resolver(new[] { Abierto(ColorEscudo.Azul), Abierto() })
            );
        }

        [Test]
        public void escudos_rojos_piden_a_ayla()
        {
            Assert.AreEqual(
                ObjetivoJefe.RomperEscudosRojos,
                ResolvedorObjetivoJefe.Resolver(new[] { Abierto(ColorEscudo.Rojo) })
            );
        }

        [Test]
        public void escudos_de_ambos_colores_piden_a_los_dos()
        {
            Assert.AreEqual(
                ObjetivoJefe.RomperEscudosAmbos,
                ResolvedorObjetivoJefe.Resolver(
                    new[] { Abierto(ColorEscudo.Rojo), Abierto(ColorEscudo.Azul) }
                )
            );
        }

        [Test]
        public void los_ojos_cerrados_no_cuentan()
        {
            Assert.AreEqual(
                ObjetivoJefe.Ninguno,
                ResolvedorObjetivoJefe.Resolver(new[] { Cerrado(ColorEscudo.Azul), Cerrado() })
            );
        }

        [TestCase(ObjetivoJefe.DestruirOjos, true, InstruccionJugador.DestruirOjos)]
        [TestCase(ObjetivoJefe.DestruirOjos, false, InstruccionJugador.DestruirOjos)]
        [TestCase(ObjetivoJefe.RomperEscudosAzules, false, InstruccionJugador.RomperEscudosAzules)]
        [TestCase(ObjetivoJefe.RomperEscudosAzules, true, InstruccionJugador.ApoyarZendre)]
        [TestCase(ObjetivoJefe.RomperEscudosRojos, true, InstruccionJugador.RomperEscudosRojos)]
        [TestCase(ObjetivoJefe.RomperEscudosRojos, false, InstruccionJugador.ApoyarAyla)]
        [TestCase(ObjetivoJefe.RomperEscudosAmbos, true, InstruccionJugador.RomperEscudosRojos)]
        [TestCase(ObjetivoJefe.RomperEscudosAmbos, false, InstruccionJugador.RomperEscudosAzules)]
        [TestCase(ObjetivoJefe.Ninguno, true, InstruccionJugador.Ninguna)]
        public void cada_jugador_recibe_su_parte_del_objetivo(
            ObjetivoJefe objetivo,
            bool esAyla,
            InstruccionJugador esperada
        )
        {
            Assert.AreEqual(esperada, ResolvedorObjetivoJefe.ParaJugador(objetivo, esAyla));
        }

        [TestCase(true, false, InstruccionJugador.EsperarCompanero)]
        [TestCase(false, true, InstruccionJugador.AlcanzarCompanero)]
        [TestCase(false, false, InstruccionJugador.Ninguna)]
        [TestCase(true, true, InstruccionJugador.Ninguna)]
        public void quien_llego_espera_y_quien_falta_alcanza(
            bool yoLlegue,
            bool companeroLlego,
            InstruccionJugador esperada
        )
        {
            Assert.AreEqual(esperada, ResolvedorObjetivoJefe.ParaLlegada(yoLlegue, companeroLlego));
        }

        [Test]
        public void sin_ojos_no_hay_objetivo()
        {
            Assert.AreEqual(
                ObjetivoJefe.Ninguno,
                ResolvedorObjetivoJefe.Resolver(new (EstadoOjo, ColorEscudo)[0])
            );
        }
    }
}
