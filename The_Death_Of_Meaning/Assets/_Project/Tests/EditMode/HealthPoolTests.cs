using System.Collections.Generic;
using NUnit.Framework;
using TDOM.Gameplay.Core;

namespace TDOM.Tests.EditMode
{
    public class HealthPoolTests
    {
        private const float Tolerancia = 0.001f;

        private static HealthPool CrearPool(float vida = 100f, float tiempoAbatido = 10f) =>
            new HealthPool(vida, tiempoAbatido);

        [Test]
        public void Constructor_inicia_con_vida_llena_y_estado_vivo()
        {
            var pool = CrearPool(100f, 10f);

            Assert.AreEqual(100f, pool.BaseMax, Tolerancia);
            Assert.AreEqual(100f, pool.Max, Tolerancia);
            Assert.AreEqual(100f, pool.Current, Tolerancia);
            Assert.AreEqual(PlayerLifeState.Vivo, pool.State);
            Assert.AreEqual(10f, pool.DownedDuration, Tolerancia);
            Assert.AreEqual(10f, pool.DownedRemainingTime, Tolerancia);
        }

        [Test]
        public void El_dano_resta_vida_correctamente()
        {
            var pool = CrearPool(100f, 10f);

            pool.TakeDamage(35f);

            Assert.AreEqual(65f, pool.Current, Tolerancia);
            Assert.AreEqual(PlayerLifeState.Vivo, pool.State);
        }

        [Test]
        public void El_dano_nunca_baja_la_vida_de_cero()
        {
            var pool = CrearPool(100f, 10f);

            pool.TakeDamage(150f);

            Assert.AreEqual(0f, pool.Current, Tolerancia);
        }

        [Test]
        public void Al_llegar_a_cero_de_vida_pasa_a_abatido()
        {
            var pool = CrearPool(100f, 10f);

            pool.TakeDamage(100f);

            Assert.AreEqual(0f, pool.Current, Tolerancia);
            Assert.AreEqual(PlayerLifeState.Abatido, pool.State);
            Assert.AreEqual(10f, pool.DownedRemainingTime, Tolerancia);
        }

        [Test]
        public void Si_el_tiempo_de_abatido_se_agota_pasa_a_muerto()
        {
            var pool = CrearPool(100f, 10f);
            pool.TakeDamage(100f);

            pool.Tick(5f);
            Assert.AreEqual(PlayerLifeState.Abatido, pool.State);
            Assert.AreEqual(5f, pool.DownedRemainingTime, Tolerancia);

            pool.Tick(5.1f);
            Assert.AreEqual(PlayerLifeState.Muerto, pool.State);
            Assert.AreEqual(0f, pool.DownedRemainingTime, Tolerancia);
        }

        [Test]
        public void Si_lo_reviven_antes_vuelve_a_vivo_con_la_vida_configurada()
        {
            var pool = CrearPool(100f, 10f);
            pool.TakeDamage(100f);
            pool.Tick(4f);

            bool revivio = pool.Revive(45f);

            Assert.IsTrue(revivio);
            Assert.AreEqual(PlayerLifeState.Vivo, pool.State);
            Assert.AreEqual(45f, pool.Current, Tolerancia);
            Assert.AreEqual(10f, pool.DownedRemainingTime, Tolerancia);
        }

        [Test]
        public void No_se_puede_revivir_a_un_jugador_que_ya_esta_vivo_o_muerto()
        {
            var poolVivo = CrearPool(100f, 10f);
            Assert.IsFalse(poolVivo.Revive(50f));

            var poolMuerto = CrearPool(100f, 10f);
            poolMuerto.TakeDamage(100f);
            poolMuerto.Tick(11f);
            Assert.AreEqual(PlayerLifeState.Muerto, poolMuerto.State);
            Assert.IsFalse(poolMuerto.Revive(50f));
        }

        [Test]
        public void El_progreso_de_revivir_completa_al_alcanzar_la_duracion()
        {
            var pool = CrearPool(100f, 10f);
            pool.TakeDamage(100f);

            bool completado1 = pool.ProgressRevive(1f, 3f, 50f);
            Assert.IsFalse(completado1);
            Assert.AreEqual(1f / 3f, pool.ReviveProgress, Tolerancia);
            Assert.AreEqual(PlayerLifeState.Abatido, pool.State);

            bool completado2 = pool.ProgressRevive(2f, 3f, 50f);
            Assert.IsTrue(completado2);
            Assert.AreEqual(PlayerLifeState.Vivo, pool.State);
            Assert.AreEqual(50f, pool.Current, Tolerancia);
        }

        [Test]
        public void Cancelar_revivir_reinicia_el_progreso()
        {
            var pool = CrearPool(100f, 10f);
            pool.TakeDamage(100f);
            pool.ProgressRevive(1.5f, 3f, 50f);

            pool.CancelRevive();

            Assert.AreEqual(0f, pool.ReviveProgress, Tolerancia);
            Assert.AreEqual(PlayerLifeState.Abatido, pool.State);
        }

        [Test]
        public void Curar_incrementa_vida_sin_superar_el_maximo()
        {
            var pool = CrearPool(100f, 10f);
            pool.TakeDamage(40f);

            pool.Heal(20f);
            Assert.AreEqual(80f, pool.Current, Tolerancia);

            pool.Heal(50f);
            Assert.AreEqual(100f, pool.Current, Tolerancia);
        }

        [Test]
        public void No_se_puede_recibir_dano_si_ya_esta_abatido_o_muerto()
        {
            var pool = CrearPool(100f, 10f);
            pool.TakeDamage(100f);
            Assert.AreEqual(PlayerLifeState.Abatido, pool.State);

            pool.TakeDamage(50f);
            Assert.AreEqual(0f, pool.Current, Tolerancia);

            pool.Tick(11f);
            Assert.AreEqual(PlayerLifeState.Muerto, pool.State);
            pool.TakeDamage(50f);
            Assert.AreEqual(0f, pool.Current, Tolerancia);
        }

        [Test]
        public void Los_dos_abatidos_a_la_vez_es_game_over()
        {
            var p1 = CrearPool(100f, 10f);
            var p2 = CrearPool(100f, 10f);

            p1.TakeDamage(100f);
            Assert.AreEqual(PlayerLifeState.Abatido, p1.State);
            Assert.IsFalse(HealthPool.CheckGameOver(p1, p2));

            p2.TakeDamage(100f);
            Assert.AreEqual(PlayerLifeState.Abatido, p2.State);
            Assert.IsTrue(HealthPool.CheckGameOver(p1, p2));
        }

        [Test]
        public void Un_solo_jugador_abatido_no_es_game_over_si_el_otro_sigue_vivo()
        {
            var p1 = CrearPool(100f, 10f);
            var p2 = CrearPool(100f, 10f);

            p1.TakeDamage(100f);

            Assert.IsFalse(HealthPool.CheckGameOver(p1, p2));
            Assert.IsFalse(HealthPool.CheckGameOver(new List<HealthPool> { p1, p2 }));
        }

        [Test]
        public void Jugador_muerto_provoca_game_over()
        {
            var p1 = CrearPool(100f, 10f);
            var p2 = CrearPool(100f, 10f);

            p1.TakeDamage(100f);
            p1.Tick(10.1f);

            Assert.AreEqual(PlayerLifeState.Muerto, p1.State);
            Assert.IsTrue(HealthPool.CheckGameOver(p1, p2));
            Assert.IsTrue(HealthPool.CheckGameOver(new List<HealthPool> { p1, p2 }));
        }

        [Test]
        public void El_tiempo_de_abatido_es_determinista_con_dt_fijo()
        {
            var a60 = CrearPool(100f, 10f);
            var a30 = CrearPool(100f, 10f);
            a60.TakeDamage(100f);
            a30.TakeDamage(100f);

            for (int i = 0; i < 300; i++)
                a60.Tick(1f / 60f);

            for (int i = 0; i < 150; i++)
                a30.Tick(1f / 30f);

            Assert.AreEqual(a60.DownedRemainingTime, a30.DownedRemainingTime, Tolerancia);
            Assert.AreEqual(5f, a60.DownedRemainingTime, Tolerancia);
            Assert.AreEqual(a60.DownedNormalizedTime, a30.DownedNormalizedTime, Tolerancia);
            Assert.AreEqual(0.5f, a60.DownedNormalizedTime, Tolerancia);
            Assert.AreEqual(a60.State, a30.State);
        }

        [Test]
        public void El_progreso_de_revivir_es_determinista_con_dt_fijo()
        {
            var a60 = CrearPool(100f, 10f);
            var a30 = CrearPool(100f, 10f);
            a60.TakeDamage(100f);
            a30.TakeDamage(100f);

            for (int i = 0; i < 60; i++)
                a60.ProgressRevive(1f / 60f, 3f, 50f);

            for (int i = 0; i < 30; i++)
                a30.ProgressRevive(1f / 30f, 3f, 50f);

            Assert.AreEqual(a60.ReviveProgress, a30.ReviveProgress, Tolerancia);
            Assert.AreEqual(1f / 3f, a60.ReviveProgress, Tolerancia);
        }
    }
}
