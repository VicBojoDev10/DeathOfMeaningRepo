
using NUnit.Framework;
using TDOM.Contracts;
using TDOM.Gameplay.Combat;
using TDOM.POCO.ScriptableObjects;

namespace TDOM.Tests.EditMode
{
    public class IBossPhaseStateTest 
    {
        [Test]
        public void Transiciones_de_fase_ocurren_en_umbral_correcto()
        {
            var fases = CrearFasesDePrueba();
            var boss = new BossPhaseStateMachine(fases);

           
            boss.AplicarDaño(0.19f);
            Assert.AreEqual(1, boss.FaseActual);


            boss.AplicarDaño(0.19f);
            Assert.AreEqual(2, boss.FaseActual);

            boss.AplicarDaño(0.19f);
            Assert.AreEqual(3, boss.FaseActual);

            boss.AplicarDaño(0.19f);
            Assert.AreEqual(4, boss.FaseActual);

            boss.AplicarDaño(0.19f);
            Assert.AreEqual(5, boss.FaseActual);
        }
        [Test]
        public void Orden_de_ataque_se_respeta_en_cada_fase()
        {
            var fases = CrearFasesDePrueba();
            var boss = new BossPhaseStateMachine(fases);

            Assert.AreEqual(BossAttackKind.Basico, boss.AtaqueActual);
            Assert.AreEqual(BossAttackKind.Pesado, boss.SiguienteAtaque());
        }

        [Test]
        public void La_maquina_es_determinista()
        {
            var fases = CrearFasesDePrueba();
            var boss1 = new BossPhaseStateMachine(fases);
            var boss2 = new BossPhaseStateMachine(fases);

            boss1.AplicarDaño(0.3f);
            boss2.AplicarDaño(0.3f);

            Assert.AreEqual(boss1.FaseActual, boss2.FaseActual);
            Assert.AreEqual(boss1.AtaqueActual, boss2.AtaqueActual);
        }


        private BossPhaseProfile[] CrearFasesDePrueba()
        {
            return new BossPhaseProfile[]
            {
        new BossPhaseProfile
        {
            Fase = 1,
            OrdenDeAtaque = new[] { BossAttackKind.Basico, BossAttackKind.Pesado, BossAttackKind.Basico, BossAttackKind.Basico },
            VidaTransicion = 0.8f
        },
        new BossPhaseProfile
        {
            Fase = 2,
            OrdenDeAtaque = new[] { BossAttackKind.Pesado, BossAttackKind.Basico, BossAttackKind.Especial, BossAttackKind.Pesado, BossAttackKind.Basico },
            VidaTransicion = 0.6f
        },
        new BossPhaseProfile
        {
            Fase = 3,
            OrdenDeAtaque = new[] { BossAttackKind.Basico, BossAttackKind.Pesado, BossAttackKind.Pesado, BossAttackKind.Basico, BossAttackKind.Especial },
            VidaTransicion = 0.4f
        },
        new BossPhaseProfile
        {
            Fase = 4,
            OrdenDeAtaque = new[] { BossAttackKind.Basico, BossAttackKind.Especial, BossAttackKind.Basico, BossAttackKind.Especial, BossAttackKind.Pesado, BossAttackKind.Pesado, BossAttackKind.InstaKill },
            VidaTransicion = 0.2f
        },
        new BossPhaseProfile
        {
            Fase = 5,
            OrdenDeAtaque = new[] { BossAttackKind.Basico, BossAttackKind.InstaKill, BossAttackKind.InstaKill, BossAttackKind.Especial, BossAttackKind.Pesado, BossAttackKind.Especial, BossAttackKind.InstaKill, BossAttackKind.Especial, BossAttackKind.InstaKill, BossAttackKind.Especial },
            VidaTransicion = 0.0f
        }
            };
        }

    }
}
