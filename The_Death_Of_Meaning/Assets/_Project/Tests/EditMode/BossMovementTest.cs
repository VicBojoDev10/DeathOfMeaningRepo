using System.Collections.Generic;
using NUnit.Framework;
using TDOM.Gameplay.Combat;

namespace TDOM.Tests.EditMode
{
    public class BossMovementTest
    {
        private const float Dt = 1f / 60f;
        private readonly BossMovement _movimiento = new BossMovement();

        [Test]
        public void fase_uno_no_se_mueve_aunque_tenga_velocidad_configurada()
        {
            Avanzar(1, false, 4f, out float x, out float z);

            Assert.AreEqual(0f, x);
            Assert.AreEqual(0f, z);
        }

        [Test]
        public void fase_dos_avanza_hacia_el_jugador_mas_cercano_con_dt_fijo()
        {
            var candidatos = new List<BossTargetCandidate>
            {
                new BossTargetCandidate(1, 8f, 0f),
                new BossTargetCandidate(2, 3f, 0f),
            };
            Assert.IsTrue(_movimiento.TryElegirObjetivo(candidatos, null, out ulong objetivoId));
            Assert.AreEqual(2UL, objetivoId);

            _movimiento.Avanzar(
                2,
                false,
                0f,
                0f,
                0f,
                10f,
                6f,
                5f,
                Dt,
                out float x,
                out float z
            );

            Assert.AreEqual(0f, x, 0.00001f);
            Assert.AreEqual(0.1f, z, 0.00001f);
        }

        [Test]
        public void se_detiene_a_5f_del_objetivo_y_no_lo_sobrepasa()
        {
            _movimiento.Avanzar(
                2,
                false,
                0f,
                0f,
                0f,
                10f,
                6f,
                5f,
                1f,
                out float x,
                out float z
            );

            Assert.AreEqual(0f, x, 0.00001f);
            Assert.AreEqual(5f, z, 0.00001f);
        }

        [Test]
        public void con_zendre_anclado_elige_a_zendre_y_se_detiene()
        {
            var candidatos = new List<BossTargetCandidate>
            {
                new BossTargetCandidate(1, 8f, 0f),
                new BossTargetCandidate(2, 3f, 0f),
            };
            Assert.IsTrue(_movimiento.TryElegirObjetivo(candidatos, 1UL, out ulong objetivoId));
            Assert.AreEqual(1UL, objetivoId);

            Avanzar(2, true, 6f, out float x, out float z);

            Assert.AreEqual(0f, x);
            Assert.AreEqual(0f, z);
        }

        private void Avanzar(int fase, bool zendreAnclado, float velocidad, out float x, out float z)
        {
            _movimiento.Avanzar(
                fase,
                zendreAnclado,
                0f,
                0f,
                0f,
                10f,
                velocidad,
                5f,
                Dt,
                out x,
                out z
            );
        }
    }
}
