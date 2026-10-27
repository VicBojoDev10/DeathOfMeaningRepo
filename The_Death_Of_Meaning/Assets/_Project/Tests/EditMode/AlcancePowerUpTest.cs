using System.Collections.Generic;
using NUnit.Framework;
using TDOM.Contracts;
using TDOM.Gameplay.Combat;

namespace TDOM.Tests.EditMode
{
    public class AlcancePowerUpTest
    {
        private const ulong Ayla = 1;
        private const ulong Zendre = 2;

        private readonly List<ulong> _ids = new List<ulong>();

        private static EstadoJugadorPowerUp Jugador(ulong id, bool vivo, bool necesita) =>
            new EstadoJugadorPowerUp(id, vivo, necesita);

        private bool Resolver(
            ModoAlcancePowerUp alcance,
            ulong quienToco,
            params EstadoJugadorPowerUp[] jugadores
        ) => AlcancePowerUp.Resolver(alcance, quienToco, jugadores, _ids);

        [Test]
        public void individual_cura_solo_a_quien_lo_toca_y_se_consume()
        {
            bool consumido = Resolver(
                ModoAlcancePowerUp.QuienLoToca,
                Ayla,
                Jugador(Ayla, true, true),
                Jugador(Zendre, true, true)
            );

            Assert.IsTrue(consumido);
            CollectionAssert.AreEqual(new[] { Ayla }, _ids);
        }

        [Test]
        public void individual_con_el_jugador_lleno_no_se_consume()
        {
            bool consumido = Resolver(
                ModoAlcancePowerUp.QuienLoToca,
                Ayla,
                Jugador(Ayla, true, false),
                Jugador(Zendre, true, true)
            );

            Assert.IsFalse(consumido);
            Assert.IsEmpty(_ids);
        }

        [Test]
        public void grupal_cura_a_ambos_si_los_dos_lo_necesitan()
        {
            bool consumido = Resolver(
                ModoAlcancePowerUp.AmbosJugadores,
                Ayla,
                Jugador(Ayla, true, true),
                Jugador(Zendre, true, true)
            );

            Assert.IsTrue(consumido);
            CollectionAssert.AreEquivalent(new[] { Ayla, Zendre }, _ids);
        }

        [Test]
        public void grupal_cura_al_companero_aunque_quien_toca_tenga_la_vida_llena()
        {
            bool consumido = Resolver(
                ModoAlcancePowerUp.AmbosJugadores,
                Ayla,
                Jugador(Ayla, true, false),
                Jugador(Zendre, true, true)
            );

            Assert.IsTrue(consumido);
            CollectionAssert.AreEqual(new[] { Zendre }, _ids);
        }

        [Test]
        public void grupal_con_los_dos_llenos_no_se_consume()
        {
            bool consumido = Resolver(
                ModoAlcancePowerUp.AmbosJugadores,
                Ayla,
                Jugador(Ayla, true, false),
                Jugador(Zendre, true, false)
            );

            Assert.IsFalse(consumido);
            Assert.IsEmpty(_ids);
        }

        [Test]
        public void un_jugador_abatido_nunca_lo_recibe_en_grupal()
        {
            bool consumido = Resolver(
                ModoAlcancePowerUp.AmbosJugadores,
                Ayla,
                Jugador(Ayla, true, true),
                Jugador(Zendre, false, true)
            );

            Assert.IsTrue(consumido);
            CollectionAssert.AreEqual(new[] { Ayla }, _ids);
        }

        [Test]
        public void un_jugador_abatido_nunca_lo_recibe_en_individual()
        {
            bool consumido = Resolver(
                ModoAlcancePowerUp.QuienLoToca,
                Ayla,
                Jugador(Ayla, false, true),
                Jugador(Zendre, true, true)
            );

            Assert.IsFalse(consumido);
            Assert.IsEmpty(_ids);
        }

        [Test]
        public void individual_de_un_jugador_que_no_esta_en_la_lista_no_se_consume()
        {
            bool consumido = Resolver(
                ModoAlcancePowerUp.QuienLoToca,
                99,
                Jugador(Ayla, true, true),
                Jugador(Zendre, true, true)
            );

            Assert.IsFalse(consumido);
            Assert.IsEmpty(_ids);
        }

        [Test]
        public void la_lista_de_resultado_se_limpia_en_cada_llamada()
        {
            Resolver(
                ModoAlcancePowerUp.AmbosJugadores,
                Ayla,
                Jugador(Ayla, true, true),
                Jugador(Zendre, true, true)
            );

            Resolver(ModoAlcancePowerUp.AmbosJugadores, Ayla, Jugador(Ayla, true, false));

            Assert.IsEmpty(_ids);
        }
    }
}
