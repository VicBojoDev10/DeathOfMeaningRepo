using NUnit.Framework;
using TDOM.Contracts;
using TDOM.Gameplay.Combat;
using TDOM.Gameplay.Core;

namespace TDOM.Tests.EditMode
{
    public class ComboStateMachineCargadoRetenidoTests
    {
        private ComboStateMachine combo;

        [SetUp]
        public void Setup()
        {
            var pasos = new[]
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
            var cargado = new AttackStep
            {
                Damage = 25f,
                ActiveTime = 0.1f,
                WindupTime = 0.3f,
                ComboWindowTime = 0.3f,
            };
            combo = new ComboStateMachine(pasos, cargado, 0.15f, 1.2f, 0.20f, 0.3f);
        }

        // tap, tap, hold + release: ligero 0, ligero 1 y cargado (ComboIndex 2, 2 ligeros previos).
        private AttackEvent? TapTapHold()
        {
            var press = new InputSnapshot { AttackPressed = true, AttackHeld = true };
            var hold = new InputSnapshot { AttackHeld = true };
            var release = new InputSnapshot { AttackReleased = true };
            var neutral = new InputSnapshot();

            combo.Tick(press, 0.05f);
            combo.Tick(release, 0.016f);
            combo.Tick(neutral, 0.20f);
            combo.Tick(neutral, 0.30f);

            combo.Tick(press, 0.05f);
            combo.Tick(release, 0.016f);
            combo.Tick(neutral, 0.25f);
            combo.Tick(neutral, 0.40f);

            combo.Tick(press, 0.05f);
            combo.Tick(hold, 0.30f);
            combo.Tick(hold, 0.50f);
            return combo.Tick(release, 0.016f);
        }

        [Test]
        public void Sin_predicado_el_cargado_dura_su_ActiveTime_como_siempre()
        {
            var evento = TapTapHold();
            Assert.AreEqual(2, evento.Value.ComboIndex);
            Assert.AreEqual(ComboPhase.ChargedSwing, combo.Fase);
            Assert.IsFalse(combo.CargadoRetenido);

            combo.Tick(new InputSnapshot(), 0.2f);
            Assert.AreEqual(ComboPhase.Idle, combo.Fase);
        }

        [Test]
        public void Un_cargado_retenido_bloquea_hasta_terminarlo()
        {
            combo.RetenerCargadoSi = ligeros => ligeros == 2;
            var evento = TapTapHold();

            Assert.AreEqual(AttackKind.Charged, evento.Value.Kind);
            Assert.IsTrue(combo.CargadoRetenido);

            for (int i = 0; i < 100; i++)
                combo.Tick(new InputSnapshot(), 0.1f);

            Assert.AreEqual(ComboPhase.ChargedSwing, combo.Fase);
            Assert.IsTrue(combo.BloqueaMovimiento);

            combo.TerminarCargado();

            Assert.AreEqual(ComboPhase.Idle, combo.Fase);
            Assert.IsFalse(combo.CargadoRetenido);
            Assert.IsTrue(combo.EnCooldown, "cuenta como fin de combo");
        }

        [Test]
        public void El_predicado_solo_retiene_la_cantidad_de_ligeros_que_dice()
        {
            combo.RetenerCargadoSi = ligeros => ligeros == 1;
            TapTapHold();

            Assert.IsFalse(combo.CargadoRetenido);
            combo.Tick(new InputSnapshot(), 0.2f);
            Assert.AreEqual(ComboPhase.Idle, combo.Fase);
        }

        [Test]
        public void Los_presses_durante_un_cargado_retenido_se_ignoran()
        {
            combo.RetenerCargadoSi = ligeros => ligeros == 2;
            TapTapHold();

            var press = new InputSnapshot { AttackPressed = true, AttackHeld = true };
            for (int i = 0; i < 10; i++)
            {
                var evento = combo.Tick(press, 0.1f);
                Assert.IsFalse(evento.HasValue);
            }
            Assert.AreEqual(ComboPhase.ChargedSwing, combo.Fase);
        }

        [Test]
        public void Cancelar_libera_un_cargado_retenido()
        {
            combo.RetenerCargadoSi = ligeros => ligeros == 2;
            TapTapHold();

            combo.Cancelar();

            Assert.AreEqual(ComboPhase.Idle, combo.Fase);
            Assert.IsFalse(combo.CargadoRetenido);
        }

        [Test]
        public void Empezar_a_cargar_durante_el_Swing_tambien_cuenta_dos_ligeros()
        {
            combo.RetenerCargadoSi = ligeros => ligeros == 2;
            var tap = new InputSnapshot { AttackPressed = true };
            var pressHold = new InputSnapshot { AttackPressed = true, AttackHeld = true };
            var hold = new InputSnapshot { AttackHeld = true };
            var release = new InputSnapshot { AttackReleased = true };

            combo.Tick(tap, 0.21f); // ligero 0 (Swing)
            combo.Tick(tap, 0.05f); // ligero 1 encadenado dentro del Swing
            combo.Tick(pressHold, 0.01f); // press que se mantiene en pleno Swing: empieza la carga
            combo.Tick(hold, 0.60f);
            var evento = combo.Tick(release, 0.016f);

            Assert.AreEqual(AttackKind.Charged, evento.Value.Kind);
            Assert.AreEqual(1, evento.Value.ComboIndex, "por este camino el indice es 1, no 2");
            Assert.IsTrue(combo.CargadoRetenido, "pero siguen siendo dos ligeros previos");
        }

        [Test]
        public void Un_cargado_puro_o_tras_un_ligero_no_se_retiene_para_los_giros()
        {
            combo.RetenerCargadoSi = ligeros => ligeros == 2;
            combo.Tick(new InputSnapshot { AttackPressed = true, AttackHeld = true }, 0.05f);
            combo.Tick(new InputSnapshot { AttackHeld = true }, 0.60f);
            var evento = combo.Tick(new InputSnapshot { AttackReleased = true }, 0.01f);

            Assert.AreEqual(AttackKind.Charged, evento.Value.Kind);
            Assert.IsFalse(combo.CargadoRetenido);
        }

        [Test]
        public void TerminarCargado_sin_retencion_no_hace_nada()
        {
            combo.Tick(new InputSnapshot { AttackPressed = true }, 0.21f);
            var fase = combo.Fase;

            combo.TerminarCargado();

            Assert.AreEqual(fase, combo.Fase);
            Assert.IsFalse(combo.EnCooldown);
        }
    }
}
