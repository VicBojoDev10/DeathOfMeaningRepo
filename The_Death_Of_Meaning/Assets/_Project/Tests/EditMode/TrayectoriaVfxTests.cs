using NUnit.Framework;
using TDOM.Gameplay.Vfx;
using UnityEngine;

namespace TDOM.Tests.EditMode
{
    public class TrayectoriaVfxTests
    {
        private const float Delta = 0.001f;

        private static readonly Vector3 A = Vector3.zero;
        private static readonly Vector3 B = new Vector3(0f, 0f, 4f);

        private static void IgualVector(Vector3 esperado, Vector3 real)
        {
            Assert.AreEqual(esperado.x, real.x, Delta);
            Assert.AreEqual(esperado.y, real.y, Delta);
            Assert.AreEqual(esperado.z, real.z, Delta);
        }

        [Test]
        public void el_inicio_de_la_curva_es_el_punto_a()
        {
            var control = TrayectoriaVfx.Control(A, B, Vector3.up, 0.5f, 0f);

            IgualVector(A, TrayectoriaVfx.Punto(A, control, B, 0f));
        }

        [Test]
        public void el_final_de_la_curva_es_el_punto_b()
        {
            var control = TrayectoriaVfx.Control(A, B, Vector3.up, 0.5f, 0f);

            IgualVector(B, TrayectoriaVfx.Punto(A, control, B, 1f));
        }

        [Test]
        public void sin_curvatura_la_mitad_queda_en_la_linea_recta()
        {
            var control = TrayectoriaVfx.Control(A, B, Vector3.up, 0f, 0f);

            IgualVector(new Vector3(0f, 0f, 2f), TrayectoriaVfx.Punto(A, control, B, 0.5f));
        }

        [Test]
        public void la_curvatura_positiva_levanta_la_mitad_hacia_arriba()
        {
            var control = TrayectoriaVfx.Control(A, B, Vector3.up, 0.5f, 0f);

            IgualVector(new Vector3(0f, 1f, 2f), TrayectoriaVfx.Punto(A, control, B, 0.5f));
        }

        [Test]
        public void la_curvatura_negativa_baja_la_mitad()
        {
            var control = TrayectoriaVfx.Control(A, B, Vector3.up, -0.5f, 0f);

            IgualVector(new Vector3(0f, -1f, 2f), TrayectoriaVfx.Punto(A, control, B, 0.5f));
        }

        [Test]
        public void rotar_noventa_grados_lleva_la_curva_hacia_un_lado()
        {
            var control = TrayectoriaVfx.Control(A, B, Vector3.up, 0.5f, 90f);
            var mitad = TrayectoriaVfx.Punto(A, control, B, 0.5f);

            Assert.AreEqual(0f, mitad.y, Delta);
            Assert.AreEqual(1f, Mathf.Abs(mitad.x), Delta);
        }

        [Test]
        public void rotar_ciento_ochenta_grados_invierte_la_curva()
        {
            var control = TrayectoriaVfx.Control(A, B, Vector3.up, 0.5f, 180f);

            IgualVector(new Vector3(0f, -1f, 2f), TrayectoriaVfx.Punto(A, control, B, 0.5f));
        }

        [Test]
        public void la_curvatura_escala_con_la_distancia_entre_puntos()
        {
            var lejos = new Vector3(0f, 0f, 8f);
            var control = TrayectoriaVfx.Control(A, lejos, Vector3.up, 0.5f, 0f);

            IgualVector(new Vector3(0f, 2f, 4f), TrayectoriaVfx.Punto(A, control, lejos, 0.5f));
        }

        [Test]
        public void el_valor_t_fuera_de_rango_se_limita_a_los_extremos()
        {
            var control = TrayectoriaVfx.Control(A, B, Vector3.up, 0.5f, 0f);

            IgualVector(A, TrayectoriaVfx.Punto(A, control, B, -1f));
            IgualVector(B, TrayectoriaVfx.Punto(A, control, B, 2f));
        }

        [Test]
        public void la_tangente_inicial_apunta_al_punto_de_control()
        {
            var control = TrayectoriaVfx.Control(A, B, Vector3.up, 0.5f, 0f);
            var tangente = TrayectoriaVfx.Tangente(A, control, B, 0f);

            IgualVector((control - A).normalized, tangente.normalized);
        }

        [Test]
        public void sin_curvatura_la_tangente_va_de_a_hacia_b()
        {
            var control = TrayectoriaVfx.Control(A, B, Vector3.up, 0f, 0f);

            IgualVector(Vector3.forward, TrayectoriaVfx.Tangente(A, control, B, 0.3f).normalized);
        }

        [Test]
        public void arriba_paralelo_al_trayecto_no_rompe_la_normal()
        {
            var alto = new Vector3(0f, 4f, 0f);
            var normal = TrayectoriaVfx.Normal(A, alto, Vector3.up, 0f);

            Assert.AreEqual(1f, normal.magnitude, Delta);
            Assert.AreEqual(0f, Vector3.Dot(normal, Vector3.up), Delta);
        }

        [Test]
        public void puntos_iguales_dejan_el_control_en_el_mismo_punto()
        {
            var control = TrayectoriaVfx.Control(B, B, Vector3.up, 0.5f, 0f);

            IgualVector(B, control);
        }

        [Test]
        public void la_curvatura_se_recupera_desde_el_punto_de_control()
        {
            var control = TrayectoriaVfx.Control(A, B, Vector3.up, 0.35f, 40f);

            Assert.AreEqual(0.35f, TrayectoriaVfx.Curvatura(A, B, Vector3.up, 40f, control), Delta);
        }
    }
}
