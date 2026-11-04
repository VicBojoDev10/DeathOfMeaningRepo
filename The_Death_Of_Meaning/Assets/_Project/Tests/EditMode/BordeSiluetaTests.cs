using NUnit.Framework;
using TDOM.Gameplay.Combat;
using UnityEngine;

namespace TDOM.Tests.EditMode
{
    public class BordeSiluetaTests
    {
        private const float Tolerancia = 0.001f;
        private const float Respaldo = -1f;

        // Elipse centrada en el origen del mundo.
        private static float Distancia(Vector2 origen, Vector2 semiejes, Vector2 direccion) =>
            BordeSilueta.Distancia(origen, Vector2.zero, semiejes, direccion, Respaldo);

        [Test]
        public void Desde_el_centro_de_un_circulo_la_distancia_es_el_radio()
        {
            float distancia = BordeSilueta.Distancia(
                Vector2.zero,
                Vector2.zero,
                new Vector2(5f, 5f),
                new Vector2(1f, 1f),
                Respaldo
            );

            Assert.AreEqual(5f, distancia, Tolerancia);
        }

        [Test]
        public void En_una_elipse_cada_eje_da_su_semieje()
        {
            var semiejes = new Vector2(10f, 5f);

            float enX = Distancia(Vector2.zero, semiejes, Vector2.right);
            float enY = Distancia(Vector2.zero, semiejes, Vector2.up);

            Assert.AreEqual(10f, enX, Tolerancia);
            Assert.AreEqual(5f, enY, Tolerancia);
        }

        [Test]
        public void La_direccion_no_necesita_estar_normalizada()
        {
            float distancia = BordeSilueta.Distancia(
                Vector2.zero,
                Vector2.zero,
                new Vector2(10f, 5f),
                new Vector2(0f, 2f),
                Respaldo
            );

            Assert.AreEqual(5f, distancia, Tolerancia);
        }

        [Test]
        public void Desde_un_punto_descentrado_la_distancia_cambia_con_la_direccion()
        {
            var origen = new Vector2(3f, 0f);
            var semiejes = new Vector2(5f, 5f);

            float adelante = Distancia(origen, semiejes, Vector2.right);
            float atras = Distancia(origen, semiejes, Vector2.left);

            Assert.AreEqual(2f, adelante, Tolerancia);
            Assert.AreEqual(8f, atras, Tolerancia);
        }

        [Test]
        public void El_centro_de_la_elipse_puede_estar_desplazado()
        {
            float distancia = BordeSilueta.Distancia(
                new Vector2(2f, 2f),
                new Vector2(2f, 2f),
                new Vector2(10f, 5f),
                new Vector2(1f, 1f),
                Respaldo
            );

            Assert.AreEqual(6.3246f, distancia, Tolerancia);
        }

        [Test]
        public void Si_el_origen_esta_fuera_de_la_silueta_usa_el_respaldo()
        {
            float distancia = BordeSilueta.Distancia(
                new Vector2(9f, 0f),
                Vector2.zero,
                new Vector2(5f, 5f),
                Vector2.right,
                Respaldo
            );

            Assert.AreEqual(Respaldo, distancia);
        }

        [Test]
        public void Con_semiejes_o_direccion_invalidos_usa_el_respaldo()
        {
            Assert.AreEqual(Respaldo, Distancia(Vector2.zero, Vector2.zero, Vector2.right));
            Assert.AreEqual(Respaldo, Distancia(Vector2.zero, new Vector2(5f, 5f), Vector2.zero));
        }
    }
}
