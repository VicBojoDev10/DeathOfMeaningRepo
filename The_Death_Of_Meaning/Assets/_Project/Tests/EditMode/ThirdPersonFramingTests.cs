using NUnit.Framework;
using TDOM.Gameplay.Camera;

namespace TDOM.Tests.EditMode
{
    public class ThirdPersonFramingTests
    {
        private const float Dt = 1f / 60f;
        private const float Tolerancia = 0.001f;
        private const int FramesParaEstabilizar = 600;

        private static ThirdPersonFraming CrearEncuadre()
        {
            return new ThirdPersonFraming(
                distanciaLibre: 4f,
                distanciaApuntando: 2f,
                hombroLibre: 0.5f,
                hombroApuntando: 0.3f,
                velocidad: 12f
            );
        }

        private static void Simular(ThirdPersonFraming encuadre, bool apuntando, int frames)
        {
            for (int i = 0; i < frames; i++)
                encuadre.Tick(apuntando, Dt);
        }

        [Test]
        public void sin_apuntar_mantiene_la_distancia_y_el_hombro_libres()
        {
            var encuadre = CrearEncuadre();

            Simular(encuadre, apuntando: false, FramesParaEstabilizar);

            Assert.AreEqual(4f, encuadre.Distance, Tolerancia);
            Assert.AreEqual(0.5f, encuadre.ShoulderOffset, Tolerancia);
        }

        [Test]
        public void apuntar_acerca_la_camara_hasta_los_valores_de_apuntado()
        {
            var encuadre = CrearEncuadre();

            Simular(encuadre, apuntando: true, FramesParaEstabilizar);

            Assert.AreEqual(2f, encuadre.Distance, Tolerancia);
            Assert.AreEqual(0.3f, encuadre.ShoulderOffset, Tolerancia);
        }

        [Test]
        public void apuntar_acerca_la_camara_de_forma_gradual()
        {
            var encuadre = CrearEncuadre();

            encuadre.Tick(apuntando: true, Dt);

            Assert.Less(encuadre.Distance, 4f);
            Assert.Greater(encuadre.Distance, 2f);
        }

        [Test]
        public void soltar_el_apuntado_regresa_a_la_distancia_libre()
        {
            var encuadre = CrearEncuadre();
            Simular(encuadre, apuntando: true, FramesParaEstabilizar);

            Simular(encuadre, apuntando: false, FramesParaEstabilizar);

            Assert.AreEqual(4f, encuadre.Distance, Tolerancia);
            Assert.AreEqual(0.5f, encuadre.ShoulderOffset, Tolerancia);
        }

        [Test]
        public void encuadre_es_determinista_con_dt_fijo()
        {
            var primero = CrearEncuadre();
            var segundo = CrearEncuadre();

            for (int i = 0; i < 90; i++)
            {
                bool apuntando = i % 30 < 20;
                primero.Tick(apuntando, Dt);
                segundo.Tick(apuntando, Dt);
            }

            Assert.AreEqual(primero.Distance, segundo.Distance);
            Assert.AreEqual(primero.ShoulderOffset, segundo.ShoulderOffset);
        }
    }
}
