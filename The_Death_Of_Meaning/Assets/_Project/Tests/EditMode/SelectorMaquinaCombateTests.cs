using NUnit.Framework;
using TDOM.Contracts;
using TDOM.Gameplay.Combat;
using TDOM.Gameplay.Core;

namespace TDOM.Tests.EditMode
{
    public class SelectorMaquinaCombateTests
    {
        private const int FramesPorSegundo = 60;
        private const int FramesDePrueba = FramesPorSegundo;
        private const float DeltaTimeDeterminista = 1f / FramesPorSegundo;
        private const float TiempoWindup = 0.05f;
        private const float TiempoActivo = 0.05f;
        private const float VentanaCombo = 0.05f;
        private const float UmbralCarga = 0.25f;
        private const float CargaMaxima = 0.5f;
        private const float VentanaBuffer = 0.1f;

        [Test]
        public void Si_una_maquina_esta_activa_avanza_solo_esa()
        {
            var seleccionMelee = SelectorMaquinaCombate.Seleccionar(true, true, true, false);
            var seleccionDisparo = SelectorMaquinaCombate.Seleccionar(true, true, false, true);

            Assert.AreEqual(MaquinaCombateSeleccionada.Melee, seleccionMelee);
            Assert.AreEqual(MaquinaCombateSeleccionada.Disparo, seleccionDisparo);
        }

        [Test]
        public void Si_ambas_estan_en_idle_gana_melee()
        {
            var seleccion = SelectorMaquinaCombate.Seleccionar(true, true, false, false);

            Assert.AreEqual(MaquinaCombateSeleccionada.Melee, seleccion);
        }

        [Test]
        public void Si_una_maquina_no_esta_disponible_avanza_la_otra()
        {
            var seleccionMelee = SelectorMaquinaCombate.Seleccionar(true, false, false, false);
            var seleccionDisparo = SelectorMaquinaCombate.Seleccionar(false, true, false, false);

            Assert.AreEqual(MaquinaCombateSeleccionada.Melee, seleccionMelee);
            Assert.AreEqual(MaquinaCombateSeleccionada.Disparo, seleccionDisparo);
            Assert.AreEqual(
                MaquinaCombateSeleccionada.Ninguna,
                SelectorMaquinaCombate.Seleccionar(false, false, false, false)
            );
        }

        [Test]
        public void Con_dt_fijo_el_turno_es_determinista_y_no_activa_ambas_maquinas()
        {
            var fasesPrimeraSimulacion = SimularTurnos();
            var fasesSegundaSimulacion = SimularTurnos();

            CollectionAssert.AreEqual(fasesPrimeraSimulacion, fasesSegundaSimulacion);
        }

        private static ComboPhase[] SimularTurnos()
        {
            var melee = CrearMaquina();
            var disparo = CrearMaquina();
            var fases = new ComboPhase[FramesDePrueba];

            for (var frame = 0; frame < FramesDePrueba; frame++)
            {
                var seleccion = SelectorMaquinaCombate.Seleccionar(
                    true,
                    true,
                    melee.Fase != ComboPhase.Idle,
                    disparo.Fase != ComboPhase.Idle
                );
                var input = new InputSnapshot { AttackPressed = frame == 0 };

                if (seleccion == MaquinaCombateSeleccionada.Melee)
                    melee.Tick(input, DeltaTimeDeterminista);
                else if (seleccion == MaquinaCombateSeleccionada.Disparo)
                    disparo.Tick(input, DeltaTimeDeterminista);

                Assert.IsFalse(melee.Fase != ComboPhase.Idle && disparo.Fase != ComboPhase.Idle);
                fases[frame] = melee.Fase;
            }

            return fases;
        }

        private static ComboStateMachine CrearMaquina()
        {
            var pasoLigero = new AttackStep
            {
                WindupTime = TiempoWindup,
                ActiveTime = TiempoActivo,
                RecoveryTime = TiempoActivo,
                ComboWindowTime = VentanaCombo,
            };

            return new ComboStateMachine(
                new[] { pasoLigero },
                pasoLigero,
                UmbralCarga,
                CargaMaxima,
                VentanaBuffer
            );
        }
    }
}
