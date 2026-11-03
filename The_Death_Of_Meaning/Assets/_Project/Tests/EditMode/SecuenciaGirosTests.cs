using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TDOM.Gameplay.Combat;

namespace TDOM.Tests.EditMode
{
    public class SecuenciaGirosTests
    {
        private ConfigSecuenciaGiros _config;
        private SecuenciaGiros _secuencia;

        [SetUp]
        public void Setup()
        {
            _config = new ConfigSecuenciaGiros();
            _secuencia = new SecuenciaGiros(_config);
        }

        // Simula al Animator: avanza la posicion del clip y obedece los saltos.
        private List<EventoGiros> Simular(float ratio, float paso = 0.02f, int maxFrames = 5000)
        {
            var todos = new List<EventoGiros>();
            var salida = new List<EventoGiros>();
            _secuencia.Iniciar(ratio, salida);
            float p = salida[0].Destino;
            todos.AddRange(salida);

            for (int i = 0; i < maxFrames && _secuencia.Activa; i++)
            {
                p += paso;
                salida.Clear();
                _secuencia.Actualizar(p, salida);
                foreach (var e in salida)
                {
                    todos.Add(e);
                    if (e.Tipo == TipoEventoGiros.Salto)
                        p = e.Destino;
                }
            }
            return todos;
        }

        [Test]
        public void Los_giros_dependen_de_la_carga_entre_el_minimo_y_el_maximo()
        {
            Assert.AreEqual(2, _secuencia.GirosPara(0f));
            Assert.AreEqual(2, _secuencia.GirosPara(_config.RatioCargaMinimo));
            Assert.AreEqual(4, _secuencia.GirosPara(1f));
            Assert.AreEqual(3, _secuencia.GirosPara(0.65f));
        }

        [Test]
        public void Iniciar_salta_al_inicio_del_primer_giro()
        {
            var salida = new List<EventoGiros>();
            _secuencia.Iniciar(1f, salida);

            Assert.IsTrue(_secuencia.Activa);
            Assert.AreEqual(1, salida.Count);
            Assert.AreEqual(TipoEventoGiros.Salto, salida[0].Tipo);
            Assert.AreEqual(_config.InicioGiro, salida[0].Destino, 1e-5f);
            Assert.AreEqual(_config.MezclaEntrada, salida[0].Mezcla, 1e-5f);
        }

        [Test]
        public void Hay_un_impacto_por_giro_mas_uno_en_el_tramo_final()
        {
            var eventos = Simular(1f);
            var impactos = eventos.Where(e => e.Tipo == TipoEventoGiros.Impacto).ToList();

            Assert.AreEqual(5, impactos.Count, "4 giros + 1 final");
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4 }, impactos.Select(e => e.Indice));
            Assert.IsTrue(impactos.Last().EsFinal);
            Assert.IsTrue(impactos.Take(4).All(e => !e.EsFinal));
        }

        [Test]
        public void Con_carga_minima_hace_dos_giros()
        {
            var eventos = Simular(0.3f);
            int impactos = eventos.Count(e => e.Tipo == TipoEventoGiros.Impacto);
            Assert.AreEqual(3, impactos, "2 giros + 1 final");
        }

        [Test]
        public void Cada_giro_repetido_salta_de_vuelta_al_inicio_del_giro()
        {
            var eventos = Simular(1f);
            var saltosDeGiro = eventos
                .Where(e => e.Tipo == TipoEventoGiros.Salto)
                .Skip(1) // el de entrada
                .ToList();

            Assert.AreEqual(3, saltosDeGiro.Count, "uno por cada giro que se repite");
            foreach (var s in saltosDeGiro)
            {
                Assert.AreEqual(_config.InicioGiro, s.Destino, 0.03f);
                Assert.AreEqual(_config.MezclaGiro, s.Mezcla, 1e-5f);
            }
        }

        [Test]
        public void Si_el_final_empieza_donde_acaba_el_giro_no_hay_salto_extra()
        {
            _config.InicioFinal = _config.FinGiro;
            var eventos = Simular(0.3f);
            int saltos = eventos.Count(e => e.Tipo == TipoEventoGiros.Salto);
            Assert.AreEqual(2, saltos, "entrada + 1 repeticion (2 giros)");
        }

        [Test]
        public void Si_el_final_empieza_en_otro_punto_hay_un_salto_con_la_mezcla_final()
        {
            _config.InicioFinal = 0.6f;
            _config.FinGiro = 0.5f;
            var eventos = Simular(0.3f);
            var saltos = eventos.Where(e => e.Tipo == TipoEventoGiros.Salto).ToList();

            Assert.AreEqual(3, saltos.Count);
            Assert.AreEqual(0.6f, saltos.Last().Destino, 0.03f);
            Assert.AreEqual(_config.MezclaFinal, saltos.Last().Mezcla, 1e-5f);
        }

        [Test]
        public void La_secuencia_termina_con_un_solo_Fin_y_queda_inactiva()
        {
            var eventos = Simular(1f);
            Assert.AreEqual(1, eventos.Count(e => e.Tipo == TipoEventoGiros.Fin));
            Assert.AreEqual(TipoEventoGiros.Fin, eventos.Last().Tipo);
            Assert.AreEqual(_config.MezclaSalida, eventos.Last().Mezcla, 1e-5f);
            Assert.IsFalse(_secuencia.Activa);

            var salida = new List<EventoGiros>();
            _secuencia.Actualizar(0.99f, salida);
            Assert.IsEmpty(salida);
        }

        [Test]
        public void Cancelar_detiene_la_secuencia_sin_eventos()
        {
            var salida = new List<EventoGiros>();
            _secuencia.Iniciar(1f, salida);
            salida.Clear();

            _secuencia.Cancelar();
            _secuencia.Actualizar(0.99f, salida);

            Assert.IsFalse(_secuencia.Activa);
            Assert.IsEmpty(salida);
        }

        [Test]
        public void Un_tiron_de_frame_conserva_el_ritmo_y_no_pierde_el_impacto()
        {
            var salida = new List<EventoGiros>();
            _secuencia.Iniciar(1f, salida);
            salida.Clear();

            // De golpe el clip pasa del impacto al final del primer giro.
            _secuencia.Actualizar(0.60f, salida);

            Assert.AreEqual(1, salida.Count(e => e.Tipo == TipoEventoGiros.Impacto));
            Assert.AreEqual(0, salida.First(e => e.Tipo == TipoEventoGiros.Impacto).Indice);
            var salto = salida.Single(e => e.Tipo == TipoEventoGiros.Salto);
            Assert.AreEqual(0.15f, salto.Destino, 1e-4f, "inicio del giro + lo que se paso");
            Assert.IsTrue(_secuencia.Activa);
        }

        [Test]
        public void Una_config_desordenada_se_corrige_y_termina()
        {
            _config.InicioGiro = 0.5f;
            _config.FinGiro = 0.2f;
            _config.ImpactoGiro = 0.9f;
            _config.InicioFinal = 0.7f;
            _config.FinFinal = 0.1f;
            _config.GirosMinimos = 0;
            _config.GirosMaximos = 0;

            var eventos = Simular(1f);

            Assert.IsFalse(_secuencia.Activa);
            Assert.AreEqual(TipoEventoGiros.Fin, eventos.Last().Tipo);
            Assert.AreEqual(1, _secuencia.GirosTotales);
        }

        [Test]
        public void Los_valores_del_Inspector_se_leen_al_iniciar_cada_secuencia()
        {
            Simular(0.3f);
            _config.GirosMinimos = 5;
            _config.GirosMaximos = 5;

            var eventos = Simular(0.3f);

            Assert.AreEqual(6, eventos.Count(e => e.Tipo == TipoEventoGiros.Impacto));
        }
    }
}
