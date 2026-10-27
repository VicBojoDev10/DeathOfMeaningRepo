using NUnit.Framework;
using TDOM.Gameplay.Combat;

namespace TDOM.Tests.EditMode
{
    public class LineaTiempoManotazoTests
    {
        private const float Tolerancia = 0.001f;

        // Burbuja 0-0.9 s, salida 0.9-1.5 s, barrido 1.5-2.0 s, retracción 2.0-2.2 s.
        private LineaTiempoManotazo linea;

        [SetUp]
        public void Setup()
        {
            linea = new LineaTiempoManotazo(1.5f, 0.6f, 0.5f, 0.2f);
        }

        [Test]
        public void La_duracion_es_aviso_mas_barrido_mas_retraccion()
        {
            Assert.AreEqual(2.2f, linea.Duracion, Tolerancia);
        }

        [Test]
        public void Al_empezar_hay_burbuja_sin_columna()
        {
            EstadoManotazo estado = linea.Evaluar(0f, 1);

            Assert.AreEqual(FaseManotazo.Burbuja, estado.Fase);
            Assert.AreEqual(0f, estado.Longitud, Tolerancia);
            Assert.AreEqual(0f, estado.Burbuja, Tolerancia);
            Assert.AreEqual(-90f, estado.Angulo, Tolerancia);
        }

        [Test]
        public void La_burbuja_crece_durante_su_tramo_sin_columna()
        {
            EstadoManotazo estado = linea.Evaluar(0.45f, 1);

            Assert.AreEqual(FaseManotazo.Burbuja, estado.Fase);
            Assert.AreEqual(0.5f, estado.Burbuja, Tolerancia);
            Assert.AreEqual(0f, estado.Longitud, Tolerancia);
        }

        [Test]
        public void La_columna_sale_en_el_resto_del_aviso()
        {
            EstadoManotazo estado = linea.Evaluar(1.2f, 1);

            Assert.AreEqual(FaseManotazo.Salida, estado.Fase);
            Assert.AreEqual(0.75f, estado.Longitud, Tolerancia);
            Assert.AreEqual(1f, estado.Burbuja, Tolerancia);
            Assert.AreEqual(-90f, estado.Angulo, Tolerancia);
        }

        [Test]
        public void La_salida_nunca_retrocede()
        {
            float anterior = 0f;
            for (float t = 0f; t < 1.49f; t += 0.01f)
            {
                float longitud = linea.Evaluar(t, 1).Longitud;
                Assert.GreaterOrEqual(longitud, anterior - Tolerancia);
                anterior = longitud;
            }
        }

        [Test]
        public void El_barrido_recorre_de_menos_90_a_mas_90_con_la_columna_completa()
        {
            EstadoManotazo inicio = linea.Evaluar(1.5001f, 1);
            EstadoManotazo medio = linea.Evaluar(1.75f, 1);
            EstadoManotazo final = linea.Evaluar(1.9999f, 1);

            Assert.AreEqual(FaseManotazo.Barrido, medio.Fase);
            Assert.AreEqual(1f, medio.Longitud, Tolerancia);
            Assert.AreEqual(-90f, inicio.Angulo, 0.1f);
            Assert.AreEqual(0f, medio.Angulo, Tolerancia);
            Assert.AreEqual(90f, final.Angulo, 0.1f);
        }

        [Test]
        public void Con_sentido_negativo_el_barrido_va_al_reves()
        {
            Assert.AreEqual(90f, linea.Evaluar(0f, -1).Angulo, Tolerancia);
            Assert.AreEqual(90f, linea.Evaluar(1.5001f, -1).Angulo, 0.1f);
            Assert.AreEqual(0f, linea.Evaluar(1.75f, -1).Angulo, Tolerancia);
            Assert.AreEqual(-90f, linea.Evaluar(1.9999f, -1).Angulo, 0.1f);
            Assert.AreEqual(-90f, linea.Evaluar(2.1f, -1).Angulo, Tolerancia);
        }

        [Test]
        public void El_dano_solo_esta_activo_durante_el_barrido()
        {
            Assert.IsFalse(linea.Evaluar(0.3f, 1).DanoActivo);
            Assert.IsFalse(linea.Evaluar(1.2f, 1).DanoActivo);
            Assert.IsTrue(linea.Evaluar(1.75f, 1).DanoActivo);
            Assert.IsFalse(linea.Evaluar(2.1f, 1).DanoActivo);
            Assert.IsFalse(linea.Evaluar(5f, 1).DanoActivo);
        }

        [Test]
        public void La_retraccion_repite_la_salida_al_reves_y_apaga_la_burbuja()
        {
            EstadoManotazo estado = linea.Evaluar(2.1f, 1);

            Assert.AreEqual(FaseManotazo.Retraccion, estado.Fase);
            Assert.AreEqual(0.75f, estado.Longitud, Tolerancia);
            Assert.AreEqual(0.5f, estado.Burbuja, Tolerancia);
            Assert.AreEqual(90f, estado.Angulo, Tolerancia);
        }

        [Test]
        public void La_retraccion_nunca_crece()
        {
            float anterior = 1f;
            for (float t = 2.001f; t < 2.19f; t += 0.005f)
            {
                float longitud = linea.Evaluar(t, 1).Longitud;
                Assert.LessOrEqual(longitud, anterior + Tolerancia);
                anterior = longitud;
            }
        }

        [Test]
        public void Al_terminar_no_queda_columna_ni_burbuja()
        {
            EstadoManotazo estado = linea.Evaluar(3f, 1);

            Assert.AreEqual(FaseManotazo.Terminado, estado.Fase);
            Assert.AreEqual(0f, estado.Longitud, Tolerancia);
            Assert.AreEqual(0f, estado.Burbuja, Tolerancia);
        }

        [Test]
        public void Un_tiempo_negativo_se_trata_como_el_inicio()
        {
            Assert.AreEqual(FaseManotazo.Burbuja, linea.Evaluar(-1f, 1).Fase);
        }

        [Test]
        public void Sin_tramo_de_burbuja_la_columna_sale_desde_el_primer_instante()
        {
            var sinBurbuja = new LineaTiempoManotazo(1.5f, 0f, 0.5f, 0.2f);

            EstadoManotazo estado = sinBurbuja.Evaluar(0.75f, 1);

            Assert.AreEqual(FaseManotazo.Salida, estado.Fase);
            Assert.AreEqual(0.75f, estado.Longitud, Tolerancia);
        }
    }
}
