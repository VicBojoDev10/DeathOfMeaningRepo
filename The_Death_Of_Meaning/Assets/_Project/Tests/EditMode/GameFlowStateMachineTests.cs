using System.Collections.Generic;
using NUnit.Framework;
using TDOM.Gameplay.Core;

namespace TDOM.Tests.EditMode
{
    public class GameFlowStateMachineTests
    {
        private const float DuracionTransicion = 0.5f;
        private const float Dt60 = 1f / 60f;
        private const int TicksSinCompletar = 29;
        private const int TicksCompletos = 31;
        private const int LimiteDeTicks = 1000;
        private const string IdCinematica = "prueba";

        [Test]
        public void Empieza_en_gameplay_sin_cinematica_activa()
        {
            var flujo = new GameFlowStateMachine(DuracionTransicion);

            Assert.AreEqual(EstadoFlujo.Gameplay, flujo.Estado);
            Assert.IsNull(flujo.IdActual);
        }

        [Test]
        public void Iniciar_cinematica_desde_gameplay_pasa_a_cinematica()
        {
            var flujo = new GameFlowStateMachine(DuracionTransicion);

            bool iniciada = flujo.IniciarCinematica(IdCinematica);

            Assert.IsTrue(iniciada);
            Assert.AreEqual(EstadoFlujo.Cinematica, flujo.Estado);
            Assert.AreEqual(IdCinematica, flujo.IdActual);
        }

        [Test]
        public void No_se_puede_iniciar_una_cinematica_si_ya_hay_una_en_curso()
        {
            var flujo = new GameFlowStateMachine(DuracionTransicion);
            flujo.IniciarCinematica(IdCinematica);

            bool segunda = flujo.IniciarCinematica("otra");

            Assert.IsFalse(segunda);
            Assert.AreEqual(IdCinematica, flujo.IdActual);
        }

        [Test]
        public void Iniciar_cinematica_durante_la_transicion_devuelve_false()
        {
            var flujo = new GameFlowStateMachine(DuracionTransicion);
            flujo.IniciarCinematica(IdCinematica);
            flujo.TerminarCinematica();

            bool iniciada = flujo.IniciarCinematica("otra");

            Assert.IsFalse(iniciada);
            Assert.AreEqual(EstadoFlujo.Transicion, flujo.Estado);
        }

        [Test]
        public void Iniciar_cinematica_con_id_vacio_devuelve_false()
        {
            var flujo = new GameFlowStateMachine(DuracionTransicion);

            Assert.IsFalse(flujo.IniciarCinematica(string.Empty));
            Assert.IsFalse(flujo.IniciarCinematica(null));
            Assert.AreEqual(EstadoFlujo.Gameplay, flujo.Estado);
        }

        [Test]
        public void Terminar_cinematica_en_gameplay_no_cambia_el_estado()
        {
            var flujo = new GameFlowStateMachine(DuracionTransicion);

            flujo.TerminarCinematica();

            Assert.AreEqual(EstadoFlujo.Gameplay, flujo.Estado);
        }

        [Test]
        public void Tick_fuera_de_la_transicion_no_cambia_el_estado()
        {
            var flujo = new GameFlowStateMachine(DuracionTransicion);
            flujo.IniciarCinematica(IdCinematica);

            for (int i = 0; i < LimiteDeTicks; i++)
                flujo.Tick(Dt60);

            Assert.AreEqual(EstadoFlujo.Cinematica, flujo.Estado);
        }

        [Test]
        public void El_ciclo_es_gameplay_cinematica_transicion_gameplay()
        {
            var flujo = new GameFlowStateMachine(DuracionTransicion);
            var estados = new List<EstadoFlujo>();
            flujo.Cambio += estados.Add;

            flujo.IniciarCinematica(IdCinematica);
            flujo.TerminarCinematica();
            AvanzarHastaGameplay(flujo);

            CollectionAssert.AreEqual(
                new[] { EstadoFlujo.Cinematica, EstadoFlujo.Transicion, EstadoFlujo.Gameplay },
                estados
            );
            Assert.IsNull(flujo.IdActual);
        }

        [Test]
        public void La_transicion_dura_la_duracion_configurada()
        {
            var flujo = new GameFlowStateMachine(DuracionTransicion);
            flujo.IniciarCinematica(IdCinematica);
            flujo.TerminarCinematica();

            for (int i = 0; i < TicksSinCompletar; i++)
                flujo.Tick(Dt60);
            Assert.AreEqual(EstadoFlujo.Transicion, flujo.Estado);

            for (int i = TicksSinCompletar; i < TicksCompletos; i++)
                flujo.Tick(Dt60);
            Assert.AreEqual(EstadoFlujo.Gameplay, flujo.Estado);
        }

        [Test]
        public void La_transicion_es_determinista_con_dt_fijo()
        {
            int ticksPrimera = TicksHastaGameplay();
            int ticksSegunda = TicksHastaGameplay();

            Assert.AreEqual(ticksPrimera, ticksSegunda);
        }

        [Test]
        public void Se_puede_iniciar_otra_cinematica_al_volver_a_gameplay()
        {
            var flujo = new GameFlowStateMachine(DuracionTransicion);
            flujo.IniciarCinematica(IdCinematica);
            flujo.TerminarCinematica();
            AvanzarHastaGameplay(flujo);

            Assert.IsTrue(flujo.IniciarCinematica("otra"));
        }

        private static int TicksHastaGameplay()
        {
            var flujo = new GameFlowStateMachine(DuracionTransicion);
            flujo.IniciarCinematica(IdCinematica);
            flujo.TerminarCinematica();
            return AvanzarHastaGameplay(flujo);
        }

        private static int AvanzarHastaGameplay(GameFlowStateMachine flujo)
        {
            int ticks = 0;
            while (flujo.Estado != EstadoFlujo.Gameplay && ticks < LimiteDeTicks)
            {
                flujo.Tick(Dt60);
                ticks++;
            }
            return ticks;
        }
    }
}
