using NUnit.Framework;
using TDOM.Contracts;
using TDOM.Gameplay.Combat;
using TDOM.Gameplay.Core;

namespace TDOM.Tests.EditMode
{
    public class ComboStateMachineEsperaFinalTests
    {
        private ComboStateMachine combo;
        private bool animando;

        private static readonly InputSnapshot Press = new InputSnapshot
        {
            AttackPressed = true,
            AttackHeld = true,
        };
        private static readonly InputSnapshot Release = new InputSnapshot { AttackReleased = true };
        private static readonly InputSnapshot Neutral = new InputSnapshot();

        [SetUp]
        public void Setup()
        {
            var pasos = new[]
            {
                new AttackStep
                {
                    Damage = 10f,
                    ActiveTime = 0.1f,
                    WindupTime = 0.05f,
                    ComboWindowTime = 0.2f,
                },
                new AttackStep
                {
                    Damage = 12f,
                    ActiveTime = 0.1f,
                    WindupTime = 0.05f,
                    ComboWindowTime = 0.2f,
                },
            };
            var cargado = new AttackStep
            {
                Damage = 25f,
                ActiveTime = 0.1f,
                WindupTime = 0.1f,
                ComboWindowTime = 0.1f,
            };
            combo = new ComboStateMachine(pasos, cargado, 0.15f, 1f, 0.15f, 0.1f);
            animando = false;
            combo.EsperarAnimacionFinal = () => animando;
        }

        private void TerminarCargadoMantenido()
        {
            var primero = combo.Tick(Press, 1.2f); // carga al maximo: sale solo
            Assert.AreEqual(AttackKind.Charged, primero.Value.Kind);
            combo.Tick(Neutral, 0.5f); // termina el swing/recovery
            Assert.AreEqual(ComboPhase.Idle, combo.Fase);
        }

        [Test]
        public void Tras_un_cargado_no_se_aceptan_golpes_mientras_la_animacion_sigue()
        {
            animando = true;
            TerminarCargadoMantenido();
            Assert.IsTrue(combo.EsperandoAnimacionFinal);

            for (int i = 0; i < 20; i++)
            {
                Assert.IsFalse(combo.Tick(Release, 0.05f).HasValue);
                Assert.IsFalse(combo.Tick(Press, 0.05f).HasValue);
            }
            Assert.AreEqual(ComboPhase.Idle, combo.Fase);
        }

        [Test]
        public void Al_terminar_la_animacion_hay_que_soltar_y_volver_a_presionar()
        {
            animando = true;
            TerminarCargadoMantenido();
            combo.Tick(Press, 0.05f);

            animando = false;
            Assert.IsFalse(combo.Tick(Press, 0.05f).HasValue, "boton aun apretado: no cuenta");
            Assert.IsFalse(combo.EsperandoAnimacionFinal);

            combo.Tick(Release, 0.05f);
            combo.Tick(Neutral, 0.15f); // pasa el cooldown
            Assert.IsTrue(combo.Tick(Press, 1.2f).HasValue);
        }

        [Test]
        public void Los_presses_durante_la_espera_no_quedan_en_el_buffer()
        {
            animando = true;
            TerminarCargadoMantenido();
            combo.Tick(Press, 0.02f);
            combo.Tick(Release, 0.02f);

            animando = false;
            combo.Tick(Neutral, 0.5f);
            Assert.AreEqual(ComboPhase.Idle, combo.Fase);
        }

        [Test]
        public void Tras_un_combo_completo_tambien_se_espera_la_animacion()
        {
            animando = true;
            combo.Tick(Press, 0.2f); // ligero 0
            combo.Tick(Release, 0.01f);
            combo.Tick(Neutral, 0.05f);
            combo.Tick(Press, 0.2f); // ligero 1 (ultimo)
            combo.Tick(Release, 0.01f);
            combo.Tick(Neutral, 1f); // el combo termina
            Assert.AreEqual(ComboPhase.Idle, combo.Fase);
            Assert.IsTrue(combo.EsperandoAnimacionFinal);

            combo.Tick(Neutral, 0.3f);
            Assert.IsFalse(combo.Tick(Press, 0.3f).HasValue);
        }

        [Test]
        public void Un_seguro_de_tiempo_evita_quedarse_bloqueado()
        {
            combo.MaxEsperaAnimacionFinal = 0.5f;
            animando = true; // nunca termina
            TerminarCargadoMantenido();

            for (int i = 0; i < 20; i++)
                combo.Tick(Neutral, 0.05f);
            Assert.IsFalse(combo.EsperandoAnimacionFinal);
            Assert.IsTrue(combo.Tick(Press, 1.2f).HasValue);
        }

        [Test]
        public void Cancelar_libera_la_espera()
        {
            animando = true;
            TerminarCargadoMantenido();
            Assert.IsTrue(combo.EsperandoAnimacionFinal);

            combo.Cancelar();

            Assert.IsFalse(combo.EsperandoAnimacionFinal);
        }

        [Test]
        public void Un_cargado_normal_deja_el_cooldown_de_fin_de_combo()
        {
            animando = false;
            var primero = combo.Tick(Press, 1.02f);
            Assert.AreEqual(AttackKind.Charged, primero.Value.Kind);
            combo.Tick(Neutral, 0.2f); // termina el cargado

            Assert.AreEqual(ComboPhase.Idle, combo.Fase);
            Assert.IsTrue(combo.EnCooldown, "el cargado tambien deja cooldown");
            combo.Tick(Neutral, 0.5f);
            Assert.IsFalse(combo.EnCooldown);
        }

        [Test]
        public void Sin_hook_el_comportamiento_no_cambia()
        {
            combo.EsperarAnimacionFinal = null;
            TerminarCargadoMantenido();
            Assert.IsFalse(combo.EsperandoAnimacionFinal);
        }
    }
}
