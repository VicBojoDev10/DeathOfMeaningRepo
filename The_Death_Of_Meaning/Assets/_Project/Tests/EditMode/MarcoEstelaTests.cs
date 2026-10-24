using NUnit.Framework;
using TDOM.Gameplay.Vfx;
using UnityEngine;

namespace TDOM.Tests.EditMode
{
    public class MarcoEstelaTests
    {
        private const float Delta = 0.001f;

        // Tajo horizontal de izquierda a derecha (+X) con la hoja en vertical (+Y).
        private static MuestrasEstela TajoHorizontal(int muestras)
        {
            var estela = new MuestrasEstela(16);
            for (int i = 0; i < muestras; i++)
                estela.Agregar(new Vector3(i * 0.2f, 0f, 0f), new Vector3(i * 0.2f, 1f, 0f), 0f);
            return estela;
        }

        [Test]
        public void sin_estela_no_hay_marco()
        {
            Assert.IsFalse(MarcoEstela.Calcular(null, 0.3f, out _, out _));
            Assert.IsFalse(MarcoEstela.Calcular(new MuestrasEstela(8), 0.3f, out _, out _));
        }

        [Test]
        public void con_una_sola_muestra_no_hay_marco()
        {
            Assert.IsFalse(MarcoEstela.Calcular(TajoHorizontal(1), 0.3f, out _, out _));
        }

        [Test]
        public void el_centro_es_el_promedio_de_los_medios_de_la_hoja()
        {
            Assert.IsTrue(MarcoEstela.Calcular(TajoHorizontal(5), 0.3f, out Vector3 centro, out _));
            Assert.AreEqual(0.4f, centro.x, Delta);
            Assert.AreEqual(0.5f, centro.y, Delta);
        }

        [Test]
        public void el_eje_z_sigue_el_recorrido_y_el_eje_y_es_la_normal_de_la_cinta()
        {
            Assert.IsTrue(
                MarcoEstela.Calcular(TajoHorizontal(5), 0.3f, out _, out Quaternion rotacion)
            );
            Assert.AreEqual(1f, Vector3.Dot(rotacion * Vector3.forward, Vector3.right), Delta);
            Assert.AreEqual(
                1f,
                Mathf.Abs(Vector3.Dot(rotacion * Vector3.up, Vector3.forward)),
                Delta
            );
        }

        [Test]
        public void una_muestra_vieja_no_sirve_de_marco()
        {
            MuestrasEstela estela = TajoHorizontal(5);
            estela.Envejecer(0.5f, 10f);
            Assert.IsFalse(MarcoEstela.Calcular(estela, 0.3f, out _, out _));
        }

        [Test]
        public void solo_cuenta_el_tramo_vigente_despues_de_un_corte()
        {
            MuestrasEstela estela = TajoHorizontal(5);
            estela.Cortar();
            for (int i = 0; i < 3; i++)
                estela.Agregar(
                    new Vector3(0f, 5f + i * 0.2f, 0f),
                    new Vector3(1f, 5f + i * 0.2f, 0f),
                    0f
                );

            Assert.IsTrue(MarcoEstela.Calcular(estela, 0.3f, out Vector3 centro, out _));
            Assert.AreEqual(5.2f, centro.y, Delta);
        }

        [Test]
        public void si_la_hoja_apenas_se_mueve_no_hay_marco()
        {
            var estela = new MuestrasEstela(8);
            estela.Agregar(Vector3.zero, Vector3.up, 0f);
            estela.Agregar(new Vector3(0.001f, 0f, 0f), new Vector3(0.001f, 1f, 0f), 0f);
            Assert.IsFalse(MarcoEstela.Calcular(estela, 0.3f, out _, out _));
        }

        [Test]
        public void si_el_recorrido_va_a_lo_largo_de_la_hoja_no_hay_marco()
        {
            var estela = new MuestrasEstela(8);
            estela.Agregar(Vector3.zero, Vector3.up, 0f);
            estela.Agregar(new Vector3(0f, 0.5f, 0f), new Vector3(0f, 1.5f, 0f), 0f);
            Assert.IsFalse(MarcoEstela.Calcular(estela, 0.3f, out _, out _));
        }
    }
}
