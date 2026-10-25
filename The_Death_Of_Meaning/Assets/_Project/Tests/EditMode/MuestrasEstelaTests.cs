using NUnit.Framework;
using TDOM.Gameplay.Vfx;
using UnityEngine;

namespace TDOM.Tests.EditMode
{
    public class MuestrasEstelaTests
    {
        private const float Dt = 1f / 60f;
        private const float Delta = 0.0001f;

        [Test]
        public void la_primera_muestra_siempre_se_agrega()
        {
            var muestras = new MuestrasEstela(8);

            Assert.IsTrue(muestras.Agregar(Vector3.zero, Vector3.up, 0.5f));
            Assert.AreEqual(1, muestras.Cantidad);
        }

        [Test]
        public void una_muestra_demasiado_cerca_de_la_anterior_se_descarta()
        {
            var muestras = new MuestrasEstela(8);
            muestras.Agregar(Vector3.zero, Vector3.up, 0.5f);

            Assert.IsFalse(muestras.Agregar(new Vector3(0.1f, 0f, 0f), Vector3.up, 0.5f));
            Assert.AreEqual(1, muestras.Cantidad);
        }

        [Test]
        public void al_llenarse_se_descarta_la_muestra_mas_vieja()
        {
            var muestras = new MuestrasEstela(3);
            for (int i = 0; i < 4; i++)
                muestras.Agregar(new Vector3(i, 0f, 0f), new Vector3(i, 1f, 0f), 0f);

            Assert.AreEqual(3, muestras.Cantidad);
            Assert.AreEqual(1f, muestras[0].BaseHoja.x, Delta);
            Assert.AreEqual(3f, muestras[2].BaseHoja.x, Delta);
        }

        [Test]
        public void las_muestras_que_superan_la_vida_desaparecen()
        {
            var muestras = new MuestrasEstela(8);
            muestras.Agregar(Vector3.zero, Vector3.up, 0f);
            for (int i = 0; i < 6; i++)
                muestras.Envejecer(Dt, 0.2f);
            muestras.Agregar(Vector3.right, Vector3.right + Vector3.up, 0f);
            for (int i = 0; i < 7; i++)
                muestras.Envejecer(Dt, 0.2f);

            Assert.AreEqual(1, muestras.Cantidad);
            Assert.AreEqual(7 * Dt, muestras[0].Edad, Delta);
        }

        [Test]
        public void sin_muestras_nuevas_la_estela_se_vacia_sola()
        {
            var muestras = new MuestrasEstela(8);
            muestras.Agregar(Vector3.zero, Vector3.up, 0f);
            muestras.Agregar(Vector3.right, Vector3.right + Vector3.up, 0f);

            for (int i = 0; i < 60; i++)
                muestras.Envejecer(Dt, 0.5f);

            Assert.AreEqual(0, muestras.Cantidad);
        }

        [Test]
        public void vaciar_borra_todas_las_muestras()
        {
            var muestras = new MuestrasEstela(8);
            muestras.Agregar(Vector3.zero, Vector3.up, 0f);
            muestras.Vaciar();

            Assert.AreEqual(0, muestras.Cantidad);
        }

        [Test]
        public void la_primera_muestra_no_se_une_a_ninguna_anterior()
        {
            var muestras = new MuestrasEstela(8);
            muestras.Agregar(Vector3.zero, Vector3.up, 0f);

            Assert.IsFalse(muestras[0].ConectaConAnterior);
        }

        [Test]
        public void las_muestras_seguidas_se_unen_a_la_anterior()
        {
            var muestras = new MuestrasEstela(8);
            muestras.Agregar(Vector3.zero, Vector3.up, 0f);
            muestras.Agregar(Vector3.right, Vector3.right + Vector3.up, 0f);

            Assert.IsTrue(muestras[1].ConectaConAnterior);
        }

        [Test]
        public void tras_cortar_la_siguiente_muestra_empieza_un_tramo_nuevo()
        {
            var muestras = new MuestrasEstela(8);
            muestras.Agregar(Vector3.zero, Vector3.up, 0f);
            muestras.Agregar(Vector3.right, Vector3.right + Vector3.up, 0f);
            muestras.Cortar();

            Assert.IsTrue(muestras.Agregar(new Vector3(5f, 0f, 0f), new Vector3(5f, 1f, 0f), 0f));
            Assert.AreEqual(3, muestras.Cantidad);
            Assert.IsFalse(muestras[2].ConectaConAnterior);
        }

        [Test]
        public void el_tramo_nuevo_se_agrega_aunque_este_cerca_de_la_ultima_muestra()
        {
            var muestras = new MuestrasEstela(8);
            muestras.Agregar(Vector3.zero, Vector3.up, 0.5f);
            muestras.Cortar();

            Assert.IsTrue(muestras.Agregar(new Vector3(0.1f, 0f, 0f), Vector3.up, 0.5f));
            Assert.IsFalse(muestras[1].ConectaConAnterior);
        }

        [Test]
        public void despues_del_tramo_nuevo_las_muestras_vuelven_a_unirse()
        {
            var muestras = new MuestrasEstela(8);
            muestras.Agregar(Vector3.zero, Vector3.up, 0f);
            muestras.Cortar();
            muestras.Agregar(Vector3.right, Vector3.right + Vector3.up, 0f);
            muestras.Agregar(new Vector3(2f, 0f, 0f), new Vector3(2f, 1f, 0f), 0f);

            Assert.IsFalse(muestras[1].ConectaConAnterior);
            Assert.IsTrue(muestras[2].ConectaConAnterior);
        }

        [Test]
        public void cortar_no_borra_las_muestras_existentes()
        {
            var muestras = new MuestrasEstela(8);
            muestras.Agregar(Vector3.zero, Vector3.up, 0f);
            muestras.Agregar(Vector3.right, Vector3.right + Vector3.up, 0f);

            muestras.Cortar();

            Assert.AreEqual(2, muestras.Cantidad);
        }

        [Test]
        public void envejecer_conserva_la_marca_de_tramo()
        {
            var muestras = new MuestrasEstela(8);
            muestras.Agregar(Vector3.zero, Vector3.up, 0f);
            muestras.Cortar();
            muestras.Agregar(Vector3.right, Vector3.right + Vector3.up, 0f);

            muestras.Envejecer(Dt, 1f);

            Assert.IsFalse(muestras[1].ConectaConAnterior);
        }

        [Test]
        public void vaciar_reinicia_el_tramo()
        {
            var muestras = new MuestrasEstela(8);
            muestras.Agregar(Vector3.zero, Vector3.up, 0f);
            muestras.Vaciar();
            muestras.Agregar(Vector3.right, Vector3.right + Vector3.up, 0f);

            Assert.IsFalse(muestras[0].ConectaConAnterior);
        }
    }
}
