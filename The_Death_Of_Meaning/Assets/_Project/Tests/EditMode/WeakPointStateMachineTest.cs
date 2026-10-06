using System;
using NUnit.Framework;
using TDOM.Gameplay.Combat;

namespace TDOM.Tests.EditMode
{
    public class WeakPointStateMachineTest
    {
        private const int Semilla = 1234;

        [Test]
        public void ActivarFase1_DejaExactamenteDosOjosBrillando()
        {
            var maquina = new WeakPointStateMachine(semilla: Semilla);

            maquina.ActivarFase(1);

            Assert.AreEqual(2, Contar(maquina, EstadoOjo.Brillando));
        }

        [Test]
        public void ActivarFase4_DejaExactamenteTresOjosBrillando()
        {
            var maquina = new WeakPointStateMachine(semilla: Semilla);

            maquina.ActivarFase(4);

            Assert.AreEqual(3, Contar(maquina, EstadoOjo.Brillando));
        }

        [Test]
        public void GolpeMelee_SobreOjoCerrado_EsIgnorado()
        {
            var maquina = MaquinaEnFase1();
            int ojo = Buscar(maquina, EstadoOjo.Cerrado);

            Assert.AreEqual(ResultadoGolpe.Ignorado, maquina.GolpeMelee(ojo));
            Assert.AreEqual(EstadoOjo.Cerrado, maquina.Estado(ojo));
        }

        [Test]
        public void GolpeMelee_SobreOjoBrillando_LoAbre()
        {
            var maquina = MaquinaEnFase1();
            int ojo = Buscar(maquina, EstadoOjo.Brillando);

            Assert.AreEqual(ResultadoGolpe.Abrio, maquina.GolpeMelee(ojo));
            Assert.AreEqual(EstadoOjo.Abierto, maquina.Estado(ojo));
        }

        [Test]
        public void GolpeMelee_SobreOjoAbierto_Rebota()
        {
            var maquina = MaquinaEnFase1();
            int ojo = AbrirUnOjo(maquina);

            Assert.AreEqual(ResultadoGolpe.Rebote, maquina.GolpeMelee(ojo));
            Assert.AreEqual(EstadoOjo.Abierto, maquina.Estado(ojo));
        }

        [Test]
        public void Disparo_SobreOjoBrillando_NoHaceDanio()
        {
            var maquina = MaquinaEnFase1();
            int ojo = Buscar(maquina, EstadoOjo.Brillando);

            Assert.IsFalse(maquina.Disparo(ojo));
        }

        [Test]
        public void Disparo_SobreOjoAbierto_HaceDanio()
        {
            var maquina = MaquinaEnFase1();
            int ojo = AbrirUnOjo(maquina);

            Assert.IsTrue(maquina.Disparo(ojo));
        }

        [Test]
        public void OjoAbierto_PasaAEnfriandoALos10s_YACerradoALos15s()
        {
            var maquina = MaquinaEnFase1();
            int ojo = AbrirUnOjo(maquina);

            maquina.Tick(9.9f);
            Assert.AreEqual(EstadoOjo.Abierto, maquina.Estado(ojo));

            maquina.Tick(0.1f);
            Assert.AreEqual(EstadoOjo.Enfriando, maquina.Estado(ojo));

            maquina.Tick(4.9f);
            Assert.AreEqual(EstadoOjo.Enfriando, maquina.Estado(ojo));

            maquina.Tick(0.1f);
            Assert.AreEqual(EstadoOjo.Cerrado, maquina.Estado(ojo));
        }

        [Test]
        public void AlEnfriarseUnOjo_OtroOjoBrillaParaMantenerLaCantidadDeLaFase()
        {
            var maquina = MaquinaEnFase1();
            int ojo = AbrirUnOjo(maquina);

            maquina.Tick(10f);

            Assert.AreEqual(EstadoOjo.Enfriando, maquina.Estado(ojo));
            Assert.AreEqual(2, ContarActivos(maquina));
        }

        [Test]
        public void ActivarFaseNueva_MantieneAbiertosLosOjosAbiertos_YAjustaLaCantidad()
        {
            var maquina = MaquinaEnFase1();
            int ojo = AbrirUnOjo(maquina);

            maquina.ActivarFase(4);

            Assert.AreEqual(EstadoOjo.Abierto, maquina.Estado(ojo));
            Assert.AreEqual(3, ContarActivos(maquina));
        }

        [Test]
        public void MismaSemilla_EligeLosMismosOjos()
        {
            var a = new WeakPointStateMachine(semilla: Semilla);
            var b = new WeakPointStateMachine(semilla: Semilla);

            a.ActivarFase(1);
            b.ActivarFase(1);

            AssertMismosEstados(a, b);
        }

        [Test]
        public void Tick_EsIndependienteDelFramerate()
        {
            var a = MaquinaEnFase1();
            var b = MaquinaEnFase1();
            int ojo = Buscar(a, EstadoOjo.Brillando);
            a.GolpeMelee(ojo);
            b.GolpeMelee(ojo);

            Avanzar(a, 600, 1f / 60f);
            Avanzar(b, 300, 1f / 30f);
            AssertMismosEstados(a, b);

            Avanzar(a, 300, 1f / 60f);
            Avanzar(b, 150, 1f / 30f);
            AssertMismosEstados(a, b);
        }

        [Test]
        public void ActivarFase_ConFaseInvalida_Lanza()
        {
            var maquina = new WeakPointStateMachine(semilla: Semilla);

            Assert.Throws<ArgumentOutOfRangeException>(() => maquina.ActivarFase(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => maquina.ActivarFase(6));
        }

        private static WeakPointStateMachine MaquinaEnFase1()
        {
            var maquina = new WeakPointStateMachine(semilla: Semilla);
            maquina.ActivarFase(1);
            return maquina;
        }

        private static int AbrirUnOjo(WeakPointStateMachine maquina)
        {
            int ojo = Buscar(maquina, EstadoOjo.Brillando);
            maquina.GolpeMelee(ojo);
            return ojo;
        }

        private static void Avanzar(WeakPointStateMachine maquina, int ticks, float dt)
        {
            for (int i = 0; i < ticks; i++)
                maquina.Tick(dt);
        }

        private static int Buscar(WeakPointStateMachine maquina, EstadoOjo estado)
        {
            for (int ojo = 0; ojo < maquina.TotalOjos; ojo++)
            {
                if (maquina.Estado(ojo) == estado)
                    return ojo;
            }

            Assert.Fail($"No hay ningún ojo en estado {estado}.");
            return -1;
        }

        private static int Contar(WeakPointStateMachine maquina, EstadoOjo estado)
        {
            int total = 0;
            for (int ojo = 0; ojo < maquina.TotalOjos; ojo++)
            {
                if (maquina.Estado(ojo) == estado)
                    total++;
            }
            return total;
        }

        private static int ContarActivos(WeakPointStateMachine maquina)
        {
            return Contar(maquina, EstadoOjo.Brillando) + Contar(maquina, EstadoOjo.Abierto);
        }

        private static void AssertMismosEstados(WeakPointStateMachine a, WeakPointStateMachine b)
        {
            for (int ojo = 0; ojo < a.TotalOjos; ojo++)
                Assert.AreEqual(a.Estado(ojo), b.Estado(ojo), $"El ojo {ojo} difiere.");
        }
    }
}
