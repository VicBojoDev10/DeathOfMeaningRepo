using NUnit.Framework;
using TDOM.Gameplay.Vfx;

namespace TDOM.Tests.EditMode
{
    public class DetectorFinEmbestidaTests
    {
        private const float Dt = 1f / 60f;
        private const float DuracionEmbestida = 0.8f;

        private DetectorFinEmbestida _detector;

        [SetUp]
        public void Preparar()
        {
            _detector = new DetectorFinEmbestida();
        }

        [Test]
        public void sin_embestida_no_termina_nada()
        {
            _detector.Actualizar(false);
            _detector.Actualizar(false);

            Assert.IsFalse(_detector.Termino);
        }

        [Test]
        public void durante_la_embestida_no_termina()
        {
            _detector.Actualizar(true);
            _detector.Actualizar(true);

            Assert.IsFalse(_detector.Termino);
        }

        [Test]
        public void termina_en_el_cuadro_en_que_deja_de_estar_activa()
        {
            _detector.Actualizar(true);
            _detector.Actualizar(false);

            Assert.IsTrue(_detector.Termino);
        }

        [Test]
        public void el_fin_solo_dura_un_cuadro()
        {
            _detector.Actualizar(true);
            _detector.Actualizar(false);
            _detector.Actualizar(false);

            Assert.IsFalse(_detector.Termino);
        }

        [Test]
        public void cada_embestida_seguida_tiene_su_propio_fin()
        {
            int fines = 0;
            for (int repeticion = 0; repeticion < 2; repeticion++)
            {
                _detector.Actualizar(true);
                fines += _detector.Termino ? 1 : 0;
                _detector.Actualizar(false);
                fines += _detector.Termino ? 1 : 0;
            }

            Assert.AreEqual(2, fines);
        }

        [Test]
        public void una_embestida_completa_con_dt_fijo_termina_una_sola_vez()
        {
            int fines = 0;
            for (float tiempo = 0f; tiempo < DuracionEmbestida * 2f; tiempo += Dt)
            {
                _detector.Actualizar(tiempo < DuracionEmbestida);
                fines += _detector.Termino ? 1 : 0;
            }

            Assert.AreEqual(1, fines);
        }
    }
}
