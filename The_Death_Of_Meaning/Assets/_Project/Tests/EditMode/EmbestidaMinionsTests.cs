using System;
using NUnit.Framework;
using TDOM.Gameplay.Combat;

namespace TDOM.Tests.EditMode
{
    public class EmbestidaMinionsTests
    {
        private const float Dt = 1f / 60f;

        [Test]
        public void vuelo_alcanza_altura_configurada_y_aterriza()
        {
            var impulso = new ImpulsoMinion(2f, 16f, 2f);
            impulso.Tick(0.5f);
            Assert.That(impulso.Altura, Is.EqualTo(2f).Within(0.0001f));
            impulso.Tick(0.5f);
            Assert.That(impulso.Altura, Is.Zero);
            Assert.That(impulso.Progreso, Is.EqualTo(1f));
            Assert.That(impulso.EnVuelo, Is.False);
            Assert.That(impulso.Aturdido, Is.True);
            impulso.Tick(1f);
            Assert.That(impulso.Aturdido, Is.False);
        }

        [Test]
        public void no_se_recupera_antes_de_aterrizar()
        {
            var impulso = new ImpulsoMinion(2f, 16f, 0.1f);
            impulso.Tick(0.5f);
            Assert.That(impulso.Aturdido, Is.True);
            impulso.Tick(0.5f);
            Assert.That(impulso.Aturdido, Is.False);
        }

        [Test]
        public void vuelo_es_determinista_con_dt_fijo()
        {
            var primero = new ImpulsoMinion(3f, 9.81f, 2f);
            var segundo = new ImpulsoMinion(3f, 9.81f, 2f);
            for (int i = 0; i < 180; i++)
            {
                primero.Tick(Dt);
                segundo.Tick(Dt);
                Assert.That(primero.Altura, Is.EqualTo(segundo.Altura));
                Assert.That(primero.Aturdido, Is.EqualTo(segundo.Aturdido));
                Assert.That(primero.Altura, Is.GreaterThanOrEqualTo(0f));
            }
            Assert.That(primero.Aturdido, Is.False);
        }

        [Test]
        public void trayectoria_no_depende_de_la_particion_del_tiempo()
        {
            var fijo = new ImpulsoMinion(3f, 9.81f, 2f);
            var acumulado = new ImpulsoMinion(3f, 9.81f, 2f);
            for (int i = 0; i < 30; i++)
                fijo.Tick(Dt);
            acumulado.Tick(0.5f);
            Assert.That(fijo.Altura, Is.EqualTo(acumulado.Altura).Within(0.0001f));
            Assert.That(fijo.Progreso, Is.EqualTo(acumulado.Progreso).Within(0.0001f));
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void rechaza_parametros_de_vuelo_invalidos(float valor)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ImpulsoMinion(valor, 10f, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ImpulsoMinion(1f, valor, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ImpulsoMinion(1f, 10f, valor));
        }

        [Test]
        public void empuja_varios_minions_una_vez_por_embestida()
        {
            var registro = new RegistroEmbestida();
            Assert.That(registro.TryIniciar(0f, 7f, 0.8f, 10f), Is.True);
            Assert.That(registro.TryRegistrar(1), Is.True);
            Assert.That(registro.TryRegistrar(2), Is.True);
            Assert.That(registro.TryRegistrar(1), Is.False);
            Assert.That(registro.TryIniciar(1f, 7f, 0.8f, 10f), Is.False);
            Assert.That(registro.TryRegistrar(1), Is.False);
            Assert.That(registro.TryIniciar(7f, 7f, 0.8f, 10f), Is.True);
            Assert.That(registro.TryRegistrar(1), Is.True);
        }

        [Test]
        public void rechaza_recorridos_fuera_de_la_carga_y_no_consume_el_valido()
        {
            var registro = new RegistroEmbestida();
            Assert.That(registro.TryAvanzar(0.1f, 1f), Is.False);
            registro.TryIniciar(0f, 7f, 0.8f, 10f);
            Assert.That(registro.TryAvanzar(0.1f, 4f), Is.True);
            Assert.That(registro.TryAvanzar(0.1f, 1f), Is.False);
            Assert.That(registro.TryAvanzar(0.9f, 1f), Is.False);
            Assert.That(registro.TryAvanzar(float.NaN, 1f), Is.False);
            Assert.That(registro.TryAvanzar(0.2f, float.PositiveInfinity), Is.False);
            Assert.That(registro.TryAvanzar(0.2f, 7f), Is.False);
            Assert.That(registro.TryAvanzar(0.8f, 6f), Is.True);
        }

        [Test]
        public void interrumpir_combo_exige_un_aviso_nuevo_antes_de_golpear()
        {
            var brain = new MinionBrain(2f, 0.4f, 0.2f, 1f);
            Assert.That(brain.Tick(Dt, true, 1f), Is.EqualTo(EventoMinion.Aviso));
            brain.Tick(0.3f, true, 1f);
            brain.InterrumpirAtaque();
            Assert.That(brain.DebeMoverse, Is.True);
            Assert.That(brain.GolpesDados, Is.Zero);
            Assert.That(brain.Tick(Dt, true, 1f), Is.EqualTo(EventoMinion.Aviso));
            Assert.That(brain.Tick(Dt, true, 1f), Is.EqualTo(EventoMinion.Ninguno));
        }
    }
}
