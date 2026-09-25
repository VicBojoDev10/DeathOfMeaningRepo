using NUnit.Framework;
using TDOM.Contracts;
using TDOM.Gameplay.Combat;
using TDOM.POCO.ScriptableObjects;
using UnityEngine;

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
            Assert.AreEqual(BossAttackKind.Basico, boss.SiguienteAtaque());
            Assert.AreEqual(BossAttackKind.Basico, boss.SiguienteAtaque());
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
            var fase1 = ScriptableObject.CreateInstance<BossPhaseProfile>();
            fase1.Fase = 1;
            fase1.OrdenDeAtaque = new[]
            {
                BossAttackKind.Basico,
                BossAttackKind.Pesado,
                BossAttackKind.Basico,
                BossAttackKind.Basico,
            };
            fase1.VidaTransicion = 0.8f;

            var fase2 = ScriptableObject.CreateInstance<BossPhaseProfile>();
            fase2.Fase = 2;
            fase2.OrdenDeAtaque = new[]
            {
                BossAttackKind.Pesado,
                BossAttackKind.Basico,
                BossAttackKind.Especial,
                BossAttackKind.Pesado,
                BossAttackKind.Basico,
            };
            fase2.VidaTransicion = 0.6f;

            var fase3 = ScriptableObject.CreateInstance<BossPhaseProfile>();
            fase3.Fase = 3;
            fase3.OrdenDeAtaque = new[]
            {
                BossAttackKind.Basico,
                BossAttackKind.Pesado,
                BossAttackKind.Pesado,
                BossAttackKind.Basico,
                BossAttackKind.Especial,
            };
            fase3.VidaTransicion = 0.4f;

            var fase4 = ScriptableObject.CreateInstance<BossPhaseProfile>();
            fase4.Fase = 4;
            fase4.OrdenDeAtaque = new[]
            {
                BossAttackKind.Basico,
                BossAttackKind.Especial,
                BossAttackKind.Basico,
                BossAttackKind.Especial,
                BossAttackKind.Especial,
                BossAttackKind.Pesado,
                BossAttackKind.Pesado,
                BossAttackKind.InstaKill,
            };
            fase4.VidaTransicion = 0.2f;

            var fase5 = ScriptableObject.CreateInstance<BossPhaseProfile>();
            fase5.Fase = 5;
            fase5.OrdenDeAtaque = new[]
            {
                BossAttackKind.Basico,
                BossAttackKind.Basico,
                BossAttackKind.InstaKill,
                BossAttackKind.InstaKill,
                BossAttackKind.Especial,
                BossAttackKind.Pesado,
                BossAttackKind.Pesado,
                BossAttackKind.Especial,
                BossAttackKind.InstaKill,
                BossAttackKind.Especial,
                BossAttackKind.InstaKill,
                BossAttackKind.Especial,
            };
            fase5.VidaTransicion = 0.0f;

            return new[]
            {
                fase1,
                fase2,
                fase3,
                fase4,
                fase5
            };
        }
    }
}
