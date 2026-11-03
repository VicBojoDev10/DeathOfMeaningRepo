using NUnit.Framework;
using TDOM.Gameplay.Combat;

namespace TDOM.Tests.EditMode
{
    public class BloqueoMovimientoAtaqueTests
    {
        private BloqueoMovimientoAtaque bloqueo;

        [SetUp]
        public void Setup()
        {
            bloqueo = new BloqueoMovimientoAtaque(0.5f, 10f);
        }

        [Test]
        public void Sin_ataques_el_jefe_puede_moverse()
        {
            Assert.IsTrue(bloqueo.PuedeMoverse);
            Assert.IsFalse(bloqueo.EnAtaque);
        }

        [Test]
        public void La_espera_por_defecto_es_medio_segundo()
        {
            var porDefecto = new BloqueoMovimientoAtaque();

            Assert.AreEqual(0.5f, porDefecto.EsperaTrasAtaque, 0.0001f);
        }

        [Test]
        public void Al_empezar_el_ataque_el_jefe_deja_de_moverse()
        {
            bloqueo.Iniciar();

            Assert.IsFalse(bloqueo.PuedeMoverse);
            Assert.IsTrue(bloqueo.EnAtaque);
        }

        [Test]
        public void Mientras_dura_el_ataque_el_paso_del_tiempo_no_lo_libera()
        {
            bloqueo.Iniciar();

            bloqueo.Tick(5f);

            Assert.IsFalse(bloqueo.PuedeMoverse);
        }

        [Test]
        public void Al_terminar_el_ataque_espera_antes_de_moverse()
        {
            bloqueo.Iniciar();
            bloqueo.Terminar();

            Assert.IsFalse(bloqueo.EnAtaque);
            Assert.IsFalse(bloqueo.PuedeMoverse);
            Assert.AreEqual(0.5f, bloqueo.EsperaRestante, 0.0001f);
        }

        [Test]
        public void Tras_la_espera_el_jefe_vuelve_a_moverse()
        {
            bloqueo.Iniciar();
            bloqueo.Terminar();

            bloqueo.Tick(0.3f);
            Assert.IsFalse(bloqueo.PuedeMoverse);

            bloqueo.Tick(0.2f);
            Assert.IsTrue(bloqueo.PuedeMoverse);
            Assert.AreEqual(0f, bloqueo.EsperaRestante, 0.0001f);
        }

        [Test]
        public void La_espera_configurable_se_respeta()
        {
            var largo = new BloqueoMovimientoAtaque(2f, 10f);
            largo.Iniciar();
            largo.Terminar();

            largo.Tick(1.9f);
            Assert.IsFalse(largo.PuedeMoverse);

            largo.Tick(0.1f);
            Assert.IsTrue(largo.PuedeMoverse);
        }

        [Test]
        public void Espera_cero_libera_en_cuanto_termina_el_ataque()
        {
            var sinEspera = new BloqueoMovimientoAtaque(0f, 10f);
            sinEspera.Iniciar();
            sinEspera.Terminar();

            Assert.IsTrue(sinEspera.PuedeMoverse);
        }

        [Test]
        public void Espera_negativa_se_trata_como_cero()
        {
            var negativa = new BloqueoMovimientoAtaque(-1f, 10f);

            Assert.AreEqual(0f, negativa.EsperaTrasAtaque, 0.0001f);
        }

        [Test]
        public void Un_ataque_nuevo_durante_la_espera_vuelve_a_bloquear_y_la_cancela()
        {
            bloqueo.Iniciar();
            bloqueo.Terminar();
            bloqueo.Tick(0.2f);

            bloqueo.Iniciar();

            Assert.IsTrue(bloqueo.EnAtaque);
            Assert.AreEqual(0f, bloqueo.EsperaRestante, 0.0001f);
            Assert.IsFalse(bloqueo.PuedeMoverse);
        }

        [Test]
        public void La_espera_se_reinicia_completa_tras_el_segundo_ataque()
        {
            bloqueo.Iniciar();
            bloqueo.Terminar();
            bloqueo.Tick(0.4f);
            bloqueo.Iniciar();
            bloqueo.Terminar();

            Assert.AreEqual(0.5f, bloqueo.EsperaRestante, 0.0001f);
        }

        [Test]
        public void Con_ataques_solapados_la_espera_empieza_al_terminar_el_ultimo()
        {
            bloqueo.Iniciar();
            bloqueo.Iniciar();

            bloqueo.Terminar();
            Assert.IsTrue(bloqueo.EnAtaque);
            Assert.IsFalse(bloqueo.PuedeMoverse);
            Assert.AreEqual(0f, bloqueo.EsperaRestante, 0.0001f);

            bloqueo.Terminar();
            Assert.IsFalse(bloqueo.EnAtaque);
            Assert.AreEqual(0.5f, bloqueo.EsperaRestante, 0.0001f);
        }

        [Test]
        public void Terminar_sin_ataques_activos_se_ignora()
        {
            bloqueo.Terminar();

            Assert.IsTrue(bloqueo.PuedeMoverse);
            Assert.AreEqual(0, bloqueo.AtaquesActivos);
        }

        [Test]
        public void El_tiempo_maximo_libera_un_ataque_que_nunca_termina()
        {
            bloqueo.Iniciar();

            bool timeout = bloqueo.Tick(10f);

            Assert.IsTrue(timeout);
            Assert.IsFalse(bloqueo.EnAtaque);
            Assert.IsFalse(bloqueo.PuedeMoverse);
            Assert.AreEqual(0.5f, bloqueo.EsperaRestante, 0.0001f);
        }

        [Test]
        public void Tras_el_timeout_tambien_se_aplica_la_espera()
        {
            bloqueo.Iniciar();
            bloqueo.Tick(10f);

            bloqueo.Tick(0.5f);

            Assert.IsTrue(bloqueo.PuedeMoverse);
        }

        [Test]
        public void Antes_del_tiempo_maximo_no_hay_timeout()
        {
            bloqueo.Iniciar();

            bool timeout = bloqueo.Tick(9.9f);

            Assert.IsFalse(timeout);
            Assert.IsTrue(bloqueo.EnAtaque);
        }

        [Test]
        public void El_tiempo_maximo_cuenta_desde_el_ultimo_inicio_en_frio()
        {
            bloqueo.Iniciar();
            bloqueo.Tick(9f);
            bloqueo.Terminar();
            bloqueo.Tick(1f);

            bloqueo.Iniciar();
            bool timeout = bloqueo.Tick(9f);

            Assert.IsFalse(timeout);
        }

        [Test]
        public void Tiempo_maximo_cero_desactiva_el_limite()
        {
            var sinLimite = new BloqueoMovimientoAtaque(0.5f, 0f);
            sinLimite.Iniciar();

            bool timeout = sinLimite.Tick(10000f);

            Assert.IsFalse(timeout);
            Assert.IsTrue(sinLimite.EnAtaque);
        }

        [Test]
        public void Un_terminar_tardio_tras_el_timeout_no_reabre_ni_rompe_la_espera()
        {
            bloqueo.Iniciar();
            bloqueo.Tick(10f);
            bloqueo.Tick(0.2f);

            bloqueo.Terminar();

            Assert.AreEqual(0.3f, bloqueo.EsperaRestante, 0.0001f);
            Assert.AreEqual(0, bloqueo.AtaquesActivos);
        }

        [Test]
        public void Tick_con_dt_no_positivo_no_cambia_nada()
        {
            bloqueo.Iniciar();
            bloqueo.Terminar();

            bloqueo.Tick(0f);
            bloqueo.Tick(-1f);

            Assert.AreEqual(0.5f, bloqueo.EsperaRestante, 0.0001f);
        }

        [Test]
        public void Reiniciar_olvida_ataques_y_espera()
        {
            bloqueo.Iniciar();
            bloqueo.Iniciar();

            bloqueo.Reiniciar();

            Assert.IsTrue(bloqueo.PuedeMoverse);
            Assert.AreEqual(0, bloqueo.AtaquesActivos);
            Assert.AreEqual(0f, bloqueo.EsperaRestante, 0.0001f);
        }
    }
}
