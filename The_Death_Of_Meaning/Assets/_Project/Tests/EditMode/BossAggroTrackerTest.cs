using NUnit.Framework;
using TDOM.Gameplay.Combat;

namespace TDOM.Tests.EditMode
{
    public class BossAggroTrackerTest
    {
        private const ulong JugadorA = 1;
        private const ulong JugadorB = 2;

        [Test]
        public void sin_golpes_el_danio_reciente_es_cero()
        {
            var tracker = new BossAggroTracker(5f);

            Assert.AreEqual(0f, tracker.DanioReciente(JugadorA, 10f));
        }

        [Test]
        public void suma_los_golpes_de_cada_jugador_por_separado()
        {
            var tracker = new BossAggroTracker(5f);

            tracker.Registrar(JugadorA, 10f, 1f);
            tracker.Registrar(JugadorA, 5f, 2f);
            tracker.Registrar(JugadorB, 3f, 2f);

            Assert.AreEqual(15f, tracker.DanioReciente(JugadorA, 3f));
            Assert.AreEqual(3f, tracker.DanioReciente(JugadorB, 3f));
        }

        [Test]
        public void olvida_los_golpes_fuera_de_la_ventana()
        {
            var tracker = new BossAggroTracker(5f);

            tracker.Registrar(JugadorA, 10f, 0f);
            tracker.Registrar(JugadorA, 4f, 8f);

            Assert.AreEqual(4f, tracker.DanioReciente(JugadorA, 10f));
        }

        [Test]
        public void un_golpe_justo_en_el_borde_de_la_ventana_todavia_cuenta()
        {
            var tracker = new BossAggroTracker(5f);

            tracker.Registrar(JugadorA, 10f, 5f);

            Assert.AreEqual(10f, tracker.DanioReciente(JugadorA, 10f));
        }

        [Test]
        public void ignora_danio_cero_o_negativo()
        {
            var tracker = new BossAggroTracker(5f);

            tracker.Registrar(JugadorA, 0f, 1f);
            tracker.Registrar(JugadorA, -3f, 1f);

            Assert.AreEqual(0f, tracker.DanioReciente(JugadorA, 1f));
        }

        [Test]
        public void limpiar_borra_todo_el_historial()
        {
            var tracker = new BossAggroTracker(5f);
            tracker.Registrar(JugadorA, 10f, 1f);

            tracker.Limpiar();

            Assert.AreEqual(0f, tracker.DanioReciente(JugadorA, 1f));
        }
    }
}
