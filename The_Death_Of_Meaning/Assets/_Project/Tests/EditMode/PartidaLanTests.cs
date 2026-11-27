using System.Collections.Generic;
using NUnit.Framework;
using TDOM.Gameplay;

namespace TDOM.Tests.EditMode
{
    public class PartidaLanTests
    {
        private const float Caducidad = 3f;
        private const float Dt60 = 1f / 60f;
        private const string Ip = "192.168.1.20";

        [Test]
        public void un_anuncio_codificado_se_decodifica_con_la_ip_de_origen()
        {
            string mensaje = AnuncioPartidaLan.Codificar("PC-Ayla", 7777);

            Assert.IsTrue(AnuncioPartidaLan.TryDecodificar(mensaje, Ip, out var partida));
            Assert.AreEqual(new PartidaLan("PC-Ayla", Ip, 7777), partida);
        }

        [TestCase("")]
        [TestCase("OTRO-JUEGO|1|7777|PC")]
        [TestCase("TDOM-LAN|1|puerto|PC")]
        [TestCase("TDOM-LAN|1|7777")]
        public void los_mensajes_ajenos_o_rotos_se_ignoran(string mensaje)
        {
            Assert.IsFalse(AnuncioPartidaLan.TryDecodificar(mensaje, Ip, out _));
        }

        [Test]
        public void el_nombre_no_rompe_el_formato_aunque_traiga_separadores()
        {
            string mensaje = AnuncioPartidaLan.Codificar("PC|Raro", 7777);

            Assert.IsTrue(AnuncioPartidaLan.TryDecodificar(mensaje, Ip, out var partida));
            Assert.AreEqual(7777, partida.Puerto);
        }

        [Test]
        public void la_misma_partida_anunciada_dos_veces_aparece_una_sola_vez()
        {
            var lista = new ListaPartidasLan(Caducidad);
            var partida = new PartidaLan("PC", Ip, 7777);

            Assert.IsTrue(lista.Registrar(partida));
            Assert.IsFalse(lista.Registrar(partida));
            Assert.AreEqual(1, lista.Partidas.Count);
        }

        [Test]
        public void una_partida_que_deja_de_anunciarse_desaparece_al_caducar()
        {
            var lista = new ListaPartidasLan(Caducidad);
            lista.Registrar(new PartidaLan("PC", Ip, 7777));

            Assert.IsFalse(lista.Tick(Caducidad));
            Assert.IsTrue(lista.Tick(Dt60));
            Assert.AreEqual(0, lista.Partidas.Count);
        }

        [Test]
        public void volver_a_anunciarse_reinicia_la_caducidad()
        {
            var lista = new ListaPartidasLan(Caducidad);
            var partida = new PartidaLan("PC", Ip, 7777);
            lista.Registrar(partida);

            lista.Tick(Caducidad - Dt60);
            lista.Registrar(partida);
            lista.Tick(Caducidad - Dt60);

            Assert.AreEqual(1, lista.Partidas.Count);
        }

        [Test]
        public void con_dt_fijo_dos_simulaciones_caducan_en_el_mismo_cuadro()
        {
            int Simular()
            {
                var lista = new ListaPartidasLan(Caducidad);
                lista.Registrar(new PartidaLan("PC", Ip, 7777));
                for (int cuadro = 1; cuadro < 1000; cuadro++)
                    if (lista.Tick(Dt60))
                        return cuadro;
                return -1;
            }

            int primera = Simular();
            Assert.Greater(primera, 0);
            Assert.AreEqual(primera, Simular());
        }

        [Test]
        public void limpiar_vacia_la_lista()
        {
            var lista = new ListaPartidasLan(Caducidad);
            lista.Registrar(new PartidaLan("PC", Ip, 7777));

            Assert.IsTrue(lista.Limpiar());
            Assert.AreEqual(new List<PartidaLan>(), new List<PartidaLan>(lista.Partidas));
        }
    }
}
