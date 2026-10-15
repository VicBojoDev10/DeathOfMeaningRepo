using NUnit.Framework;
using TDOM.Gameplay.Core;

namespace TDOM.Tests.EditMode
{
    public class LoadingScreenStateTests
    {
        private const float TiempoMinimo = 0.5f;
        private const float Dt60 = 1f / 60f;
        private const int TicksAntesDelMinimo = 29;
        private const int TicksDespuesDelMinimo = 31;
        private const int LimiteDeTicks = 1000;
        private const float Tolerancia = 0.0001f;

        [Test]
        public void Empieza_oculta_y_sin_progreso()
        {
            var pantalla = new LoadingScreenState(TiempoMinimo);

            Assert.IsFalse(pantalla.Visible);
            Assert.AreEqual(0f, pantalla.Progreso);
        }

        [Test]
        public void Mostrar_la_hace_visible_y_avisa_una_sola_vez()
        {
            var pantalla = new LoadingScreenState(TiempoMinimo);
            int avisos = 0;
            pantalla.Mostrada += () => avisos++;

            pantalla.Mostrar();
            pantalla.Mostrar();

            Assert.IsTrue(pantalla.Visible);
            Assert.AreEqual(1, avisos);
        }

        [Test]
        public void Carga_instantanea_sigue_visible_hasta_cumplir_el_tiempo_minimo()
        {
            var pantalla = new LoadingScreenState(TiempoMinimo);
            pantalla.Mostrar();
            pantalla.CompletarCarga();

            Avanzar(pantalla, TicksAntesDelMinimo);

            Assert.IsTrue(pantalla.Visible);
        }

        [Test]
        public void Carga_completa_y_tiempo_minimo_cumplido_la_oculta()
        {
            var pantalla = new LoadingScreenState(TiempoMinimo);
            bool ocultada = false;
            pantalla.Ocultada += () => ocultada = true;
            pantalla.Mostrar();
            pantalla.CompletarCarga();

            Avanzar(pantalla, TicksDespuesDelMinimo);

            Assert.IsFalse(pantalla.Visible);
            Assert.IsTrue(ocultada);
        }

        [Test]
        public void Sin_carga_completa_no_se_oculta_aunque_pase_el_tiempo_minimo()
        {
            var pantalla = new LoadingScreenState(TiempoMinimo);
            pantalla.Mostrar();

            Avanzar(pantalla, LimiteDeTicks);

            Assert.IsTrue(pantalla.Visible);
        }

        [Test]
        public void Completar_la_carga_despues_del_tiempo_minimo_la_oculta_de_inmediato()
        {
            var pantalla = new LoadingScreenState(TiempoMinimo);
            pantalla.Mostrar();
            Avanzar(pantalla, TicksDespuesDelMinimo);

            pantalla.CompletarCarga();

            Assert.IsFalse(pantalla.Visible);
        }

        [Test]
        public void El_progreso_se_limita_entre_cero_y_uno_y_no_retrocede()
        {
            var pantalla = new LoadingScreenState(TiempoMinimo);
            pantalla.Mostrar();

            pantalla.ReportarProgreso(0.6f);
            pantalla.ReportarProgreso(0.3f);
            Assert.AreEqual(0.6f, pantalla.Progreso, Tolerancia);

            pantalla.ReportarProgreso(2f);
            Assert.AreEqual(1f, pantalla.Progreso, Tolerancia);
        }

        [Test]
        public void Completar_la_carga_deja_el_progreso_en_uno()
        {
            var pantalla = new LoadingScreenState(TiempoMinimo);
            pantalla.Mostrar();
            pantalla.ReportarProgreso(0.2f);

            pantalla.CompletarCarga();

            Assert.AreEqual(1f, pantalla.Progreso, Tolerancia);
        }

        [Test]
        public void Completar_la_carga_sin_mostrar_no_hace_nada()
        {
            var pantalla = new LoadingScreenState(TiempoMinimo);
            bool ocultada = false;
            pantalla.Ocultada += () => ocultada = true;

            pantalla.CompletarCarga();

            Assert.IsFalse(pantalla.Visible);
            Assert.IsFalse(ocultada);
        }

        [Test]
        public void Volver_a_mostrar_reinicia_el_tiempo_minimo_y_el_progreso()
        {
            var pantalla = new LoadingScreenState(TiempoMinimo);
            pantalla.Mostrar();
            pantalla.ReportarProgreso(0.8f);
            Avanzar(pantalla, TicksAntesDelMinimo);

            pantalla.Mostrar();
            pantalla.CompletarCarga();
            Avanzar(pantalla, TicksAntesDelMinimo);

            Assert.IsTrue(pantalla.Visible);
        }

        [Test]
        public void Ocultar_es_determinista_con_dt_fijo()
        {
            int primera = TicksHastaOcultar();
            int segunda = TicksHastaOcultar();

            Assert.AreEqual(primera, segunda);
            Assert.Greater(primera, TicksAntesDelMinimo);
            Assert.LessOrEqual(primera, TicksDespuesDelMinimo);
        }

        private static int TicksHastaOcultar()
        {
            var pantalla = new LoadingScreenState(TiempoMinimo);
            pantalla.Mostrar();
            pantalla.CompletarCarga();

            int ticks = 0;
            while (pantalla.Visible && ticks < LimiteDeTicks)
            {
                pantalla.Tick(Dt60);
                ticks++;
            }
            return ticks;
        }

        private static void Avanzar(LoadingScreenState pantalla, int ticks)
        {
            for (int i = 0; i < ticks; i++)
                pantalla.Tick(Dt60);
        }
    }
}
