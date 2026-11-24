using NUnit.Framework;
using TDOM.Gameplay.Combat;

namespace TDOM.Tests.EditMode
{
    public class ProgresoTransicionVisualTests
    {
        private const float Duracion = 3f;
        private const double Inicio = 100d;
        private const double Fin = Inicio + Duracion;
        private const float Dt = 1f / 60f;

        [Test]
        public void Host_y_cliente_obtienen_el_mismo_progreso_con_dt_fijo()
        {
            for (int frame = 0; frame <= 180; frame++)
            {
                double ahora = Inicio + frame * (double)Dt;
                float host = ProgresoTransicionVisual.Calcular(
                    Fin,
                    ahora,
                    ProgresoTransicionVisual.SinPausa,
                    Duracion
                );
                float cliente = ProgresoTransicionVisual.Calcular(
                    Fin,
                    ahora,
                    ProgresoTransicionVisual.SinPausa,
                    Duracion
                );
                Assert.That(cliente, Is.EqualTo(host));
                Assert.That(
                    host,
                    Is.EqualTo(System.Math.Clamp(frame * Dt / Duracion, 0f, 1f)).Within(0.00001f)
                );
            }
        }

        [Test]
        public void La_pausa_conserva_el_progreso_aunque_avance_el_reloj()
        {
            const float restante = 2f;
            Assert.That(
                ProgresoTransicionVisual.Calcular(Fin, Inicio + 1d, restante, Duracion),
                Is.EqualTo(
                    ProgresoTransicionVisual.Calcular(Fin, Inicio + 100d, restante, Duracion)
                )
            );
            Assert.That(
                ProgresoTransicionVisual.Calcular(
                    Inicio + 100d + restante,
                    Inicio + 100d,
                    ProgresoTransicionVisual.SinPausa,
                    Duracion
                ),
                Is.EqualTo(1f / 3f).Within(0.00001f)
            );
        }

        [TestCase(99d, 0f)]
        [TestCase(101.5d, 0.5f)]
        [TestCase(104d, 1f)]
        public void La_llegada_tardia_recupera_el_instante_y_limita_los_extremos(
            double ahora,
            float esperado
        )
        {
            Assert.That(
                ProgresoTransicionVisual.Calcular(
                    Fin,
                    ahora,
                    ProgresoTransicionVisual.SinPausa,
                    Duracion
                ),
                Is.EqualTo(esperado)
            );
        }

        [Test]
        public void Una_transicion_sin_duracion_termina_inmediatamente()
        {
            Assert.That(
                ProgresoTransicionVisual.Calcular(
                    Fin,
                    Inicio,
                    ProgresoTransicionVisual.SinPausa,
                    0f
                ),
                Is.EqualTo(1f)
            );
        }
    }
}
