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
    // Umbral 0.8 -> Fase 2
    {
        var boss = new BossPhaseStateMachine(CrearFasesDePrueba());

        boss.AplicarDaño(0.1999f);
        Assert.AreEqual(1, boss.FaseActual);

        boss.AplicarDaño(0.0002f);
        Assert.AreEqual(2, boss.FaseActual);
    }

    // Umbral 0.6 -> Fase 3
    {
        var boss = new BossPhaseStateMachine(CrearFasesDePrueba());

        boss.AplicarDaño(0.2001f);
        Assert.AreEqual(2, boss.FaseActual);

        boss.AplicarDaño(0.1998f);
        Assert.AreEqual(2, boss.FaseActual);

        boss.AplicarDaño(0.0002f);
        Assert.AreEqual(3, boss.FaseActual);
    }

    // Umbral 0.4 -> Fase 4
    {
        {
            var boss = new BossPhaseStateMachine(CrearFasesDePrueba());

            boss.AplicarDaño(0.2001f);
            Assert.AreEqual(2, boss.FaseActual);

            boss.AplicarDaño(0.001f);
            Assert.AreEqual(2, boss.FaseActual);

            boss.AplicarDaño(0.1991f);
            Assert.AreEqual(3, boss.FaseActual);

            boss.AplicarDaño(0.198f);
            Assert.AreEqual(3, boss.FaseActual);

            boss.AplicarDaño(0.003f);
            Assert.AreEqual(4, boss.FaseActual);
        }

    }

    // Umbral 0.2 -> Fase 5
    {
        var boss = new BossPhaseStateMachine(CrearFasesDePrueba());

        // Llegar a fase 4 progresivamente
        boss.AplicarDaño(0.2001f);
        Assert.AreEqual(2, boss.FaseActual);

        boss.AplicarDaño(0.2001f);
        Assert.AreEqual(3, boss.FaseActual);

        boss.AplicarDaño(0.2001f);
        Assert.AreEqual(4, boss.FaseActual);

        // Quedarse justo arriba de 0.2
        boss.AplicarDaño(0.1996f);
        Assert.AreEqual(4, boss.FaseActual);

        // Cruzar el umbral
        boss.AplicarDaño(0.0005f);
        Assert.AreEqual(5, boss.FaseActual);
    }
}

        [Test]
        public void Un_golpe_grande_no_debe_saltar_varias_fases()
        {
            var fases = CrearFasesDePrueba();
            var boss = new BossPhaseStateMachine(fases);

            boss.AplicarDaño(0.9f);

            Assert.AreEqual(2, boss.FaseActual);
        }


        [Test]
        public void Orden_de_ataque_se_respeta_en_cada_fase()
        {
            var fases = CrearFasesDePrueba();
            var boss = new BossPhaseStateMachine(fases);

            Assert.AreEqual(BossAttackKind.Basico, boss.AtaqueActual.Tipo);
            Assert.AreEqual(BossAttackKind.Pesado, boss.SiguienteAtaque().Tipo);
            Assert.AreEqual(BossAttackKind.Basico, boss.SiguienteAtaque().Tipo);
            Assert.AreEqual(BossAttackKind.Basico, boss.SiguienteAtaque().Tipo);

            boss.AplicarDaño(0.205f);

            Assert.AreEqual(2, boss.FaseActual);

            Assert.AreEqual(BossAttackKind.Pesado, boss.AtaqueActual.Tipo);
            Assert.AreEqual(BossAttackKind.Basico, boss.SiguienteAtaque().Tipo);
            Assert.AreEqual(BossAttackKind.Especial, boss.SiguienteAtaque().Tipo);
            Assert.AreEqual(BossAttackKind.Pesado, boss.SiguienteAtaque().Tipo);
            Assert.AreEqual(BossAttackKind.Basico, boss.SiguienteAtaque().Tipo);
            
            boss.AplicarDaño(0.205f);
            Assert.AreEqual(3, boss.FaseActual);

            Assert.AreEqual(BossAttackKind.Basico, boss.AtaqueActual.Tipo);
            Assert.AreEqual(BossAttackKind.Pesado, boss.SiguienteAtaque().Tipo);
            Assert.AreEqual(BossAttackKind.Pesado, boss.SiguienteAtaque().Tipo);
            Assert.AreEqual(BossAttackKind.Especial, boss.SiguienteAtaque().Tipo);
            Assert.AreEqual(BossAttackKind.Basico, boss.SiguienteAtaque().Tipo);
            Assert.AreEqual(BossAttackKind.Especial, boss.SiguienteAtaque().Tipo);

            boss.AplicarDaño(0.205f);
            Assert.AreEqual(4, boss.FaseActual);

            Assert.AreEqual(BossAttackKind.Basico, boss.AtaqueActual.Tipo);
            Assert.AreEqual(BossAttackKind.Especial, boss.SiguienteAtaque().Tipo);
            Assert.AreEqual(BossAttackKind.Basico, boss.SiguienteAtaque().Tipo);
            Assert.AreEqual(BossAttackKind.Especial, boss.SiguienteAtaque().Tipo);
            Assert.AreEqual(BossAttackKind.Especial, boss.SiguienteAtaque().Tipo);
            Assert.AreEqual(BossAttackKind.Pesado, boss.SiguienteAtaque().Tipo);
            Assert.AreEqual(BossAttackKind.Pesado, boss.SiguienteAtaque().Tipo);
            Assert.AreEqual(BossAttackKind.InstaKill,boss.SiguienteAtaque().Tipo);

            boss.AplicarDaño(0.205f);
            Assert.AreEqual(5, boss.FaseActual);

            Assert.AreEqual(BossAttackKind.Basico, boss.AtaqueActual.Tipo);
            Assert.AreEqual(BossAttackKind.Basico, boss.SiguienteAtaque().Tipo);
            Assert.AreEqual(BossAttackKind.InstaKill,boss.SiguienteAtaque().Tipo);
            Assert.AreEqual(BossAttackKind.InstaKill,boss.SiguienteAtaque().Tipo);
            Assert.AreEqual(BossAttackKind.Especial, boss.SiguienteAtaque().Tipo);
            Assert.AreEqual(BossAttackKind.Pesado, boss.SiguienteAtaque().Tipo);
            Assert.AreEqual(BossAttackKind.Pesado, boss.SiguienteAtaque().Tipo);
            Assert.AreEqual(BossAttackKind.Especial, boss.SiguienteAtaque().Tipo);
            Assert.AreEqual(BossAttackKind.InstaKill,boss.SiguienteAtaque().Tipo);
            Assert.AreEqual(BossAttackKind.Especial, boss.SiguienteAtaque().Tipo);
            Assert.AreEqual(BossAttackKind.InstaKill,boss.SiguienteAtaque().Tipo);
            Assert.AreEqual(BossAttackKind.Especial, boss.SiguienteAtaque().Tipo);
        }

        [Test]
        public void La_maquina_es_determinista()
        {
            var fases = CrearFasesDePrueba();

            var bossA = new BossPhaseStateMachine(fases, 1f);
            var bossB = new BossPhaseStateMachine(fases, 1f);

            for (int i = 0; i < 60; i++)
            {
                bossA.Tick(1f / 60f);
            }

            for (int i = 0; i < 30; i++)
            {
                bossB.Tick(1f / 30f);
            }

            Assert.AreEqual(
                bossA.SiguienteAtaque().Tipo,
                bossB.SiguienteAtaque().Tipo);

            Assert.AreEqual(
                bossA.FaseActual,
                bossB.FaseActual);

            Assert.AreEqual(
                bossA.AtaqueActual.Tipo,
                bossB.AtaqueActual.Tipo);
        }

        private BossPhaseProfile[] CrearFasesDePrueba()
        {
            var fase1 = ScriptableObject.CreateInstance<BossPhaseProfile>();
            fase1.Fase = 1;
            fase1.OrdenDeAtaque = new[]
            {
             new BossAttackStep(BossAttackKind.Basico, 10),
             new BossAttackStep(BossAttackKind.Pesado, 20),
             new BossAttackStep(BossAttackKind.Basico, 10),
             new BossAttackStep(BossAttackKind.Basico, 10),
            };
            fase1.VidaTransicion = 0.8f;

            var fase2 = ScriptableObject.CreateInstance<BossPhaseProfile>();
            fase2.Fase = 2;
            fase2.OrdenDeAtaque = new[]
            {
             new BossAttackStep(BossAttackKind.Pesado, 20),
             new BossAttackStep(BossAttackKind.Basico, 10),
             new BossAttackStep(BossAttackKind.Especial, 35),
             new BossAttackStep(BossAttackKind.Pesado, 20),
             new BossAttackStep(BossAttackKind.Basico, 10),
            };
            fase2.VidaTransicion = 0.6f;

            var fase3 = ScriptableObject.CreateInstance<BossPhaseProfile>();
            fase3.Fase = 3;
            fase3.OrdenDeAtaque = new[]
            {
             new BossAttackStep(BossAttackKind.Basico, 10),
             new BossAttackStep(BossAttackKind.Pesado, 20),
             new BossAttackStep(BossAttackKind.Pesado, 20),
             new BossAttackStep(BossAttackKind.Especial, 35),
             new BossAttackStep(BossAttackKind.Basico, 10),
             new BossAttackStep(BossAttackKind.Especial, 35),

            };
            fase3.VidaTransicion = 0.4f;

            var fase4 = ScriptableObject.CreateInstance<BossPhaseProfile>();
            fase4.Fase = 4;
            fase4.OrdenDeAtaque = new[]
            {
             new BossAttackStep(BossAttackKind.Basico, 10),
             new BossAttackStep(BossAttackKind.Especial, 35),
             new BossAttackStep(BossAttackKind.Basico, 10),
             new BossAttackStep(BossAttackKind.Especial, 35),
             new BossAttackStep(BossAttackKind.Especial, 35),
             new BossAttackStep(BossAttackKind.Pesado, 20),
             new BossAttackStep(BossAttackKind.Pesado, 20),
             new BossAttackStep(BossAttackKind.InstaKill, 9999, true),
            };
            fase4.VidaTransicion = 0.2f;

            var fase5 = ScriptableObject.CreateInstance<BossPhaseProfile>();
            fase5.Fase = 5;
            fase5.OrdenDeAtaque = new[]
            {
             new BossAttackStep(BossAttackKind.Basico, 10),
             new BossAttackStep(BossAttackKind.Basico, 10),
             new BossAttackStep(BossAttackKind.InstaKill, 9999, true),
             new BossAttackStep(BossAttackKind.InstaKill, 9999, true),
             new BossAttackStep(BossAttackKind.Especial, 35),
             new BossAttackStep(BossAttackKind.Pesado, 20),
             new BossAttackStep(BossAttackKind.Pesado, 20),
             new BossAttackStep(BossAttackKind.Especial, 35),
             new BossAttackStep(BossAttackKind.InstaKill, 9999, true),
             new BossAttackStep(BossAttackKind.Especial, 35),
             new BossAttackStep(BossAttackKind.InstaKill, 9999, true),
             new BossAttackStep(BossAttackKind.Especial, 35),
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
