using System.Collections.Generic;
using NUnit.Framework;
using TDOM.Contracts;
using TDOM.Gameplay.Combat;
using TDOM.POCO.ScriptableObjects;
using UnityEngine;

namespace TDOM.Tests.EditMode
{
    public class IBossPhaseStateTest
    {
        private const float DuracionTransicion = 3f;
        private const float Cooldown = 3f;
        private const float Telegraph = 1.25f;
        private const float Dt60 = 1f / 60f;

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
                TerminarTransicion(boss);

                boss.AplicarDaño(0.1998f);
                Assert.AreEqual(2, boss.FaseActual);

                boss.AplicarDaño(0.0003f);
                Assert.AreEqual(3, boss.FaseActual);
            }

            // Umbral 0.4 -> Fase 4
            {
                {
                    var boss = new BossPhaseStateMachine(CrearFasesDePrueba());

                    boss.AplicarDaño(0.2001f);
                    Assert.AreEqual(2, boss.FaseActual);
                    TerminarTransicion(boss);

                    boss.AplicarDaño(0.001f);
                    Assert.AreEqual(2, boss.FaseActual);

                    boss.AplicarDaño(0.1991f);
                    Assert.AreEqual(3, boss.FaseActual);
                    TerminarTransicion(boss);

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
                TerminarTransicion(boss);

                boss.AplicarDaño(0.2001f);
                Assert.AreEqual(3, boss.FaseActual);
                TerminarTransicion(boss);

                boss.AplicarDaño(0.2001f);
                Assert.AreEqual(4, boss.FaseActual);
                TerminarTransicion(boss);

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

            // El daño que sobró no arrastra: al terminar la transición un golpe chico
            // no salta a la fase 3.
            TerminarTransicion(boss);
            boss.AplicarDaño(0.01f);

            Assert.AreEqual(2, boss.FaseActual);
            Assert.AreEqual(0.79f, boss.VidaActual, 1e-4f);
        }

        [Test]
        public void El_daño_durante_la_transicion_no_cambia_vida_ni_fase()
        {
            var boss = new BossPhaseStateMachine(CrearFasesDePrueba());

            boss.AplicarDaño(0.25f);
            Assert.AreEqual(2, boss.FaseActual);
            Assert.IsTrue(boss.EnTransicion);
            float vidaAlEntrar = boss.VidaActual;

            boss.AplicarDaño(0.5f);

            Assert.AreEqual(2, boss.FaseActual);
            Assert.AreEqual(vidaAlEntrar, boss.VidaActual, 1e-6f);
        }

        [Test]
        public void La_transicion_dura_lo_que_dice_el_perfil_de_la_fase_nueva()
        {
            var fases = CrearFasesDePrueba();
            fases[0].DuracionTransicion = 10f; // la que termina: no se usa
            fases[1].DuracionTransicion = 2f;
            var boss = new BossPhaseStateMachine(fases);
            int terminadas = 0;
            boss.TransicionTerminada += () => terminadas++;

            boss.AplicarDaño(0.25f);
            Assert.AreEqual(2f, boss.TiempoTransicionRestante, 1e-6f);

            boss.Tick(1.9f);
            Assert.IsTrue(boss.EnTransicion);
            Assert.AreEqual(0, terminadas);

            boss.Tick(0.2f);
            Assert.IsFalse(boss.EnTransicion);
            Assert.AreEqual(1, terminadas);

            boss.Tick(1f);
            Assert.AreEqual(1, terminadas);
        }

        [Test]
        public void Al_terminar_la_transicion_el_patron_empieza_desde_el_primer_ataque_de_la_fase_nueva()
        {
            var boss = new BossPhaseStateMachine(CrearFasesDePrueba());
            var scheduler = new BossAttackScheduler(boss, Cooldown, Telegraph);
            boss.TransicionTerminada += scheduler.ReiniciarPorCambioDeFase;

            // Avanzar el patrón de la fase 1 y quedar a mitad de un telegraph.
            Correr(boss, scheduler, Cooldown + Telegraph + Cooldown + 0.5f);
            Assert.IsTrue(scheduler.EnTelegraph);

            boss.AplicarDaño(0.25f);
            Assert.AreEqual(2, boss.FaseActual);

            // Durante la transición el scheduler no avanza (como en BossController).
            var durante = Correr(boss, scheduler, DuracionTransicion - 0.1f);
            Assert.AreEqual(0, durante.Count);
            Assert.IsTrue(boss.EnTransicion);

            var despues = Correr(boss, scheduler, 0.1f + Cooldown + 0.1f);

            Assert.IsFalse(boss.EnTransicion);
            Assert.AreEqual(1, despues.Count);
            Assert.AreEqual(EventoAtaque.Telegraph, despues[0].Evento);
            Assert.AreEqual(BossAttackKind.Pesado, despues[0].Ataque);
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

            TerminarTransicion(boss);
            boss.AplicarDaño(0.205f);
            Assert.AreEqual(3, boss.FaseActual);

            Assert.AreEqual(BossAttackKind.Basico, boss.AtaqueActual.Tipo);
            Assert.AreEqual(BossAttackKind.Pesado, boss.SiguienteAtaque().Tipo);
            Assert.AreEqual(BossAttackKind.Pesado, boss.SiguienteAtaque().Tipo);
            Assert.AreEqual(BossAttackKind.Especial, boss.SiguienteAtaque().Tipo);
            Assert.AreEqual(BossAttackKind.Basico, boss.SiguienteAtaque().Tipo);
            Assert.AreEqual(BossAttackKind.Especial, boss.SiguienteAtaque().Tipo);

            TerminarTransicion(boss);
            boss.AplicarDaño(0.205f);
            Assert.AreEqual(4, boss.FaseActual);

            Assert.AreEqual(BossAttackKind.Basico, boss.AtaqueActual.Tipo);
            Assert.AreEqual(BossAttackKind.Especial, boss.SiguienteAtaque().Tipo);
            Assert.AreEqual(BossAttackKind.Basico, boss.SiguienteAtaque().Tipo);
            Assert.AreEqual(BossAttackKind.Especial, boss.SiguienteAtaque().Tipo);
            Assert.AreEqual(BossAttackKind.Especial, boss.SiguienteAtaque().Tipo);
            Assert.AreEqual(BossAttackKind.Pesado, boss.SiguienteAtaque().Tipo);
            Assert.AreEqual(BossAttackKind.Pesado, boss.SiguienteAtaque().Tipo);
            Assert.AreEqual(BossAttackKind.InstaKill, boss.SiguienteAtaque().Tipo);

            TerminarTransicion(boss);
            boss.AplicarDaño(0.205f);
            Assert.AreEqual(5, boss.FaseActual);

            Assert.AreEqual(BossAttackKind.Basico, boss.AtaqueActual.Tipo);
            Assert.AreEqual(BossAttackKind.Basico, boss.SiguienteAtaque().Tipo);
            Assert.AreEqual(BossAttackKind.InstaKill, boss.SiguienteAtaque().Tipo);
            Assert.AreEqual(BossAttackKind.InstaKill, boss.SiguienteAtaque().Tipo);
            Assert.AreEqual(BossAttackKind.Especial, boss.SiguienteAtaque().Tipo);
            Assert.AreEqual(BossAttackKind.Pesado, boss.SiguienteAtaque().Tipo);
            Assert.AreEqual(BossAttackKind.Pesado, boss.SiguienteAtaque().Tipo);
            Assert.AreEqual(BossAttackKind.Especial, boss.SiguienteAtaque().Tipo);
            Assert.AreEqual(BossAttackKind.InstaKill, boss.SiguienteAtaque().Tipo);
            Assert.AreEqual(BossAttackKind.Especial, boss.SiguienteAtaque().Tipo);
            Assert.AreEqual(BossAttackKind.InstaKill, boss.SiguienteAtaque().Tipo);
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

            Assert.AreEqual(bossA.SiguienteAtaque().Tipo, bossB.SiguienteAtaque().Tipo);

            Assert.AreEqual(bossA.FaseActual, bossB.FaseActual);

            Assert.AreEqual(bossA.AtaqueActual.Tipo, bossB.AtaqueActual.Tipo);
        }

        [Test]
        public void Aplicar_daño_emite_vida_cambiada_con_la_vida_resultante()
        {
            var boss = new BossPhaseStateMachine(CrearFasesDePrueba());
            var vidas = new List<float>();
            boss.VidaCambiada += vidas.Add;

            boss.AplicarDaño(0.1f);

            Assert.AreEqual(1, vidas.Count);
            Assert.AreEqual(0.9f, vidas[0], 1e-6f);
            Assert.AreEqual(boss.VidaActual, vidas[0], 1e-6f);
        }

        [Test]
        public void Al_cruzar_un_umbral_vida_cambiada_emite_el_umbral_de_la_fase()
        {
            var boss = new BossPhaseStateMachine(CrearFasesDePrueba());
            var vidas = new List<float>();
            boss.VidaCambiada += vidas.Add;

            boss.AplicarDaño(0.5f);

            Assert.AreEqual(1, vidas.Count);
            Assert.AreEqual(0.8f, vidas[0], 1e-6f);
        }

        [Test]
        public void El_daño_durante_la_transicion_se_ignora_y_no_emite_vida_cambiada()
        {
            var boss = new BossPhaseStateMachine(CrearFasesDePrueba());
            boss.AplicarDaño(0.25f);
            Assert.IsTrue(boss.EnTransicion);
            int emisiones = 0;
            boss.VidaCambiada += _ => emisiones++;

            boss.AplicarDaño(0.1f);

            Assert.AreEqual(0, emisiones);
        }

        [Test]
        public void El_daño_nulo_o_negativo_no_cambia_la_vida_ni_emite_vida_cambiada()
        {
            var boss = new BossPhaseStateMachine(CrearFasesDePrueba());
            int emisiones = 0;
            boss.VidaCambiada += _ => emisiones++;

            boss.AplicarDaño(0f);
            boss.AplicarDaño(-0.3f);

            Assert.AreEqual(0, emisiones);
            Assert.AreEqual(1f, boss.VidaActual, 1e-6f);
        }

        [Test]
        public void La_vida_no_baja_de_cero_y_emite_muerto_una_sola_vez()
        {
            var boss = new BossPhaseStateMachine(CrearFasesDePrueba());
            LlevarAUltimaFase(boss);
            int muertes = 0;
            int emisiones = 0;
            boss.Muerto += () => muertes++;
            boss.VidaCambiada += _ => emisiones++;

            boss.AplicarDaño(0.5f);
            boss.AplicarDaño(0.5f);

            Assert.AreEqual(0f, boss.VidaActual, 1e-6f);
            Assert.IsTrue(boss.EstaMuerto);
            Assert.AreEqual(1, muertes);
            Assert.AreEqual(1, emisiones);
        }

        [Test]
        public void Muerto_no_se_emite_si_la_vida_queda_justo_encima_de_cero()
        {
            var boss = new BossPhaseStateMachine(CrearFasesDePrueba());
            LlevarAUltimaFase(boss);
            int muertes = 0;
            boss.Muerto += () => muertes++;

            boss.AplicarDaño(boss.VidaActual - 0.001f);

            Assert.AreEqual(0, muertes);
            Assert.IsFalse(boss.EstaMuerto);
        }

        [Test]
        public void Muerto_no_se_emite_antes_de_la_ultima_fase()
        {
            var fases = CrearFasesDePrueba();
            fases[0].VidaTransicion = 0f;
            var boss = new BossPhaseStateMachine(fases);
            int muertes = 0;
            boss.Muerto += () => muertes++;

            boss.AplicarDaño(1.5f);

            Assert.AreEqual(2, boss.FaseActual);
            Assert.AreEqual(0, muertes);
            Assert.IsFalse(boss.EstaMuerto);
        }

        [Test]
        public void La_vida_emitida_es_determinista_con_dt_fijo()
        {
            var vidasA = SimularGolpesConDtFijo();
            var vidasB = SimularGolpesConDtFijo();

            CollectionAssert.AreEqual(vidasA, vidasB);
            Assert.AreEqual(0f, vidasA[vidasA.Count - 1], 1e-6f);
        }

        private List<float> SimularGolpesConDtFijo()
        {
            const int TicksEntreGolpes = 30;
            const int TicksTotales = 60 * 60;
            const float DanioPorGolpe = 0.05f;

            var boss = new BossPhaseStateMachine(CrearFasesDePrueba());
            var vidas = new List<float>();
            boss.VidaCambiada += vidas.Add;

            for (int tick = 1; tick <= TicksTotales && !boss.EstaMuerto; tick++)
            {
                boss.Tick(Dt60);
                if (tick % TicksEntreGolpes == 0)
                    boss.AplicarDaño(DanioPorGolpe);
            }

            return vidas;
        }

        private static void LlevarAUltimaFase(BossPhaseStateMachine boss)
        {
            const int FasesPorAvanzar = 4;
            for (int i = 0; i < FasesPorAvanzar; i++)
            {
                boss.AplicarDaño(0.2001f);
                TerminarTransicion(boss);
            }
            Assert.AreEqual(5, boss.FaseActual);
        }

        private static void TerminarTransicion(BossPhaseStateMachine boss)
        {
            boss.Tick(boss.TiempoTransicionRestante + 0.01f);
            Assert.IsFalse(boss.EnTransicion);
        }

        private readonly struct Registro
        {
            public readonly EventoAtaque Evento;
            public readonly BossAttackKind Ataque;

            public Registro(EventoAtaque evento, BossAttackKind ataque)
            {
                Evento = evento;
                Ataque = ataque;
            }
        }

        // Igual que BossController.Update: la máquina siempre avanza; el scheduler solo
        // fuera de la transición.
        private static List<Registro> Correr(
            BossPhaseStateMachine boss,
            BossAttackScheduler scheduler,
            float segundos
        )
        {
            var registros = new List<Registro>();
            int pasos = Mathf.RoundToInt(segundos / Dt60);
            for (int i = 0; i < pasos; i++)
            {
                boss.Tick(Dt60);
                if (boss.EnTransicion)
                    continue;

                var evento = scheduler.Tick(Dt60, out var ataque);
                if (evento != EventoAtaque.Ninguno)
                    registros.Add(new Registro(evento, ataque));
            }
            return registros;
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

            return new[] { fase1, fase2, fase3, fase4, fase5 };
        }
    }
}
