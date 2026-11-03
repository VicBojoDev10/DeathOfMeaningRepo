using System.Collections.Generic;
using NUnit.Framework;
using TDOM.Gameplay.Combat;
using UnityEngine;

namespace TDOM.Tests.EditMode
{
    public class MorteroPlacementTest
    {
        private const float RadioArea = 4f;
        private const float Separacion = 2f * RadioArea;
        private const float Delta = 0.01f;

        private static readonly Vector2 Objetivo = new Vector2(10f, 5f);
        private static readonly Vector2[] SinOtros = new Vector2[0];

        private static void AssertSinSolaparse(List<Vector2> puntos)
        {
            for (int i = 0; i < puntos.Count; i++)
            for (int j = i + 1; j < puntos.Count; j++)
                Assert.GreaterOrEqual(
                    Vector2.Distance(puntos[i], puntos[j]),
                    Separacion - Delta,
                    $"Los disparos {i} y {j} se solapan"
                );
        }

        [Test]
        public void un_disparo_cae_sobre_el_objetivo()
        {
            var puntos = MorteroPlacement.Colocar(Objetivo, SinOtros, 1, RadioArea, 0f);

            Assert.AreEqual(1, puntos.Count);
            Assert.AreEqual(Objetivo, puntos[0]);
        }

        [Test]
        public void tres_disparos_solo_objetivo_forman_triangulo_pegado_sobre_el()
        {
            var puntos = MorteroPlacement.Colocar(Objetivo, SinOtros, 3, RadioArea, 30f);

            Assert.AreEqual(3, puntos.Count);
            Assert.AreEqual(Objetivo, puntos[0]);
            AssertSinSolaparse(puntos);
            // Lo más juntos posible: cada par se toca justo.
            Assert.AreEqual(Separacion, Vector2.Distance(puntos[0], puntos[1]), Delta);
            Assert.AreEqual(Separacion, Vector2.Distance(puntos[0], puntos[2]), Delta);
            Assert.AreEqual(Separacion, Vector2.Distance(puntos[1], puntos[2]), Delta);
        }

        [Test]
        public void otro_jugador_lejos_recibe_su_propio_disparo()
        {
            var otro = Objetivo + new Vector2(30f, 0f);

            var puntos = MorteroPlacement.Colocar(Objetivo, new[] { otro }, 3, RadioArea, 0f);

            Assert.AreEqual(Objetivo, puntos[0]);
            Assert.AreEqual(otro, puntos[1]);
            AssertSinSolaparse(puntos);
            // El tercero queda pegado a la del objetivo.
            Assert.AreEqual(Separacion, Vector2.Distance(puntos[0], puntos[2]), Delta);
        }

        [Test]
        public void otro_jugador_muy_cerca_se_recorre_lo_justo_hacia_el()
        {
            var otro = Objetivo + new Vector2(3f, 0f);

            var puntos = MorteroPlacement.Colocar(Objetivo, new[] { otro }, 2, RadioArea, 90f);

            Assert.AreEqual(Objetivo, puntos[0]);
            Assert.AreEqual(Separacion, Vector2.Distance(puntos[0], puntos[1]), Delta);
            // Va en dirección al otro jugador, no a otro lado.
            Assert.AreEqual(Objetivo.x + Separacion, puntos[1].x, Delta);
            Assert.AreEqual(Objetivo.y, puntos[1].y, Delta);
        }

        [Test]
        public void nunca_se_solapan_aunque_los_jugadores_esten_encimados()
        {
            var otros = new[] { Objetivo + new Vector2(0.5f, 0.5f), Objetivo };

            var puntos = MorteroPlacement.Colocar(Objetivo, otros, 3, RadioArea, 45f);

            Assert.AreEqual(3, puntos.Count);
            AssertSinSolaparse(puntos);
        }

        [Test]
        public void con_mas_jugadores_que_disparos_el_objetivo_siempre_tiene_el_suyo()
        {
            var otros = new[] { new Vector2(50f, 0f), new Vector2(-50f, 0f), new Vector2(0f, 50f) };

            var puntos = MorteroPlacement.Colocar(Objetivo, otros, 2, RadioArea, 0f);

            Assert.AreEqual(2, puntos.Count);
            Assert.AreEqual(Objetivo, puntos[0]);
            Assert.AreEqual(otros[0], puntos[1]);
        }

        [Test]
        public void cero_disparos_se_trata_como_uno()
        {
            var puntos = MorteroPlacement.Colocar(Objetivo, SinOtros, 0, RadioArea, 0f);

            Assert.AreEqual(1, puntos.Count);
        }
    }
}
