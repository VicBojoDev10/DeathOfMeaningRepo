using System.Collections.Generic;
using NUnit.Framework;
using TDOM.Contracts;
using TDOM.Gameplay.Combat;

namespace TDOM.Tests.EditMode
{
    public class ComboStateMachineDisparoContinuoTests
    {
        private const float DeltaTimeDeterminista = 1f / 60f;
        private const int Toques = 9;
        private const int FramesEntreToques = 7;
        private const int FramesPresionado = 3;
        private const int FramesDeSobra = 60;

        [Test]
        public void cada_toque_rapido_produce_un_disparo_aunque_el_combo_se_complete()
        {
            Assert.AreEqual(Toques, Simular(encadenar: true).Count);
        }

        [Test]
        public void sin_encadenar_se_pierden_toques_al_completar_el_combo()
        {
            Assert.Less(Simular(encadenar: false).Count, Toques);
        }

        [Test]
        public void los_disparos_continuos_alternan_los_pasos_del_combo()
        {
            List<AttackEvent> eventos = Simular(encadenar: true);

            for (int i = 0; i < eventos.Count; i++)
                Assert.AreEqual(i % 2, eventos[i].ComboIndex);
        }

        [Test]
        public void con_dt_fijo_dos_simulaciones_producen_los_mismos_disparos()
        {
            CollectionAssert.AreEqual(Simular(encadenar: true), Simular(encadenar: true));
        }

        private static List<AttackEvent> Simular(bool encadenar)
        {
            var paso = new AttackStep
            {
                WindupTime = 0.05f,
                ActiveTime = 0.05f,
                ComboWindowTime = 0.05f,
                Damage = 12f,
            };
            var maquina = new ComboStateMachine(
                new[] { paso, paso },
                paso,
                umbralHold: 0.15f,
                cargaMaxima: 1f,
                bufferWindow: 0.15f,
                encadenarAlTerminar: encadenar
            );
            var eventos = new List<AttackEvent>();
            int framesConToques = Toques * FramesEntreToques;

            for (int frame = 0; frame < framesConToques + FramesDeSobra; frame++)
            {
                bool hayToque = frame < framesConToques;
                int frameDelToque = frame % FramesEntreToques;
                var input = new InputSnapshot
                {
                    AttackPressed = hayToque && frameDelToque == 0,
                    AttackHeld = hayToque && frameDelToque < FramesPresionado,
                    AttackReleased = hayToque && frameDelToque == FramesPresionado,
                };
                AttackEvent? evento = maquina.Tick(input, DeltaTimeDeterminista);
                if (evento.HasValue)
                    eventos.Add(evento.Value);
            }

            return eventos;
        }
    }
}
