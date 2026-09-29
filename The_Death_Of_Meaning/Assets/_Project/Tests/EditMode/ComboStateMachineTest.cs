using NUnit.Framework;
using TDOM.Contracts;
using TDOM.Gameplay.Combat;
using TDOM.Gameplay.Core;

namespace TDOM.Tests.EditMode
{
    public class ComboStateMachineTests
    {
        private ComboStateMachine combo;
        private AttackStep[] pasos;
        private AttackStep cargado;

        [SetUp]
        public void Setup()
        {
            pasos = new AttackStep[]
            {
                new AttackStep
                {
                    Damage = 10f,
                    ActiveTime = 0.3f,
                    WindupTime = 0.2f,
                    ComboWindowTime = 0.25f,
                },
                new AttackStep
                {
                    Damage = 12f,
                    ActiveTime = 0.4f,
                    WindupTime = 0.25f,
                    ComboWindowTime = 0.3f,
                },
                new AttackStep
                {
                    Damage = 15f,
                    ActiveTime = 0.5f,
                    WindupTime = 0.3f,
                    ComboWindowTime = 0.35f,
                },
            };

            cargado = new AttackStep
            {
                Damage = 25f,
                ActiveTime = 0.6f,
                WindupTime = 0.3f,
                ComboWindowTime = 0.3f,
            };

            combo = new ComboStateMachine(pasos, cargado, 0.15f, 1.2f, 0.20f);
        }

        [Test]
        public void Tres_taps_producen_tres_golpes_ligeros_en_Ayla()
        {
            var press = new InputSnapshot { AttackPressed = true };

            var golpe1 = combo.Tick(press, 0.21f); // Windup (0.2s) termina -> Swing paso 0
            var golpe2 = combo.Tick(press, 0.10f); // press en Swing -> encadena paso 1
            var golpe3 = combo.Tick(press, 0.10f); // press en Swing -> encadena paso 2

            Assert.IsTrue(golpe1.HasValue, "falta el golpe 1");
            Assert.IsTrue(golpe2.HasValue, "falta el golpe 2");
            Assert.IsTrue(golpe3.HasValue, "falta el golpe 3");

            Assert.AreEqual(AttackKind.Light, golpe1.Value.Kind);
            Assert.AreEqual(AttackKind.Light, golpe2.Value.Kind);
            Assert.AreEqual(AttackKind.Light, golpe3.Value.Kind);

            Assert.AreEqual(0, golpe1.Value.ComboIndex);
            Assert.AreEqual(1, golpe2.Value.ComboIndex);
            Assert.AreEqual(2, golpe3.Value.ComboIndex);

            Assert.AreEqual(10f, golpe1.Value.Damage);
            Assert.AreEqual(12f, golpe2.Value.Damage);
            Assert.AreEqual(15f, golpe3.Value.Damage);

            Assert.AreEqual(ComboPhase.Swing, combo.Fase);
        }

        [Test]
        public void Dos_taps_producen_dos_golpes_ligeros_en_Zendre()
        {
            var input = new InputSnapshot { AttackPressed = true };
            combo.Tick(input, 0.25f);
            combo.Tick(input, 0.25f);
            Assert.AreEqual(ComboPhase.Swing, combo.Fase);
        }

        [Test]
        public void Hold_desde_neutral_produce_cargado_puro_sin_ligero_previo()
        {
            var input = new InputSnapshot { AttackHeld = true, AttackPressed = true };
            combo.Tick(input, 0.3f);
            combo.Tick(input, 1.2f);
            Assert.AreEqual(ComboPhase.ChargedSwing, combo.Fase);
        }

        [Test]
        public void Tap_tap_hold_produce_dos_ligeros_y_un_cargado()
        {
            var press = new InputSnapshot { AttackPressed = true, AttackHeld = true };
            var hold = new InputSnapshot { AttackHeld = true };
            var release = new InputSnapshot { AttackReleased = true };
            var neutral = new InputSnapshot();

            // Tap 1: press corto; al acabar el Windup (0.2s) ya no hay hold -> Swing paso 0
            combo.Tick(press, 0.05f);
            combo.Tick(release, 0.016f);
            var ligero1 = combo.Tick(neutral, 0.20f);
            Assert.AreEqual(ComboPhase.Swing, combo.Fase);

            combo.Tick(neutral, 0.30f); // el Swing termina sin press
            Assert.AreEqual(ComboPhase.ComboWindow, combo.Fase);

            // Tap 2: press dentro de la ventana -> Windup paso 1 -> Swing paso 1
            combo.Tick(press, 0.05f);
            combo.Tick(release, 0.016f);
            var ligero2 = combo.Tick(neutral, 0.25f);
            Assert.AreEqual(ComboPhase.Swing, combo.Fase);

            combo.Tick(neutral, 0.40f);
            Assert.AreEqual(ComboPhase.ComboWindow, combo.Fase);

            // Hold: press que se mantiene; al acabar el Windup (0.3s) sigue held -> Charging
            combo.Tick(press, 0.05f);
            combo.Tick(hold, 0.30f);
            Assert.AreEqual(ComboPhase.Charging, combo.Fase);

            combo.Tick(hold, 0.50f);
            var cargado = combo.Tick(release, 0.016f);

            // Los tres golpes existen y son los correctos
            Assert.IsTrue(ligero1.HasValue, "falta el ligero 1");
            Assert.IsTrue(ligero2.HasValue, "falta el ligero 2");
            Assert.IsTrue(cargado.HasValue, "falta el cargado");

            Assert.AreEqual(AttackKind.Light, ligero1.Value.Kind);
            Assert.AreEqual(0, ligero1.Value.ComboIndex);
            Assert.AreEqual(10f, ligero1.Value.Damage);

            Assert.AreEqual(AttackKind.Light, ligero2.Value.Kind);
            Assert.AreEqual(1, ligero2.Value.ComboIndex);
            Assert.AreEqual(12f, ligero2.Value.Damage);

            Assert.AreEqual(AttackKind.Charged, cargado.Value.Kind);
            Assert.AreEqual(2, cargado.Value.ComboIndex);
            Assert.AreEqual(25f, cargado.Value.Damage);
            Assert.That(cargado.Value.ChargeRatio, Is.InRange(0.01f, 0.99f));

            Assert.AreEqual(ComboPhase.ChargedSwing, combo.Fase);
        }

        [Test]
        public void El_cargado_siempre_termina_el_combo()
        {
            var input = new InputSnapshot { AttackHeld = true, AttackPressed = true };
            combo.Tick(input, 1.2f);
            combo.Tick(input, 0.6f);
            Assert.AreEqual(ComboPhase.Idle, combo.Fase);
        }

        [Test]
        public void Al_llegar_al_ultimo_paso_el_combo_termina()
        {
            var press = new InputSnapshot { AttackPressed = true };

            for (int i = 0; i < pasos.Length; i++)
                combo.Tick(press, 0.3f);

            Assert.AreEqual(ComboPhase.Swing, combo.Fase);

            combo.Tick(new InputSnapshot(), 2.0f);

            Assert.AreEqual(ComboPhase.Idle, combo.Fase);
        }

        [Test]
        public void La_ventana_de_combo_expira_y_reinicia_el_indice()
        {
            var input = new InputSnapshot { AttackPressed = true };
            combo.Tick(input, 0.3f);
            combo.Tick(new InputSnapshot(), 1.0f);
            Assert.AreEqual(ComboPhase.Idle, combo.Fase);
        }

        [Test]
        public void Un_press_en_frames_activos_encadena_el_siguiente_golpe()
        {
            var press = new InputSnapshot { AttackPressed = true };

            var golpe1 = combo.Tick(press, 0.21f);
            Assert.AreEqual(ComboPhase.Swing, combo.Fase);

            var golpe2 = combo.Tick(press, 0.10f);
            Assert.AreEqual(ComboPhase.Swing, combo.Fase);

            Assert.IsTrue(golpe1.HasValue);
            Assert.IsTrue(golpe2.HasValue);
            Assert.AreEqual(AttackKind.Light, golpe1.Value.Kind);
            Assert.AreEqual(AttackKind.Light, golpe2.Value.Kind);
            Assert.AreEqual(0, golpe1.Value.ComboIndex);
            Assert.AreEqual(1, golpe2.Value.ComboIndex);
            Assert.AreEqual(10f, golpe1.Value.Damage);
            Assert.AreEqual(12f, golpe2.Value.Damage);
        }

        [Test]
        public void La_carga_se_libera_sola_al_llegar_al_maximo()
        {
            var input = new InputSnapshot { AttackHeld = true, AttackPressed = true };
            combo.Tick(input, 1.3f);
            Assert.AreEqual(ComboPhase.ChargedSwing, combo.Fase);
        }

        [Test]
        public void El_ChargeRatio_va_de_cero_a_uno()
        {
            // Press + hold parcial + release: el ratio debe quedar entre 0 y 1
            combo.Tick(new InputSnapshot { AttackPressed = true, AttackHeld = true }, 0.05f);
            combo.Tick(new InputSnapshot { AttackHeld = true }, 0.60f);
            var evento = combo.Tick(new InputSnapshot { AttackReleased = true }, 0.01f);

            Assert.IsTrue(evento.HasValue);
            Assert.AreEqual(AttackKind.Charged, evento.Value.Kind);
            Assert.That(evento.Value.ChargeRatio, Is.InRange(0.01f, 0.99f));
            Assert.AreEqual(ComboPhase.ChargedSwing, combo.Fase);
        }
    }
}
