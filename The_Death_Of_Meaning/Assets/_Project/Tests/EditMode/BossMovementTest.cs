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

            _movimiento.Avanzar(2, false, 0f, 0f, 0f, 10f, 6f, 5f, Dt, out float x, out float z);

            Assert.AreEqual(0f, x, 0.00001f);
            Assert.AreEqual(0.1f, z, 0.00001f);
        }

        [Test]
        public void se_detiene_a_5f_del_objetivo_y_no_lo_sobrepasa()
        {
            _movimiento.Avanzar(2, false, 0f, 0f, 0f, 10f, 6f, 5f, 1f, out float x, out float z);

            Assert.AreEqual(0f, x, 0.00001f);
            Assert.AreEqual(5f, z, 0.00001f);
        }

        [Test]
        public void limita_la_posicion_al_circulo_interior_de_la_arena()
        {
            _movimiento.LimitarDentroDeCirculo(
                0f,
                12f,
                0f,
                0f,
                10f,
                out float x,
                out float z
            );

            Assert.AreEqual(0f, x, 0.00001f);
            Assert.AreEqual(10f, z, 0.00001f);
        }

        [Test]
        public void el_radio_seguro_reserva_el_margen_y_el_tamano_del_boss()
        {
            float radioSeguro = BossMovement.CalcularRadioSeguro(50f, 3f, 2f);

            Assert.AreEqual(45f, radioSeguro, 0.00001f);
        }

        [Test]
        public void conserva_el_primer_objetivo_mientras_siga_en_el_radio()
        {
            var logica = new BossMovement();
            var candidatos = new List<BossTargetCandidate>
            {
                new BossTargetCandidate(1, 4f, 0f),
                new BossTargetCandidate(2, 6f, 0f),
            };
            Assert.IsTrue(logica.TryElegirObjetivo(candidatos, null, out ulong primero));
            Assert.AreEqual(1UL, primero);

            candidatos[0] = new BossTargetCandidate(1, 7f, 0f);
            candidatos[1] = new BossTargetCandidate(2, 2f, 0f);
            Assert.IsTrue(logica.TryElegirObjetivo(candidatos, null, out ulong fijado));
            Assert.AreEqual(1UL, fijado);
        }

        [Test]
        public void el_ancla_prioriza_a_zendre_temporalmente_y_luego_recupera_el_objetivo_fijado()
        {
            var logica = new BossMovement();
            var candidatos = new List<BossTargetCandidate>
            {
                new BossTargetCandidate(1, 4f, 0f),
                new BossTargetCandidate(2, 6f, 0f),
            };

            Assert.IsTrue(logica.TryElegirObjetivo(candidatos, null, out ulong primero));
            Assert.AreEqual(1UL, primero);
            Assert.IsTrue(logica.TryElegirObjetivo(candidatos, 2UL, out ulong anclado));
            Assert.AreEqual(2UL, anclado);
            Assert.IsTrue(logica.TryElegirObjetivo(candidatos, null, out ulong trasRomperAncla));
            Assert.AreEqual(1UL, trasRomperAncla);
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

        private void Avanzar(
            int fase,
            bool zendreAnclado,
            float velocidad,
            out float x,
            out float z
        )
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
