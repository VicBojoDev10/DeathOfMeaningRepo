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
        public void el_avance_va_al_frente_sin_subir_ni_bajar()
        {
            Vector3 avance = MarcoTajo.Avance(new Vector3(0f, 0.5f, 1f), 3f);

            Assert.AreEqual(0f, avance.y, Delta);
            Assert.AreEqual(3f, avance.z, Delta);
        }

        [Test]
        public void sin_direccion_horizontal_no_avanza()
        {
            Assert.AreEqual(Vector3.zero, MarcoTajo.Avance(Vector3.up, 3f));
        }
    }
}
