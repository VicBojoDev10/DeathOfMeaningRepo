using NUnit.Framework;
using TDOM.Gameplay.Camera;
using UnityEngine;

namespace TDOM.Tests.EditMode
{
    public class EscalaSensibilidadTests
    {
        private const float NivelMinimo = 1f;
        private const float NivelMaximo = 10f;
        private const float PasoNivel = 0.5f;
        private const float GradosPorNivel = 30f;

        private static EscalaSensibilidad CrearEscala()
        {
            return new EscalaSensibilidad(NivelMinimo, NivelMaximo, PasoNivel, GradosPorNivel);
        }

        [Test]
        public void la_escala_va_de_uno_a_diez_en_pasos_de_medio()
        {
            var escala = CrearEscala();

            Assert.AreEqual(1f, escala.NivelDesdePaso(escala.PasoMinimo), 0.001f);
            Assert.AreEqual(1.5f, escala.NivelDesdePaso(escala.PasoMinimo + 1), 0.001f);
            Assert.AreEqual(10f, escala.NivelDesdePaso(escala.PasoMaximo), 0.001f);
            Assert.AreEqual(19, escala.PasoMaximo - escala.PasoMinimo + 1);
        }

        [Test]
        public void cada_nivel_equivale_a_los_grados_por_nivel_configurados()
        {
            var escala = CrearEscala();

            Assert.AreEqual(120f, escala.GradosDesdePaso(escala.PasoDesdeGrados(120f)), 0.001f);
            Assert.AreEqual(300f, escala.GradosDesdePaso(escala.PasoMaximo), 0.001f);
        }

        [Test]
        public void los_grados_guardados_se_ajustan_al_paso_mas_cercano_dentro_del_rango()
        {
            var escala = CrearEscala();

            Assert.AreEqual(4.5f, escala.NivelDesdePaso(escala.PasoDesdeGrados(134f)), 0.001f);
            Assert.AreEqual(1f, escala.NivelDesdePaso(escala.PasoDesdeGrados(5f)), 0.001f);
            Assert.AreEqual(10f, escala.NivelDesdePaso(escala.PasoDesdeGrados(900f)), 0.001f);
        }

        [Test]
        public void el_doble_de_nivel_gira_el_doble_con_dt_fijo()
        {
            var escala = CrearEscala();
            int pasoNivelDos = escala.PasoDesdeGrados(2f * GradosPorNivel);
            int pasoNivelCuatro = escala.PasoDesdeGrados(4f * GradosPorNivel);
            var lento = LookResolver.CrearConSensibilidades(
                escala.GradosDesdePaso(pasoNivelDos),
                escala.GradosDesdePaso(pasoNivelDos)
            );
            var rapido = LookResolver.CrearConSensibilidades(
                escala.GradosDesdePaso(pasoNivelCuatro),
                escala.GradosDesdePaso(pasoNivelCuatro)
            );

            for (int frame = 0; frame < 60; frame++)
            {
                lento.Tick(new Vector2(0.1f, 0f), 1f / 60f);
                rapido.Tick(new Vector2(0.1f, 0f), 1f / 60f);
            }

            Assert.AreEqual(lento.Yaw * 2f, rapido.Yaw, 0.001f);
        }
    }
}
