using System.Collections.Generic;
using NUnit.Framework;
using TDOM.Gameplay.Core;

namespace TDOM.Tests.EditMode
{
    public class LineaPresentacionArenaTests
    {
        private const double DuracionVideo = 49.5d;
        private const double DuracionTimeline = 15.25d;
        private const double MomentoRevelado = 1.5d;
        private const double DuracionRegreso = 2.5d;
        private const double Dt60 = 1d / 60d;

        private static readonly LineaPresentacionArena Linea = new LineaPresentacionArena(
            DuracionVideo,
            DuracionTimeline,
            MomentoRevelado,
            DuracionRegreso
        );

        [TestCase(0d, EtapaPresentacionArena.Video)]
        [TestCase(DuracionVideo - 0.01d, EtapaPresentacionArena.Video)]
        [TestCase(DuracionVideo, EtapaPresentacionArena.Timeline)]
        [TestCase(
            DuracionVideo + DuracionTimeline - DuracionRegreso - 0.01d,
            EtapaPresentacionArena.Timeline
        )]
        [TestCase(
            DuracionVideo + DuracionTimeline - DuracionRegreso,
            EtapaPresentacionArena.Regreso
        )]
        public void la_etapa_depende_del_tiempo_transcurrido(
            double transcurrido,
            EtapaPresentacionArena esperada
        )
        {
            Assert.AreEqual(esperada, Linea.Etapa(transcurrido));
        }

        [Test]
        public void el_timeline_empieza_en_cero_cuando_termina_el_video()
        {
            Assert.AreEqual(0d, Linea.TiempoTimeline(DuracionVideo), 1e-9);
        }

        [TestCase(DuracionVideo + MomentoRevelado - 0.01d, false)]
        [TestCase(DuracionVideo + MomentoRevelado, true)]
        public void el_jefe_se_revela_tras_el_video_y_su_momento_del_timeline(
            double transcurrido,
            bool esperado
        )
        {
            Assert.AreEqual(esperado, Linea.DebeRevelarJefe(transcurrido));
        }

        [Test]
        public void sin_video_la_presentacion_empieza_directo_en_el_timeline()
        {
            var sinVideo = new LineaPresentacionArena(
                0d,
                DuracionTimeline,
                MomentoRevelado,
                DuracionRegreso
            );

            Assert.AreEqual(EtapaPresentacionArena.Timeline, sinVideo.Etapa(0d));
        }

        [Test]
        public void con_dt_fijo_las_etapas_avanzan_en_orden_y_sin_retrocesos()
        {
            List<EtapaPresentacionArena> Recorrer()
            {
                var etapas = new List<EtapaPresentacionArena>();
                double fin = DuracionVideo + DuracionTimeline;
                for (double t = 0d; t <= fin; t += Dt60)
                    etapas.Add(Linea.Etapa(t));
                return etapas;
            }

            var primera = Recorrer();
            CollectionAssert.AreEqual(primera, Recorrer());
            for (int i = 1; i < primera.Count; i++)
                Assert.GreaterOrEqual((int)primera[i], (int)primera[i - 1]);
            Assert.AreEqual(EtapaPresentacionArena.Video, primera[0]);
            Assert.AreEqual(EtapaPresentacionArena.Regreso, primera[primera.Count - 1]);
        }
    }
}
