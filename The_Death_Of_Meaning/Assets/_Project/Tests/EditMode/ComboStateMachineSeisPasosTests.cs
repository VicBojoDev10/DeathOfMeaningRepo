using NUnit.Framework;
using TDOM.Contracts;
using TDOM.Gameplay.Combat;
using TDOM.Gameplay.Core;

namespace TDOM.Tests.EditMode
{
    public class ComboStateMachineSeisPasosTests
    {
        private static AttackStep Paso(float danio) =>
            new AttackStep
            {
                Damage = danio,
                ActiveTime = 0.1f,
                WindupTime = 0f,
                ComboWindowTime = 0.25f,
            };

        private ComboStateMachine CrearCombo() =>
            new ComboStateMachine(
                new[] { Paso(20), Paso(20), Paso(20), Paso(20), Paso(25), Paso(30) },
                new AttackStep
                {
                    Damage = 35f,
                    ActiveTime = 0.1f,
                    WindupTime = 0.3f,
                    ComboWindowTime = 0.3f,
                },
                0.2f,
                1f,
                0.15f,
                0.3f
            );

        [Test]
        public void Seis_taps_encadenados_producen_seis_golpes_en_orden_y_terminan_el_combo()
        {
            var combo = CrearCombo();
            var press = new InputSnapshot { AttackPressed = true };
            var neutral = new InputSnapshot();

            int golpes = 0;
            for (int i = 0; i < 6; i++)
            {
                var evento = combo.Tick(press, 0.12f);
                Assert.IsTrue(evento.HasValue, "golpe " + i);
                Assert.AreEqual(AttackKind.Light, evento.Value.Kind);
                Assert.AreEqual(i, evento.Value.ComboIndex);
                golpes++;
            }

            Assert.AreEqual(6, golpes);
            combo.Tick(neutral, 1f);
            Assert.AreEqual(ComboPhase.Idle, combo.Fase);
        }

        [Test]
        public void Un_cargado_tras_un_ligero_cuenta_un_ligero_previo()
        {
            var combo = CrearCombo();
            combo.Tick(new InputSnapshot { AttackPressed = true, AttackHeld = true }, 0.02f);
            combo.Tick(new InputSnapshot { AttackReleased = true }, 0.01f);
            combo.Tick(new InputSnapshot(), 0.12f);

            var press = new InputSnapshot { AttackPressed = true, AttackHeld = true };
            var hold = new InputSnapshot { AttackHeld = true };
            combo.Tick(press, 0.01f);
            AttackEvent? evento = null;
            for (int i = 0; i < 40 && !evento.HasValue; i++)
                evento = combo.Tick(hold, 0.05f);

            Assert.IsTrue(evento.HasValue);
            Assert.AreEqual(AttackKind.Charged, evento.Value.Kind);
            Assert.AreEqual(1, combo.LigerosEnCombo);
        }
    }
}
