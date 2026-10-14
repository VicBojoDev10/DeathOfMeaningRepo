using NUnit.Framework;
using TDOM.Gameplay.Camera;
using UnityEngine;

namespace TDOM.Tests.EditMode
{
    public class AimResolverTests
    {
        private const float Tolerancia = 0.001f;

        private static readonly Ray RayoCamara = new Ray(
            new Vector3(0.5f, 1.6f, -4f),
            Vector3.forward
        );

        private static readonly Vector3 OrigenFisico = new Vector3(0f, 1.6f, 0f);

        private static void AssertVectoresIguales(Vector3 esperado, Vector3 actual)
        {
            Assert.AreEqual(esperado.x, actual.x, Tolerancia);
            Assert.AreEqual(esperado.y, actual.y, Tolerancia);
            Assert.AreEqual(esperado.z, actual.z, Tolerancia);
        }

        [Test]
        public void con_impacto_el_objetivo_es_el_punto_de_impacto()
        {
            var impacto = new Vector3(0.5f, 1.6f, 10f);

            var solucion = AimResolver.ResolveHit(RayoCamara, impacto, OrigenFisico);

            Assert.IsTrue(solucion.HasHit);
            AssertVectoresIguales(impacto, solucion.TargetPoint);
        }

        [Test]
        public void sin_impacto_el_objetivo_esta_al_alcance_maximo_del_rayo()
        {
            var solucion = AimResolver.ResolveMiss(RayoCamara, 100f, OrigenFisico);

            Assert.IsFalse(solucion.HasHit);
            AssertVectoresIguales(new Vector3(0.5f, 1.6f, 96f), solucion.TargetPoint);
        }

        [Test]
        public void la_direccion_va_del_origen_fisico_al_objetivo_sin_paralaje()
        {
            var impacto = new Vector3(0.5f, 1.6f, 10f);

            var solucion = AimResolver.ResolveHit(RayoCamara, impacto, OrigenFisico);

            AssertVectoresIguales((impacto - OrigenFisico).normalized, solucion.Direction);
            Assert.AreEqual(1f, solucion.Direction.magnitude, Tolerancia);
        }

        [Test]
        public void si_el_objetivo_coincide_con_el_origen_usa_la_direccion_de_la_camara()
        {
            var solucion = AimResolver.ResolveHit(RayoCamara, OrigenFisico, OrigenFisico);

            AssertVectoresIguales(RayoCamara.direction, solucion.Direction);
        }

        [Test]
        public void el_alcance_se_mide_desde_el_origen_fisico()
        {
            Assert.IsTrue(AimResolver.IsWithinRange(OrigenFisico, new Vector3(0f, 1.6f, 30f), 30f));
            Assert.IsFalse(
                AimResolver.IsWithinRange(OrigenFisico, new Vector3(0f, 1.6f, 31f), 30f)
            );
        }

        [Test]
        public void la_solucion_es_determinista_para_las_mismas_entradas()
        {
            var impacto = new Vector3(3f, 2f, 12f);

            var primera = AimResolver.ResolveHit(RayoCamara, impacto, OrigenFisico);
            var segunda = AimResolver.ResolveHit(RayoCamara, impacto, OrigenFisico);

            Assert.AreEqual(primera.TargetPoint, segunda.TargetPoint);
            Assert.AreEqual(primera.Direction, segunda.Direction);
            Assert.AreEqual(primera.HasHit, segunda.HasHit);
        }
    }
}
