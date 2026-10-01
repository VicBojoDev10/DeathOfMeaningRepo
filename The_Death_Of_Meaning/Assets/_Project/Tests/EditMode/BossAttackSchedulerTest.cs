using System;
using System.Collections.Generic;
using NUnit.Framework;
using TDOM.Contracts;
using TDOM.Gameplay.Combat;
using TDOM.POCO.ScriptableObjects;
using UnityEngine;

namespace TDOM.Tests.EditMode
{
    public class BossAttackSchedulerTest
    {
        private const float Cooldown = 3f;
        private const float Telegraph = 1.25f;
        private const float Dt60 = 1f / 60f;
        private const float Dt30 = 1f / 30f;

        private readonly List<BossPhaseProfile> _perfiles = new List<BossPhaseProfile>();

        [TearDown]
        public void DestruirPerfiles()
        {
            foreach (var perfil in _perfiles)
                UnityEngine.Object.DestroyImmediate(perfil);
            _perfiles.Clear();
        }

        [Test]
        public void Fase1_ProduceTelegraphEImpacto_EnOrdenBPBBB_ConLosTiemposEsperados()
        {
            var scheduler = new BossAttackScheduler(CrearMaquina(), Cooldown, Telegraph);
            var esperados = new[]
            {
                BossAttackKind.Basico,
                BossAttackKind.Pesado,
                BossAttackKind.Basico,
                BossAttackKind.Basico,
                BossAttackKind.Basico, // la secuencia vuelve a empezar
            };

            var registros = Correr(scheduler, 21.5f, Dt60);

            Assert.AreEqual(esperados.Length * 2, registros.Count);
            for (int i = 0; i < esperados.Length; i++)
            {
                float tiempoTelegraph = Cooldown + i * (Cooldown + Telegraph);
                AssertEvento(
                    registros[i * 2],
                    EventoAtaque.Telegraph,
                    esperados[i],
                    tiempoTelegraph,
                    Dt60
                );
                AssertEvento(
                    registros[i * 2 + 1],
                    EventoAtaque.Impacto,
                    esperados[i],
                    tiempoTelegraph + Telegraph,
                    Dt60
                );
            }
        }

        [Test]
        public void TrasCambioDeFase_ElSiguienteAtaqueEsElPrimeroDeLaNuevaSecuencia()
        {
            var maquina = CrearMaquina();
            var scheduler = new BossAttackScheduler(maquina, Cooldown, Telegraph);
            Correr(scheduler, 5f, Dt60); // Telegraph e Impacto del primer Basico

            maquina.AplicarDaño(0.25f);
            scheduler.ReiniciarPorCambioDeFase();
            var registros = Correr(scheduler, Cooldown + 0.5f, Dt60);

            Assert.AreEqual(2, maquina.FaseActual);
            AssertEvento(
                registros[0],
                EventoAtaque.Telegraph,
                BossAttackKind.Pesado,
                Cooldown,
                Dt60
            );
        }

        [Test]
        public void CambioDeFaseDuranteTelegraph_CancelaElImpactoPendiente()
        {
            var maquina = CrearMaquina();
            var scheduler = new BossAttackScheduler(maquina, Cooldown, Telegraph);
            Correr(scheduler, Cooldown + 0.5f, Dt60); // queda en telegraph del Basico
            Assert.IsTrue(scheduler.EnTelegraph);

            maquina.AplicarDaño(0.25f);
            scheduler.ReiniciarPorCambioDeFase();
            var registros = Correr(scheduler, Cooldown + 0.5f, Dt60);

            Assert.AreEqual(1, registros.Count);
            AssertEvento(
                registros[0],
                EventoAtaque.Telegraph,
                BossAttackKind.Pesado,
                Cooldown,
                Dt60
            );
        }

        [Test]
        public void EsIndependienteDelFramerate()
        {
            var a = new BossAttackScheduler(CrearMaquina(), Cooldown, Telegraph);
            var b = new BossAttackScheduler(CrearMaquina(), Cooldown, Telegraph);

            var eventosA = SoloEventos(Correr(a, 30f, Dt60));
            var eventosB = SoloEventos(Correr(b, 30f, Dt30));

            Assert.IsNotEmpty(eventosA);
            CollectionAssert.AreEqual(eventosA, eventosB);
        }

        [Test]
        public void TiempoRestante_BajaDuranteElCooldown()
        {
            var scheduler = new BossAttackScheduler(CrearMaquina(), Cooldown, Telegraph);

            scheduler.Tick(1f, out _);

            Assert.AreEqual(Cooldown - 1f, scheduler.TiempoRestante, 1e-3f);
            Assert.AreEqual(BossAttackKind.Basico, scheduler.AtaqueActual);
        }

        [Test]
        public void Constructor_SinMaquina_Lanza()
        {
            Assert.Throws<ArgumentNullException>(() => new BossAttackScheduler(null));
        }

        private struct Registro
        {
            public EventoAtaque Evento;
            public BossAttackKind Ataque;
            public float Tiempo;
        }

        private static List<Registro> Correr(
            BossAttackScheduler scheduler,
            float segundos,
            float dt
        )
        {
            var registros = new List<Registro>();
            int ticks = (int)Math.Round(segundos / dt);

            for (int i = 1; i <= ticks; i++)
            {
                var evento = scheduler.Tick(dt, out var ataque);
                if (evento != EventoAtaque.Ninguno)
                    registros.Add(
                        new Registro
                        {
                            Evento = evento,
                            Ataque = ataque,
                            Tiempo = i * dt,
                        }
                    );
            }

            return registros;
        }

        private static List<string> SoloEventos(List<Registro> registros)
        {
            var eventos = new List<string>();
            foreach (var registro in registros)
                eventos.Add($"{registro.Evento}:{registro.Ataque}");
            return eventos;
        }

        private static void AssertEvento(
            Registro registro,
            EventoAtaque evento,
            BossAttackKind ataque,
            float tiempoEsperado,
            float dt
        )
        {
            Assert.AreEqual(evento, registro.Evento);
            Assert.AreEqual(ataque, registro.Ataque);
            Assert.AreEqual(tiempoEsperado, registro.Tiempo, dt + 1e-3f);
        }

        // Dos fases bastan: la fase 1 real (B-P-B-B) y una segunda que empieza con Pesado.
        private BossPhaseStateMachine CrearMaquina()
        {
            var fase1 = CrearPerfil(
                1,
                0.8f,
                BossAttackKind.Basico,
                BossAttackKind.Pesado,
                BossAttackKind.Basico,
                BossAttackKind.Basico
            );
            var fase2 = CrearPerfil(
                2,
                0f,
                BossAttackKind.Pesado,
                BossAttackKind.Basico,
                BossAttackKind.Especial
            );

            return new BossPhaseStateMachine(new[] { fase1, fase2 });
        }

        private BossPhaseProfile CrearPerfil(
            int fase,
            float vidaTransicion,
            params BossAttackKind[] orden
        )
        {
            var perfil = ScriptableObject.CreateInstance<BossPhaseProfile>();
            perfil.Fase = fase;
            perfil.VidaTransicion = vidaTransicion;
            perfil.OrdenDeAtaque = new BossAttackStep[orden.Length];
            for (int i = 0; i < orden.Length; i++)
                perfil.OrdenDeAtaque[i] = new BossAttackStep(orden[i], 10);

            _perfiles.Add(perfil);
            return perfil;
        }
    }
}
