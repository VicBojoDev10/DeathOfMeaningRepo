using System;
using System.Collections.Generic;
using NUnit.Framework;
using TDOM.Contracts;
using TDOM.Gameplay.Combat;
using TDOM.POCO.ScriptableObjects;
using UnityEngine;

namespace TDOM.Tests.EditMode
{
    public class RitmoAtaqueTest
    {
        private const float Cooldown = 3f;
        private const float Telegraph = 1f;
        private const float Dt60 = 1f / 60f;
        private const float Tolerancia = 1e-4f;

        private readonly List<BossPhaseProfile> _perfiles = new List<BossPhaseProfile>();

        [TearDown]
        public void DestruirPerfiles()
        {
            foreach (var perfil in _perfiles)
                UnityEngine.Object.DestroyImmediate(perfil);
            _perfiles.Clear();
        }

        [Test]
        public void El_ritmo_normal_no_cambia_esperas_ni_duraciones()
        {
            Assert.AreEqual(Cooldown, RitmoAtaque.Normal.Espera(Cooldown), Tolerancia);
            Assert.AreEqual(Telegraph, RitmoAtaque.Normal.Duracion(Telegraph), Tolerancia);
        }

        [Test]
        public void Mas_frecuencia_acorta_la_espera_entre_ataques()
        {
            var ritmo = new RitmoAtaque(1.5f, 1f);

            Assert.AreEqual(2f, ritmo.Espera(Cooldown), Tolerancia);
            Assert.AreEqual(Telegraph, ritmo.Duracion(Telegraph), Tolerancia);
        }

        [Test]
        public void Mas_velocidad_acorta_la_duracion_del_ataque()
        {
            var ritmo = new RitmoAtaque(1f, 2f);

            Assert.AreEqual(0.5f, ritmo.Duracion(Telegraph), Tolerancia);
            Assert.AreEqual(Cooldown, ritmo.Espera(Cooldown), Tolerancia);
        }

        [Test]
        public void La_velocidad_se_limita_para_que_el_aviso_no_baje_del_minimo()
        {
            var ritmo = new RitmoAtaque(1f, 2f);

            float velocidad = ritmo.VelocidadConAvisoLegible(1f, 0.8f);

            Assert.AreEqual(1.25f, velocidad, Tolerancia);
        }

        [Test]
        public void Un_aviso_ya_mas_corto_que_el_minimo_no_se_acelera()
        {
            var ritmo = new RitmoAtaque(1f, 2f);

            Assert.AreEqual(1f, ritmo.VelocidadConAvisoLegible(0.5f, 0.8f), Tolerancia);
        }

        [Test]
        public void Sin_aviso_minimo_se_usa_la_velocidad_completa()
        {
            var ritmo = new RitmoAtaque(1f, 2f);

            Assert.AreEqual(2f, ritmo.VelocidadConAvisoLegible(1f, 0f), Tolerancia);
        }

        [Test]
        public void Multiplicadores_no_positivos_lanzan_excepcion()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new RitmoAtaque(0f, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RitmoAtaque(1f, -1f));
        }

        [Test]
        public void El_scheduler_sin_ritmo_aplicado_conserva_los_tiempos_de_la_fase_1()
        {
            var scheduler = new BossAttackScheduler(CrearMaquina(), Cooldown, Telegraph);

            Assert.AreEqual(Cooldown, scheduler.TiempoRestante, Tolerancia);
        }

        [Test]
        public void El_scheduler_con_ritmo_lanza_antes_el_aviso_y_el_impacto()
        {
            var scheduler = new BossAttackScheduler(CrearMaquina(), Cooldown, Telegraph);
            scheduler.AplicarRitmo(new RitmoAtaque(2f, 2f));

            var tiempos = TiemposDeEventos(scheduler, 2f, Dt60);

            Assert.AreEqual(2, tiempos.Count);
            Assert.AreEqual(1.5f, tiempos[0], Dt60 + Tolerancia);
            Assert.AreEqual(2f, tiempos[1], Dt60 + Tolerancia);
        }

        [Test]
        public void El_scheduler_con_ritmo_es_determinista_con_dt_fijo()
        {
            var a = new BossAttackScheduler(CrearMaquina(), Cooldown, Telegraph);
            var b = new BossAttackScheduler(CrearMaquina(), Cooldown, Telegraph);
            var ritmo = new RitmoAtaque(1.3f, 1.3f);
            a.AplicarRitmo(ritmo);
            b.AplicarRitmo(ritmo);

            var tiemposA = TiemposDeEventos(a, 20f, Dt60);
            var tiemposB = TiemposDeEventos(b, 20f, Dt60);

            Assert.IsNotEmpty(tiemposA);
            CollectionAssert.AreEqual(tiemposA, tiemposB);
        }

        [Test]
        public void Aplicar_ritmo_nulo_lanza_excepcion()
        {
            var scheduler = new BossAttackScheduler(CrearMaquina(), Cooldown, Telegraph);

            Assert.Throws<ArgumentNullException>(() => scheduler.AplicarRitmo(null));
        }

        private static List<float> TiemposDeEventos(
            BossAttackScheduler scheduler,
            float segundos,
            float dt
        )
        {
            var tiempos = new List<float>();
            int ticks = (int)Math.Round(segundos / dt);
            for (int i = 1; i <= ticks; i++)
            {
                if (scheduler.Tick(dt, out _) != EventoAtaque.Ninguno)
                    tiempos.Add(i * dt);
            }
            return tiempos;
        }

        private BossPhaseStateMachine CrearMaquina()
        {
            var perfil = ScriptableObject.CreateInstance<BossPhaseProfile>();
            perfil.Fase = 1;
            perfil.OrdenDeAtaque = new[] { new BossAttackStep(BossAttackKind.Basico, 10) };
            _perfiles.Add(perfil);
            return new BossPhaseStateMachine(new[] { perfil });
        }
    }
}
