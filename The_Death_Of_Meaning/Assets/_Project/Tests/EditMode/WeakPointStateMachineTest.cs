using System;
using NUnit.Framework;
using TDOM.Gameplay.Combat;

namespace TDOM.Tests.EditMode
{
    public class WeakPointStateMachineTest
    {
        private const int TotalOjos = 8;
        private const float VidaOjo = 160f;

        [Test]
        public void AlCrearla_TodosLosOjosEstanAbiertosConLaVidaLlena()
        {
            var maquina = new WeakPointStateMachine(TotalOjos, VidaOjo);

            Assert.AreEqual(TotalOjos, maquina.OjosAbiertos);
            Assert.IsFalse(maquina.TodosCerrados);
            for (int ojo = 0; ojo < TotalOjos; ojo++)
            {
                Assert.AreEqual(EstadoOjo.Abierto, maquina.Estado(ojo));
                Assert.AreEqual(VidaOjo, maquina.Vida(ojo));
            }
        }

        [Test]
        public void SinOjos_Lanza()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new WeakPointStateMachine(0, VidaOjo));
        }

        [Test]
        public void ConVidaDeOjoNoPositiva_Lanza()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new WeakPointStateMachine(TotalOjos, 0f));
        }

        [Test]
        public void AplicarDanio_RestaVidaYDevuelveLoQueAbsorbio()
        {
            var maquina = new WeakPointStateMachine(TotalOjos, VidaOjo);

            float aplicado = maquina.AplicarDanio(2, 60f);

            Assert.AreEqual(60f, aplicado);
            Assert.AreEqual(VidaOjo - 60f, maquina.Vida(2));
            Assert.AreEqual(EstadoOjo.Abierto, maquina.Estado(2));
            Assert.AreEqual(VidaOjo, maquina.Vida(3), "Los demás ojos no se tocan.");
        }

        [Test]
        public void AplicarDanio_ConMasDanioQueVida_SoloAbsorbeLoQueQueda_YCierraElOjo()
        {
            var maquina = new WeakPointStateMachine(TotalOjos, VidaOjo);

            float aplicado = maquina.AplicarDanio(0, VidaOjo * 3f);

            Assert.AreEqual(VidaOjo, aplicado);
            Assert.AreEqual(0f, maquina.Vida(0));
            Assert.AreEqual(EstadoOjo.Cerrado, maquina.Estado(0));
            Assert.AreEqual(TotalOjos - 1, maquina.OjosAbiertos);
        }

        [Test]
        public void UnOjoCerrado_NoRecibeDanio()
        {
            var maquina = new WeakPointStateMachine(TotalOjos, VidaOjo);
            maquina.AplicarDanio(0, VidaOjo);

            Assert.AreEqual(0f, maquina.AplicarDanio(0, 50f));
            Assert.AreEqual(0f, maquina.Vida(0));
        }

        [Test]
        public void DanioCeroONegativo_NoHaceNada()
        {
            var maquina = new WeakPointStateMachine(TotalOjos, VidaOjo);

            Assert.AreEqual(0f, maquina.AplicarDanio(0, 0f));
            Assert.AreEqual(0f, maquina.AplicarDanio(0, -10f));
            Assert.AreEqual(VidaOjo, maquina.Vida(0));
        }

        [Test]
        public void CuandoSeCierranTodos_TodosCerradosEsTrue()
        {
            var maquina = new WeakPointStateMachine(TotalOjos, VidaOjo);

            for (int ojo = 0; ojo < TotalOjos; ojo++)
                maquina.AplicarDanio(ojo, VidaOjo);

            Assert.IsTrue(maquina.TodosCerrados);
            Assert.AreEqual(0, maquina.OjosAbiertos);
        }

        [Test]
        public void AbrirTodos_ReabreLosOjosConLaVidaLlena()
        {
            var maquina = new WeakPointStateMachine(TotalOjos, VidaOjo);
            maquina.AplicarDanio(0, VidaOjo);
            maquina.AplicarDanio(1, 40f);

            maquina.AbrirTodos();

            Assert.AreEqual(TotalOjos, maquina.OjosAbiertos);
            Assert.AreEqual(VidaOjo, maquina.Vida(0));
            Assert.AreEqual(VidaOjo, maquina.Vida(1));
        }

        [Test]
        public void AbrirOjoParaDebug_SoloReabreEseOjo()
        {
            var maquina = new WeakPointStateMachine(TotalOjos, VidaOjo);
            maquina.AplicarDanio(0, VidaOjo);
            maquina.AplicarDanio(1, VidaOjo);

            maquina.AbrirOjoParaDebug(0);

            Assert.AreEqual(EstadoOjo.Abierto, maquina.Estado(0));
            Assert.AreEqual(EstadoOjo.Cerrado, maquina.Estado(1));
        }

        [Test]
        public void SoloUnOjoAbiertoAdmiteAncla()
        {
            Assert.IsTrue(WeakPointStateMachine.AdmiteAncla(EstadoOjo.Abierto));
            Assert.IsFalse(WeakPointStateMachine.AdmiteAncla(EstadoOjo.Cerrado));
        }

        [Test]
        public void ImpactoEnUnOjoAbierto_SeAdmiteConCualquierTipo()
        {
            var maquina = new WeakPointStateMachine(TotalOjos, VidaOjo);

            Assert.IsTrue(maquina.AdmiteImpacto(0, TipoImpactoOjo.Melee));
            Assert.IsTrue(maquina.AdmiteImpacto(0, TipoImpactoOjo.Disparo));
            Assert.IsTrue(maquina.AdmiteImpacto(0, TipoImpactoOjo.Disparo, atacanteEsAyla: false));
            Assert.IsTrue(maquina.AdmiteImpacto(0, TipoImpactoOjo.Melee, atacanteEsAyla: true));
        }

        [Test]
        public void ElDisparoDeAyla_NoSeAdmite()
        {
            var maquina = new WeakPointStateMachine(TotalOjos, VidaOjo);

            Assert.IsFalse(maquina.AdmiteImpacto(0, TipoImpactoOjo.Disparo, atacanteEsAyla: true));
        }

        [Test]
        public void ImpactoEnUnOjoCerrado_NoSeAdmite()
        {
            var maquina = new WeakPointStateMachine(TotalOjos, VidaOjo);
            maquina.AplicarDanio(0, VidaOjo);

            Assert.IsFalse(maquina.AdmiteImpacto(0, TipoImpactoOjo.Melee));
            Assert.IsFalse(maquina.AdmiteImpacto(0, TipoImpactoOjo.Disparo));
        }

        [Test]
        public void PonerEscudos_CubreATodosLosOjosAbiertos()
        {
            var maquina = new WeakPointStateMachine(TotalOjos, VidaOjo);

            maquina.PonerEscudos(ColorEscudo.Rojo);

            for (int ojo = 0; ojo < TotalOjos; ojo++)
                Assert.AreEqual(ColorEscudo.Rojo, maquina.Escudo(ojo));
        }

        [Test]
        public void UnOjoConEscudo_NoRecibeDanioNiAdmiteImpacto()
        {
            var maquina = new WeakPointStateMachine(TotalOjos, VidaOjo);
            maquina.PonerEscudos(ColorEscudo.Azul);

            Assert.AreEqual(0f, maquina.AplicarDanio(0, 50f));
            Assert.AreEqual(VidaOjo, maquina.Vida(0));
            Assert.IsFalse(maquina.AdmiteImpacto(0, TipoImpactoOjo.Melee));
        }

        [Test]
        public void RomperEscudo_SoloLoRompeElAtacanteDelColor()
        {
            var maquina = new WeakPointStateMachine(TotalOjos, VidaOjo);
            maquina.PonerEscudos(ColorEscudo.Azul);

            Assert.IsFalse(maquina.RomperEscudo(0, atacanteEsAyla: true));
            Assert.AreEqual(ColorEscudo.Azul, maquina.Escudo(0));

            Assert.IsTrue(maquina.RomperEscudo(0, atacanteEsAyla: false));
            Assert.AreEqual(ColorEscudo.Ninguno, maquina.Escudo(0));
            Assert.IsTrue(maquina.AdmiteImpacto(0, TipoImpactoOjo.Melee));
        }

        [Test]
        public void UnOjoConVariasCapas_SeRompenDeAfueraHaciaAdentro_YSoloEntoncesRecibeDanio()
        {
            var maquina = new WeakPointStateMachine(TotalOjos, VidaOjo);
            maquina.PonerEscudos(
                0,
                new[] { ColorEscudo.Rojo, ColorEscudo.Azul, ColorEscudo.Rojo }
            );

            Assert.AreEqual(3, maquina.CapasEscudo(0));
            Assert.AreEqual(ColorEscudo.Rojo, maquina.Escudo(0));
            Assert.AreEqual(0, maquina.CapasEscudo(1), "Los demás ojos no se tocan.");

            Assert.IsFalse(maquina.RomperEscudo(0, atacanteEsAyla: false));
            Assert.IsTrue(maquina.RomperEscudo(0, atacanteEsAyla: true));
            Assert.AreEqual(ColorEscudo.Azul, maquina.Escudo(0));
            Assert.AreEqual(0f, maquina.AplicarDanio(0, 50f));

            Assert.IsTrue(maquina.RomperEscudo(0, atacanteEsAyla: false));
            Assert.AreEqual(ColorEscudo.Rojo, maquina.Escudo(0));
            Assert.IsTrue(maquina.RomperEscudo(0, atacanteEsAyla: true));

            Assert.AreEqual(0, maquina.CapasEscudo(0));
            Assert.AreEqual(50f, maquina.AplicarDanio(0, 50f));
        }

        [Test]
        public void PonerEscudosEnUnOjoCerrado_NoLeDejaEscudo()
        {
            var maquina = new WeakPointStateMachine(TotalOjos, VidaOjo);
            maquina.AplicarDanio(0, VidaOjo);

            maquina.PonerEscudos(0, new[] { ColorEscudo.Azul });

            Assert.AreEqual(0, maquina.CapasEscudo(0));
        }

        [Test]
        public void AbrirTodos_QuitaLosEscudos()
        {
            var maquina = new WeakPointStateMachine(TotalOjos, VidaOjo);
            maquina.PonerEscudos(ColorEscudo.Azul);

            maquina.AbrirTodos();

            for (int ojo = 0; ojo < TotalOjos; ojo++)
                Assert.AreEqual(ColorEscudo.Ninguno, maquina.Escudo(ojo));
        }

        [Test]
        public void OjoFueraDeRango_Lanza()
        {
            var maquina = new WeakPointStateMachine(TotalOjos, VidaOjo);

            Assert.Throws<ArgumentOutOfRangeException>(() => maquina.Estado(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => maquina.Estado(TotalOjos));
            Assert.Throws<ArgumentOutOfRangeException>(() => maquina.AplicarDanio(TotalOjos, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => maquina.Escudo(TotalOjos));
        }
    }
}
