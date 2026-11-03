using System.Collections.Generic;
using NUnit.Framework;
using TDOM.Gameplay.Combat;

namespace TDOM.Tests.EditMode
{
    public class BossTargetSelectorTest
    {
        private const ulong JugadorA = 1;
        private const ulong JugadorB = 2;
        private const float RadioDeteccion = 40f;
        private const float FactorAgro = 1.5f;
        private const float RangoAtaque = 25f;

        private static MotivoObjetivo Elegir(
            out ulong objetivo,
            float rangoAtaque,
            params BossTargetCandidate[] candidatos
        )
        {
            var selector = new BossTargetSelector(RadioDeteccion, FactorAgro);
            return selector.Elegir(
                new List<BossTargetCandidate>(candidatos),
                rangoAtaque,
                out objetivo
            );
        }

        [Test]
        public void sin_jugadores_no_hay_objetivo()
        {
            var motivo = Elegir(out _, RangoAtaque);

            Assert.AreEqual(MotivoObjetivo.Ninguno, motivo);
        }

        [Test]
        public void nadie_en_rango_del_ataque_no_hay_objetivo()
        {
            var motivo = Elegir(
                out _,
                RangoAtaque,
                new BossTargetCandidate(JugadorA, 30f, 0f),
                new BossTargetCandidate(JugadorB, 35f, 0f)
            );

            Assert.AreEqual(MotivoObjetivo.Ninguno, motivo);
        }

        [Test]
        public void el_radio_de_deteccion_limita_aunque_el_ataque_llegue_mas_lejos()
        {
            var motivo = Elegir(out _, 50f, new BossTargetCandidate(JugadorA, 45f, 0f));

            Assert.AreEqual(MotivoObjetivo.Ninguno, motivo);
        }

        [Test]
        public void solo_a_en_rango_ataca_a_a()
        {
            var motivo = Elegir(
                out ulong objetivo,
                RangoAtaque,
                new BossTargetCandidate(JugadorA, 10f, 0f),
                new BossTargetCandidate(JugadorB, 30f, 100f)
            );

            Assert.AreEqual(MotivoObjetivo.Unico, motivo);
            Assert.AreEqual(JugadorA, objetivo);
        }

        [Test]
        public void solo_b_en_rango_ataca_a_b()
        {
            var motivo = Elegir(
                out ulong objetivo,
                RangoAtaque,
                new BossTargetCandidate(JugadorA, 30f, 0f),
                new BossTargetCandidate(JugadorB, 10f, 0f)
            );

            Assert.AreEqual(MotivoObjetivo.Unico, motivo);
            Assert.AreEqual(JugadorB, objetivo);
        }

        [Test]
        public void ambos_en_rango_sin_agro_ataca_al_mas_cercano()
        {
            var motivo = Elegir(
                out ulong objetivo,
                RangoAtaque,
                new BossTargetCandidate(JugadorA, 20f, 0f),
                new BossTargetCandidate(JugadorB, 8f, 0f)
            );

            Assert.AreEqual(MotivoObjetivo.MasCercano, motivo);
            Assert.AreEqual(JugadorB, objetivo);
        }

        [Test]
        public void ambos_en_rango_y_a_ataca_notablemente_mas_ataca_a_a()
        {
            var motivo = Elegir(
                out ulong objetivo,
                RangoAtaque,
                new BossTargetCandidate(JugadorA, 20f, 30f),
                new BossTargetCandidate(JugadorB, 8f, 10f)
            );

            Assert.AreEqual(MotivoObjetivo.Agro, motivo);
            Assert.AreEqual(JugadorA, objetivo);
        }

        [Test]
        public void ambos_en_rango_y_b_ataca_notablemente_mas_ataca_a_b()
        {
            var motivo = Elegir(
                out ulong objetivo,
                RangoAtaque,
                new BossTargetCandidate(JugadorA, 8f, 5f),
                new BossTargetCandidate(JugadorB, 20f, 20f)
            );

            Assert.AreEqual(MotivoObjetivo.Agro, motivo);
            Assert.AreEqual(JugadorB, objetivo);
        }

        [Test]
        public void si_solo_uno_hizo_danio_cuenta_como_notablemente_mas()
        {
            var motivo = Elegir(
                out ulong objetivo,
                RangoAtaque,
                new BossTargetCandidate(JugadorA, 8f, 0f),
                new BossTargetCandidate(JugadorB, 20f, 1f)
            );

            Assert.AreEqual(MotivoObjetivo.Agro, motivo);
            Assert.AreEqual(JugadorB, objetivo);
        }

        [Test]
        public void con_danio_parecido_ataca_al_mas_cercano()
        {
            var motivo = Elegir(
                out ulong objetivo,
                RangoAtaque,
                new BossTargetCandidate(JugadorA, 20f, 12f),
                new BossTargetCandidate(JugadorB, 8f, 10f)
            );

            Assert.AreEqual(MotivoObjetivo.MasCercano, motivo);
            Assert.AreEqual(JugadorB, objetivo);
        }

        [Test]
        public void el_que_ataca_mas_pero_esta_fuera_de_rango_no_cuenta()
        {
            var motivo = Elegir(
                out ulong objetivo,
                RangoAtaque,
                new BossTargetCandidate(JugadorA, 30f, 100f),
                new BossTargetCandidate(JugadorB, 10f, 0f)
            );

            Assert.AreEqual(MotivoObjetivo.Unico, motivo);
            Assert.AreEqual(JugadorB, objetivo);
        }
    }
}
