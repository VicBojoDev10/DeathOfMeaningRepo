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
        public void melee_y_disparo_en_el_mismo_frame_solo_arranca_el_melee()
        {
            var melee = CrearMaquina();
            var disparo = CrearMaquina();

            AvanzarMaquinas(
                melee,
                disparo,
                new InputSnapshot { AttackPressed = true, FirePressed = true }
            );

            Assert.AreNotEqual(ComboPhase.Idle, melee.Fase);
            Assert.AreEqual(ComboPhase.Idle, disparo.Fase);
        }

        [Test]
        public void con_el_disparo_activo_el_melee_no_avanza()
        {
            var melee = CrearMaquina();
            var disparo = CrearMaquina();

            AvanzarMaquinas(null, disparo, new InputSnapshot { FirePressed = true });
            AvanzarMaquinas(
                melee,
                disparo,
                new InputSnapshot { AttackPressed = true, FirePressed = true }
            );

            Assert.AreEqual(ComboPhase.Idle, melee.Fase);
            Assert.AreNotEqual(ComboPhase.Idle, disparo.Fase);
        }

        [Test]
        public void mantener_melee_y_disparo_nunca_deja_las_dos_maquinas_activas()
        {
            RegistroFases primeraSimulacion = SimularAmbosBotonesSostenidos();
            RegistroFases segundaSimulacion = SimularAmbosBotonesSostenidos();

            CollectionAssert.AreEqual(primeraSimulacion.Melee, segundaSimulacion.Melee);
            CollectionAssert.AreEqual(primeraSimulacion.Disparo, segundaSimulacion.Disparo);
        }

        [Test]
        public void al_terminar_el_melee_con_r2_sostenido_arranca_el_disparo()
        {
            var melee = CrearMaquina();
            var disparo = CrearMaquina();
            bool disparoInicioDespuesDelMelee = false;

            for (int frame = 0; frame < FramesDePrueba; frame++)
            {
                AvanzarMaquinas(
                    melee,
                    disparo,
                    new InputSnapshot { AttackPressed = frame == 0, FireHeld = true }
                );

                if (melee.Fase == ComboPhase.Idle && disparo.Fase != ComboPhase.Idle)
                {
                    disparoInicioDespuesDelMelee = true;
                    break;
                }
            }

            Assert.IsTrue(disparoInicioDespuesDelMelee);
        }

        private static RegistroFases SimularAmbosBotonesSostenidos()
        {
            var melee = CrearMaquina();
            var disparo = CrearMaquina();
            var fasesMelee = new ComboPhase[FramesDePrueba];
            var fasesDisparo = new ComboPhase[FramesDePrueba];

            for (int frame = 0; frame < FramesDePrueba; frame++)
            {
                AvanzarMaquinas(
                    melee,
                    disparo,
                    new InputSnapshot
                    {
                        AttackPressed = frame == 0,
                        AttackHeld = true,
                        FirePressed = frame == 0,
                        FireHeld = true,
                    }
                );

                fasesMelee[frame] = melee.Fase;
                fasesDisparo[frame] = disparo.Fase;
                Assert.IsFalse(melee.Fase != ComboPhase.Idle && disparo.Fase != ComboPhase.Idle);
            }

            return new RegistroFases(fasesMelee, fasesDisparo);
        }

        private static MaquinaCombateSeleccionada AvanzarMaquinas(
            ComboStateMachine melee,
            ComboStateMachine disparo,
            InputSnapshot input
        )
        {
            var seleccion = SelectorMaquinaCombate.Seleccionar(
                melee != null,
                disparo != null,
                melee != null && melee.Fase != ComboPhase.Idle,
                disparo != null && disparo.Fase != ComboPhase.Idle,
                input.AttackPressed || input.AttackHeld,
                input.FirePressed || input.FireHeld
            );

            if (seleccion == MaquinaCombateSeleccionada.Melee)
                melee?.Tick(input, DeltaTimeDeterminista);
            else if (seleccion == MaquinaCombateSeleccionada.Disparo)
                disparo?.Tick(ComoDisparo(input), DeltaTimeDeterminista);

            return seleccion;
        }

        private static InputSnapshot ComoDisparo(InputSnapshot input) =>
            new InputSnapshot
            {
                AttackPressed = input.FirePressed,
                AttackHeld = input.FireHeld,
                AttackReleased = input.FireReleased,
            };

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

        private sealed class RegistroFases
        {
            public ComboPhase[] Melee { get; }
            public ComboPhase[] Disparo { get; }

            public RegistroFases(ComboPhase[] melee, ComboPhase[] disparo)
            {
                Melee = melee;
                Disparo = disparo;
            }
        }
    }
}
