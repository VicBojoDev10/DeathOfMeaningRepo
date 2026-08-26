/*using NUnit.Framework;
using Tdom.Core.Combat;

namespace Tdom.Tests.EditMode
{
    /// <summary>
    /// Tests del CooldownTimer.
    ///
    /// Es la clase POCO más simple del proyecto, así que sirve como
    /// plantilla de referencia: mira la estructura Arrange / Act / Assert
    /// y los nombres de los tests antes de escribir los tuyos.
    ///
    /// Fíjate en lo que NO hay aquí: ni escena, ni GameObject, ni play mode,
    /// ni Time.deltaTime. El dt se pasa a mano. Por eso corren en milisegundos.
    /// </summary>
    public sealed class CooldownTimerTests
    {
        private const float Dt = 1f / 60f;   // un frame a 60 fps

        [Test]
        public void Un_timer_recien_creado_esta_listo()
        {
            // Arrange
            var timer = new CooldownTimer(duracion: 4f);

            // Assert
            Assert.IsTrue(timer.Listo);
        }

        [Test]
        public void Al_dispararse_deja_de_estar_listo()
        {
            var timer = new CooldownTimer(duracion: 4f);

            timer.Disparar();

            Assert.IsFalse(timer.Listo);
        }

        [Test]
        public void Vuelve_a_estar_listo_al_cumplirse_la_duracion()
        {
            var timer = new CooldownTimer(duracion: 1f);
            timer.Disparar();

            // Simular 1 segundo de juego, frame por frame
            for (int i = 0; i < 60; i++)
                timer.Tick(Dt);

            Assert.IsTrue(timer.Listo);
        }

        [Test]
        public void Sigue_bloqueado_antes_de_cumplirse_la_duracion()
        {
            var timer = new CooldownTimer(duracion: 1f);
            timer.Disparar();

            // Solo medio segundo
            for (int i = 0; i < 30; i++)
                timer.Tick(Dt);

            Assert.IsFalse(timer.Listo);
        }

        [Test]
        public void El_valor_normalizado_va_de_uno_a_cero()
        {
            var timer = new CooldownTimer(duracion: 1f);
            timer.Disparar();

            Assert.AreEqual(1f, timer.Normalizado, 0.01f);

            for (int i = 0; i < 30; i++)
                timer.Tick(Dt);

            Assert.AreEqual(0.5f, timer.Normalizado, 0.05f);
        }

        [Test]
        public void Dispararlo_de_nuevo_reinicia_la_cuenta()
        {
            var timer = new CooldownTimer(duracion: 1f);
            timer.Disparar();

            for (int i = 0; i < 50; i++)
                timer.Tick(Dt);

            timer.Disparar();   // reinicio

            for (int i = 0; i < 50; i++)
                timer.Tick(Dt);

            Assert.IsFalse(timer.Listo);
        }

        /// <summary>
        /// Este test es el que importa de verdad: el resultado no puede
        /// depender del framerate. Si falla, el cooldown se comporta
        /// distinto en una máquina rápida y en una lenta.
        /// </summary>
        [Test]
        public void El_resultado_no_depende_del_framerate()
        {
            var rapido = new CooldownTimer(duracion: 1f);
            var lento  = new CooldownTimer(duracion: 1f);

            rapido.Disparar();
            lento.Disparar();

            // 120 fps
            for (int i = 0; i < 120; i++) rapido.Tick(1f / 120f);
            // 30 fps
            for (int i = 0; i < 30; i++)  lento.Tick(1f / 30f);

            Assert.AreEqual(rapido.Listo, lento.Listo);
        }
    }
}*/
