using NUnit.Framework;
using TDOM.Gameplay.Vfx;
using UnityEngine;

namespace TDOM.Tests.EditMode
{
    public class ColorPolvoSueloTests
    {
        private const float Delta = 0.001f;

        private static readonly Color Cafe = new Color(0.5f, 0.3f, 0.1f, 0.2f);
        private static readonly Color Gris = new Color(0.6f, 0.6f, 0.6f, 1f);

        [Test]
        public void con_suelo_y_sin_aclarado_usa_el_color_del_suelo()
        {
            Color resultado = ColorPolvoSuelo.Calcular(true, Cafe, Gris, 0f);

            Assert.AreEqual(Cafe.r, resultado.r, Delta);
            Assert.AreEqual(Cafe.g, resultado.g, Delta);
            Assert.AreEqual(Cafe.b, resultado.b, Delta);
        }

        [Test]
        public void sin_suelo_usa_el_color_de_respaldo()
        {
            Color resultado = ColorPolvoSuelo.Calcular(false, Cafe, Gris, 0f);

            Assert.AreEqual(Gris.r, resultado.r, Delta);
        }

        [Test]
        public void el_aclarado_a_la_mitad_acerca_el_color_al_blanco()
        {
            Color resultado = ColorPolvoSuelo.Calcular(true, Cafe, Gris, 0.5f);

            Assert.AreEqual(0.75f, resultado.r, Delta);
            Assert.AreEqual(0.65f, resultado.g, Delta);
            Assert.AreEqual(0.55f, resultado.b, Delta);
        }

        [Test]
        public void el_polvo_siempre_es_opaco_aunque_el_suelo_tenga_transparencia()
        {
            Color resultado = ColorPolvoSuelo.Calcular(true, Cafe, Gris, 0f);

            Assert.AreEqual(1f, resultado.a, Delta);
        }

        [Test]
        public void el_aclarado_fuera_de_rango_se_limita()
        {
            Color resultado = ColorPolvoSuelo.Calcular(true, Cafe, Gris, 3f);

            Assert.AreEqual(1f, resultado.r, Delta);
            Assert.AreEqual(1f, resultado.b, Delta);
        }
    }
}
