using System;
using NUnit.Framework;
using TDOM.Gameplay.Vfx;

namespace TDOM.Tests.EditMode
{
    public class CurvaDestelloGolpeTests
    {
        private const float Delta = 0.001f;
        private const float Dt = 1f / 60f;
        private const float Duracion = 0.3f;
        private const float IntensidadMaxima = 0.8f;
        private static readonly Func<float, float> Lineal = t => 1f - t;

        private static float AvanzarCuadros(CurvaDestelloGolpe destello, int cuadros)
        {
            float intensidad = 0f;
            for (int i = 0; i < cuadros; i++)
                intensidad = destello.Avanzar(Dt, Lineal, IntensidadMaxima);
            return intensidad;
        }

        [Test]
        public void sin_golpe_la_intensidad_es_cero()
        {
            var destello = new CurvaDestelloGolpe(Duracion);

            Assert.AreEqual(0f, destello.Avanzar(Dt, Lineal, IntensidadMaxima), Delta);
            Assert.IsFalse(destello.Activo);
        }

        [Test]
        public void al_recibir_el_golpe_brilla_con_la_intensidad_maxima()
        {
            var destello = new CurvaDestelloGolpe(Duracion);
            destello.Reiniciar();

            Assert.AreEqual(
                IntensidadMaxima,
                destello.Avanzar(Dt, Lineal, IntensidadMaxima),
                Delta
            );
        }

        [Test]
        public void a_la_mitad_del_destello_sigue_la_curva()
        {
            var destello = new CurvaDestelloGolpe(Duracion);
            destello.Reiniciar();

            float intensidad = AvanzarCuadros(destello, 10);

            Assert.AreEqual((1f - 9f * Dt / Duracion) * IntensidadMaxima, intensidad, Delta);
        }

        [Test]
        public void al_terminar_la_duracion_vuelve_a_cero_y_se_apaga()
        {
            var destello = new CurvaDestelloGolpe(Duracion);
            destello.Reiniciar();

            float intensidad = AvanzarCuadros(destello, 20);

            Assert.AreEqual(0f, intensidad, Delta);
            Assert.IsFalse(destello.Activo);
        }

        [Test]
        public void otro_golpe_durante_el_destello_reinicia_la_curva()
        {
            var destello = new CurvaDestelloGolpe(Duracion);
            destello.Reiniciar();
            AvanzarCuadros(destello, 12);

            destello.Reiniciar();

            Assert.AreEqual(
                IntensidadMaxima,
                destello.Avanzar(Dt, Lineal, IntensidadMaxima),
                Delta
            );
        }

        [Test]
        public void la_curva_fuera_de_rango_se_limita()
        {
            var destello = new CurvaDestelloGolpe(Duracion);
            destello.Reiniciar();

            Assert.AreEqual(
                IntensidadMaxima,
                destello.Avanzar(Dt, _ => 3f, IntensidadMaxima),
                Delta
            );
        }

        [Test]
        public void solo_cuenta_como_golpe_cuando_la_vida_baja()
        {
            Assert.IsTrue(CurvaDestelloGolpe.EsGolpe(100f, 80f));
            Assert.IsFalse(CurvaDestelloGolpe.EsGolpe(40f, 60f));
            Assert.IsFalse(CurvaDestelloGolpe.EsGolpe(50f, 50f));
        }
    }
}
