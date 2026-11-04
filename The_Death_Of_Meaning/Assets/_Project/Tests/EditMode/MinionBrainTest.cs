using System;
using System.Collections.Generic;
using NUnit.Framework;
using TDOM.Gameplay.Combat;

namespace TDOM.Tests.EditMode
{
    public class MinionBrainTest
    {
        private const float Rango = 1.5f;
        private const float Aviso = 0.4f;
        private const float Pausa = 0.25f;
        private const float Recuperar = 1f;
        private const float Dt60 = 1f / 60f;

        private static MinionBrain Crear() => new MinionBrain(Rango, Aviso, Pausa, Recuperar);

        // Corre el cerebro con el objetivo siempre a la misma distancia y anota cada evento.
        private static List<(EventoMinion evento, float tiempo)> Correr(
            MinionBrain brain,
            float duracion,
            float distancia,
            float dt = Dt60
        )
        {
            var registros = new List<(EventoMinion, float)>();
            float t = 0f;
            int pasos = (int)Math.Round(duracion / dt);
            for (int i = 0; i < pasos; i++)
            {
                t += dt;
                var evento = brain.Tick(dt, true, distancia);
                if (evento != EventoMinion.Ninguno)
                    registros.Add((evento, t));
            }

            return registros;
        }

        [Test]
        public void empieza_persiguiendo_y_debe_moverse()
        {
            var brain = Crear();

            Assert.AreEqual(EstadoMinion.Perseguir, brain.Estado);
            Assert.IsTrue(brain.DebeMoverse);
        }

        [Test]
        public void sin_objetivo_sigue_persiguiendo()
        {
            var brain = Crear();

            var evento = brain.Tick(Dt60, false, 0f);

            Assert.AreEqual(EventoMinion.Ninguno, evento);
            Assert.AreEqual(EstadoMinion.Perseguir, brain.Estado);
        }

        [Test]
        public void objetivo_fuera_de_rango_sigue_persiguiendo()
        {
            var brain = Crear();

            var registros = Correr(brain, 2f, Rango + 0.5f);

            Assert.IsEmpty(registros);
            Assert.AreEqual(EstadoMinion.Perseguir, brain.Estado);
        }

        [Test]
        public void objetivo_en_rango_pasa_a_atacar_con_aviso_y_deja_de_moverse()
        {
            var brain = Crear();

            var evento = brain.Tick(Dt60, true, Rango);

            Assert.AreEqual(EventoMinion.Aviso, evento);
            Assert.AreEqual(EstadoMinion.Atacar, brain.Estado);
            Assert.IsFalse(brain.DebeMoverse);
        }

        [Test]
        public void combo_BB_da_dos_golpes_cada_uno_despues_de_su_aviso()
        {
            var brain = Crear();

            var registros = Correr(brain, Aviso * 2 + Pausa + 0.1f, 1f);

            Assert.AreEqual(4, registros.Count);
            Assert.AreEqual(EventoMinion.Aviso, registros[0].evento);
            Assert.AreEqual(EventoMinion.Golpe, registros[1].evento);
            Assert.AreEqual(EventoMinion.Aviso, registros[2].evento);
            Assert.AreEqual(EventoMinion.Golpe, registros[3].evento);

            float inicio = registros[0].tiempo;
            Assert.AreEqual(inicio + Aviso, registros[1].tiempo, Dt60 * 1.5f);
            Assert.AreEqual(inicio + Aviso + Pausa, registros[2].tiempo, Dt60 * 1.5f);
            Assert.AreEqual(inicio + Aviso * 2 + Pausa, registros[3].tiempo, Dt60 * 1.5f);
            Assert.AreEqual(2, brain.GolpesDados);
        }

        [Test]
        public void despues_del_combo_recupera_y_vuelve_a_perseguir()
        {
            var brain = Crear();
            Correr(brain, Aviso * 2 + Pausa + 0.05f, 1f);

            Assert.AreEqual(EstadoMinion.Recuperar, brain.Estado);
            Assert.IsFalse(brain.DebeMoverse);

            // Durante Recuperar no ataca aunque el objetivo siga en rango.
            var registros = Correr(brain, Recuperar - 0.1f, 1f);
            Assert.IsEmpty(registros);
            Assert.AreEqual(EstadoMinion.Recuperar, brain.Estado);

            Correr(brain, 0.2f, Rango + 5f);
            Assert.AreEqual(EstadoMinion.Perseguir, brain.Estado);
        }

        [Test]
        public void el_combo_se_termina_aunque_el_objetivo_se_aleje()
        {
            var brain = Crear();
            brain.Tick(Dt60, true, 1f);

            var registros = Correr(brain, Aviso * 2 + Pausa + 0.1f, 50f);

            Assert.AreEqual(3, registros.Count);
            Assert.AreEqual(2, brain.GolpesDados);
        }

        [Test]
        public void ciclo_completo_vuelve_a_atacar_si_el_objetivo_sigue_en_rango()
        {
            var brain = Crear();

            // Dos ciclos completos; el tercer aviso cae justo después de este tiempo.
            var registros = Correr(brain, (Aviso * 2 + Pausa + Recuperar) * 2 - 0.1f, 1f);

            Assert.AreEqual(8, registros.Count);
        }

        [Test]
        public void el_ritmo_no_depende_del_framerate()
        {
            var a60 = Correr(Crear(), 2.2f, 1f, 1f / 60f);
            var a30 = Correr(Crear(), 2.2f, 1f, 1f / 30f);

            Assert.AreEqual(a60.Count, a30.Count);
            for (int i = 0; i < a60.Count; i++)
                Assert.AreEqual(a60[i].evento, a30[i].evento);
        }

        [Test]
        public void golpes_por_combo_configurable()
        {
            var brain = new MinionBrain(Rango, Aviso, Pausa, Recuperar, golpesPorCombo: 3);

            var registros = Correr(brain, Aviso * 3 + Pausa * 2 + 0.1f, 1f);

            Assert.AreEqual(6, registros.Count);
            Assert.AreEqual(EstadoMinion.Recuperar, brain.Estado);
        }

        [Test]
        public void parametros_invalidos_lanzan_excepcion()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new MinionBrain(0f, Aviso, Pausa, Recuperar)
            );
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new MinionBrain(Rango, 0f, Pausa, Recuperar)
            );
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new MinionBrain(Rango, Aviso, -1f, Recuperar)
            );
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new MinionBrain(Rango, Aviso, Pausa, -1f)
            );
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new MinionBrain(Rango, Aviso, Pausa, Recuperar, 0)
            );
        }

        [Test]
        public void elige_al_jugador_mas_cercano()
        {
            var candidatos = new List<BossTargetCandidate>
            {
                new BossTargetCandidate(1, 8f, 100f),
                new BossTargetCandidate(2, 3f, 0f),
            };

            bool hay = MinionBrain.TryElegirMasCercano(candidatos, out ulong objetivo);

            Assert.IsTrue(hay);
            Assert.AreEqual(2UL, objetivo);
        }

        [Test]
        public void sin_candidatos_no_hay_objetivo()
        {
            Assert.IsFalse(MinionBrain.TryElegirMasCercano(new List<BossTargetCandidate>(), out _));
            Assert.IsFalse(MinionBrain.TryElegirMasCercano(null, out _));
        }

        [Test]
        public void con_un_abatido_elige_al_vivo()
        {
            var candidatos = new List<MinionCandidato>
            {
                new MinionCandidato(1, 2f, false),
                new MinionCandidato(2, 9f, true),
            };

            bool hay = MinionBrain.TryElegirObjetivoVivo(candidatos, out ulong objetivo);

            Assert.IsTrue(hay);
            Assert.AreEqual(2UL, objetivo);
        }

        [Test]
        public void el_vivo_lejano_gana_al_abatido_cercano()
        {
            var candidatos = new List<MinionCandidato>
            {
                new MinionCandidato(1, 0.5f, false),
                new MinionCandidato(2, 40f, true),
            };

            Assert.IsTrue(MinionBrain.TryElegirObjetivoVivo(candidatos, out ulong objetivo));
            Assert.AreEqual(2UL, objetivo);
        }

        [Test]
        public void con_ambos_abatidos_no_elige_a_nadie()
        {
            var candidatos = new List<MinionCandidato>
            {
                new MinionCandidato(1, 2f, false),
                new MinionCandidato(2, 9f, false),
            };

            Assert.IsFalse(MinionBrain.TryElegirObjetivoVivo(candidatos, out ulong objetivo));
            Assert.AreEqual(0UL, objetivo);
        }

        [Test]
        public void con_ambos_vivos_elige_al_mas_cercano()
        {
            var candidatos = new List<MinionCandidato>
            {
                new MinionCandidato(1, 8f, true),
                new MinionCandidato(2, 3f, true),
            };

            Assert.IsTrue(MinionBrain.TryElegirObjetivoVivo(candidatos, out ulong objetivo));
            Assert.AreEqual(2UL, objetivo);
        }

        [Test]
        public void al_caer_el_objetivo_la_siguiente_eleccion_cambia_al_companero()
        {
            var candidatos = new List<MinionCandidato>
            {
                new MinionCandidato(1, 2f, true),
                new MinionCandidato(2, 9f, true),
            };
            MinionBrain.TryElegirObjetivoVivo(candidatos, out ulong antes);

            candidatos[0] = new MinionCandidato(1, 2f, false);
            MinionBrain.TryElegirObjetivoVivo(candidatos, out ulong despues);

            Assert.AreEqual(1UL, antes);
            Assert.AreEqual(2UL, despues);
        }

        [Test]
        public void un_jugador_revivido_vuelve_a_ser_candidato()
        {
            var candidatos = new List<MinionCandidato>
            {
                new MinionCandidato(1, 2f, false),
                new MinionCandidato(2, 9f, true),
            };

            candidatos[0] = new MinionCandidato(1, 2f, true);

            Assert.IsTrue(MinionBrain.TryElegirObjetivoVivo(candidatos, out ulong objetivo));
            Assert.AreEqual(1UL, objetivo);
        }

        [Test]
        public void sin_candidatos_no_hay_objetivo_vivo()
        {
            Assert.IsFalse(MinionBrain.TryElegirObjetivoVivo(new List<MinionCandidato>(), out _));
            Assert.IsFalse(MinionBrain.TryElegirObjetivoVivo(null, out _));
        }
    }
}
