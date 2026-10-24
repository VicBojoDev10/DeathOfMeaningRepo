using NUnit.Framework;
using TDOM.Gameplay.Vfx;

namespace TDOM.Tests.EditMode
{
    public class ContextoDashVfxTests
    {
        private const float Dt = 1f / 60f;
        private const float VentanaTrasGancho = 1f;
        private const int FramesPorSegundo = 60;

        private static ContextoDashVfx Crear() => new ContextoDashVfx(VentanaTrasGancho);

        private static void AvanzarSinGancho(ContextoDashVfx contexto, int frames)
        {
            for (int i = 0; i < frames; i++)
                contexto.Capturar(true, false, false, false, Dt);
        }

        [Test]
        public void dash_en_el_suelo_es_dorado()
        {
            var contexto = Crear();
            contexto.Capturar(true, false, false, false, Dt);
            Assert.AreEqual(TipoDashVfx.Suelo, contexto.Tipo);
        }

        [Test]
        public void dash_en_el_aire_es_rojo()
        {
            var contexto = Crear();
            contexto.Capturar(false, false, false, false, Dt);
            Assert.AreEqual(TipoDashVfx.Aire, contexto.Tipo);
        }

        [Test]
        public void dash_que_cancela_un_ataque_en_el_suelo_es_morado()
        {
            var contexto = Crear();
            contexto.Capturar(true, false, true, false, Dt);
            Assert.AreEqual(TipoDashVfx.Cancelacion, contexto.Tipo);
        }

        [Test]
        public void dash_que_cancela_un_ataque_en_el_aire_es_morado()
        {
            var contexto = Crear();
            contexto.Capturar(false, false, true, false, Dt);
            Assert.AreEqual(TipoDashVfx.Cancelacion, contexto.Tipo);
        }

        [Test]
        public void dash_justo_despues_del_gancho_es_morado()
        {
            var contexto = Crear();
            contexto.Capturar(false, false, false, true, Dt);
            contexto.Capturar(false, false, false, false, Dt);
            Assert.AreEqual(TipoDashVfx.Cancelacion, contexto.Tipo);
        }

        [Test]
        public void dash_antes_de_cumplir_la_ventana_del_gancho_sigue_morado()
        {
            var contexto = Crear();
            contexto.Capturar(true, false, false, true, Dt);
            AvanzarSinGancho(contexto, FramesPorSegundo - 1);
            Assert.AreEqual(TipoDashVfx.Cancelacion, contexto.Tipo);
        }

        [Test]
        public void dash_despues_de_la_ventana_del_gancho_vuelve_al_color_normal()
        {
            var contexto = Crear();
            contexto.Capturar(true, false, false, true, Dt);
            AvanzarSinGancho(contexto, FramesPorSegundo + 1);
            Assert.AreEqual(TipoDashVfx.Suelo, contexto.Tipo);
        }

        [Test]
        public void sin_haber_usado_el_gancho_no_cuenta_como_reciente()
        {
            Assert.IsFalse(Crear().GanchoReciente);
        }

        [Test]
        public void caminando_en_el_suelo_sale_la_nube_normal()
        {
            var contexto = Crear();
            contexto.Capturar(true, false, false, false, Dt);
            Assert.AreEqual(NubeDashVfx.Normal, contexto.Nube);
        }

        [Test]
        public void corriendo_en_el_suelo_sale_la_nube_grande()
        {
            var contexto = Crear();
            contexto.Capturar(true, true, false, false, Dt);
            Assert.AreEqual(NubeDashVfx.Grande, contexto.Nube);
        }

        [Test]
        public void en_el_aire_no_sale_nube()
        {
            var contexto = Crear();
            contexto.Capturar(false, true, false, false, Dt);
            Assert.AreEqual(NubeDashVfx.Ninguna, contexto.Nube);
        }

        [Test]
        public void dash_morado_en_el_suelo_tambien_saca_la_nube()
        {
            var contexto = Crear();
            contexto.Capturar(true, false, true, false, Dt);
            Assert.AreEqual(NubeDashVfx.Normal, contexto.Nube);
        }
    }
}
