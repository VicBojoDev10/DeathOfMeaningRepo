using NUnit.Framework;
using TDOM.Gameplay.Vfx;
using UnityEngine;

namespace TDOM.Tests.EditMode
{
    public class MarcoTajoTests
    {
        private const float Delta = 0.001f;

        [Test]
        public void el_centro_esta_a_la_mitad_de_la_hoja()
        {
            Vector3 centro = MarcoTajo.Centro(Vector3.zero, new Vector3(0f, 2f, 0f));

            Assert.AreEqual(1f, centro.y, Delta);
        }

        [Test]
        public void un_tajo_horizontal_mira_hacia_donde_barre_la_punta()
        {
            Quaternion rotacion = MarcoTajo.Rotacion(
                Vector3.zero,
                Vector3.forward,
                new Vector3(-1f, 0f, 1f),
                Quaternion.identity
            );

            Vector3 adelante = rotacion * Vector3.forward;
            Assert.AreEqual(1f, adelante.x, Delta);
        }

        [Test]
        public void un_tajo_horizontal_queda_acostado()
        {
            Quaternion rotacion = MarcoTajo.Rotacion(
                Vector3.zero,
                Vector3.forward,
                new Vector3(-1f, 0f, 1f),
                Quaternion.identity
            );

            Vector3 arriba = rotacion * Vector3.up;
            Assert.AreEqual(1f, Mathf.Abs(arriba.y), Delta);
        }

        [Test]
        public void si_la_espada_no_se_movio_usa_la_rotacion_de_respaldo()
        {
            Quaternion respaldo = Quaternion.Euler(0f, 45f, 0f);

            Quaternion rotacion = MarcoTajo.Rotacion(
                Vector3.zero,
                Vector3.forward,
                Vector3.forward,
                respaldo
            );

            Assert.AreEqual(1f, Mathf.Abs(Quaternion.Dot(respaldo, rotacion)), Delta);
        }
    }
}
