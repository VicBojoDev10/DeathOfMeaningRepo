using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using TDOM.Gameplay.Combat;

namespace TDOM.Tests.EditMode
{
    public class WeakPointStateMachineTest
    {
        private const int Semilla = 1234;
        private const float DuracionBrillando = 4f;

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
        public void AbrirOjoParaDebug_AbreUnOjoCerrado_DuranteLaDuracionNormal()
        {
            var maquina = MaquinaEnFase1();
            int ojoCerrado = Buscar(maquina, EstadoOjo.Cerrado);

            maquina.AbrirOjoParaDebug(ojoCerrado);

            Assert.AreEqual(EstadoOjo.Abierto, maquina.Estado(ojoCerrado));
            maquina.Tick(9.9f);
            Assert.AreEqual(EstadoOjo.Abierto, maquina.Estado(ojoCerrado));
            maquina.Tick(0.1f);
            Assert.AreEqual(EstadoOjo.Enfriando, maquina.Estado(ojoCerrado));
        }

        [Test]
        public void Brillar_ojo_para_debug_deja_un_ojo_cerrado_listo_para_abrirse()
        {
            var maquina = MaquinaEnFase1();
            int ojoCerrado = Buscar(maquina, EstadoOjo.Cerrado);

            maquina.BrillarOjoParaDebug(ojoCerrado);

            Assert.AreEqual(EstadoOjo.Brillando, maquina.Estado(ojoCerrado));
            Assert.AreEqual(ResultadoGolpe.Abrio, maquina.GolpeMelee(ojoCerrado));
        }

        [TestCase(EstadoOjo.Brillando, true)]
        [TestCase(EstadoOjo.Abierto, true)]
        [TestCase(EstadoOjo.Cerrado, false)]
        [TestCase(EstadoOjo.Enfriando, false)]
        public void El_ancla_solo_se_engancha_a_ojos_brillando_o_abiertos(
            EstadoOjo estado,
            bool esperado
        )
        {
            Assert.AreEqual(esperado, WeakPointStateMachine.AdmiteAncla(estado));
        }

        [Test]
        public void Un_ojo_anclado_mientras_brilla_sigue_admitiendo_el_ancla_al_abrirse()
        {
            var maquina = MaquinaEnFase1();
            int ojo = Buscar(maquina, EstadoOjo.Brillando);
            Assert.IsTrue(WeakPointStateMachine.AdmiteAncla(maquina.Estado(ojo)));

            maquina.GolpeMelee(ojo);

            Assert.IsTrue(WeakPointStateMachine.AdmiteAncla(maquina.Estado(ojo)));
        }

        [Test]
        public void Brillar_ojo_para_debug_valida_el_indice()
        {
            var maquina = MaquinaEnFase1();

            Assert.Throws<ArgumentOutOfRangeException>(() => maquina.BrillarOjoParaDebug(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                maquina.BrillarOjoParaDebug(maquina.TotalOjos)
            );
        }

        [Test]
        public void AbrirOjoParaDebug_ValidaElIndice()
        {
            var maquina = MaquinaEnFase1();

            Assert.Throws<ArgumentOutOfRangeException>(() => maquina.AbrirOjoParaDebug(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                maquina.AbrirOjoParaDebug(maquina.TotalOjos)
            );
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
        public void El_golpe_de_ayla_a_un_ojo_brillando_lo_abre_sin_hacer_danio()
        {
            var maquina = MaquinaEnFase1();
            int ojo = Buscar(maquina, EstadoOjo.Brillando);

            Assert.IsFalse(maquina.ResolverImpacto(ojo, TipoImpactoOjo.Melee, true));
            Assert.AreEqual(EstadoOjo.Abierto, maquina.Estado(ojo));
        }

        [Test]
        public void El_golpe_de_ayla_a_un_ojo_abierto_hace_danio()
        {
            var maquina = MaquinaEnFase1();
            int ojo = AbrirUnOjo(maquina);

            Assert.IsTrue(maquina.ResolverImpacto(ojo, TipoImpactoOjo.Melee, true));
            Assert.AreEqual(EstadoOjo.Abierto, maquina.Estado(ojo));
        }

        [Test]
        public void El_golpe_de_zendre_a_un_ojo_no_lo_abre_ni_hace_danio()
        {
            var maquina = MaquinaEnFase1();
            int abierto = AbrirUnOjo(maquina);
            int brillando = Buscar(maquina, EstadoOjo.Brillando);

            Assert.IsFalse(maquina.ResolverImpacto(brillando, TipoImpactoOjo.Melee, false));
            Assert.IsFalse(maquina.ResolverImpacto(abierto, TipoImpactoOjo.Melee, false));
            Assert.AreEqual(EstadoOjo.Brillando, maquina.Estado(brillando));
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

        [Test]
        public void Ojos_por_fase_configurados_definen_cuantos_ojos_brillan()
        {
            var maquina = new WeakPointStateMachine(semilla: Semilla, ojosPorFase: new[] { 4, 1 });

            maquina.ActivarFase(1);
            Assert.AreEqual(4, Contar(maquina, EstadoOjo.Brillando));

            maquina.ActivarFase(2);
            Assert.AreEqual(1, Contar(maquina, EstadoOjo.Brillando));
            Assert.Throws<ArgumentOutOfRangeException>(() => maquina.ActivarFase(3));
        }

        [Test]
        public void Ojos_por_fase_vacios_o_negativos_lanzan()
        {
            Assert.Throws<ArgumentException>(() =>
                new WeakPointStateMachine(ojosPorFase: Array.Empty<int>())
            );
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new WeakPointStateMachine(ojosPorFase: new[] { 2, -1 })
            );
        }

        [Test]
        public void OjoBrillandoSinTocar_CaducaYSeEnciendeOtroDistinto()
        {
            var maquina = MaquinaConCaducidad();
            int ojo = Buscar(maquina, EstadoOjo.Brillando);

            maquina.Tick(DuracionBrillando);

            Assert.AreEqual(EstadoOjo.Cerrado, maquina.Estado(ojo));
            Assert.AreEqual(2, Contar(maquina, EstadoOjo.Brillando));
        }

        [Test]
        public void OjoBrillando_NoCaducaAntesDeTiempo()
        {
            var maquina = MaquinaConCaducidad();
            var brillando = Listar(maquina, EstadoOjo.Brillando);

            maquina.Tick(DuracionBrillando - 0.1f);

            CollectionAssert.AreEqual(brillando, Listar(maquina, EstadoOjo.Brillando));
        }

        [Test]
        public void CaducanDosOjosAlMismoTiempo_NingunoSeVuelveAEncender()
        {
            var maquina = MaquinaConCaducidad();
            var caducados = Listar(maquina, EstadoOjo.Brillando);
            Assert.AreEqual(2, caducados.Count);

            maquina.Tick(DuracionBrillando);

            foreach (int ojo in caducados)
                Assert.AreEqual(
                    EstadoOjo.Cerrado,
                    maquina.Estado(ojo),
                    $"El ojo {ojo} se repitió."
                );
            Assert.AreEqual(2, Contar(maquina, EstadoOjo.Brillando));
        }

        [Test]
        public void SinDuracionBrillando_ElOjoNoCaducaNunca()
        {
            var maquina = new WeakPointStateMachine(semilla: Semilla);
            maquina.ActivarFase(1);
            var brillando = Listar(maquina, EstadoOjo.Brillando);

            Avanzar(maquina, 100, 1f);

            CollectionAssert.AreEqual(brillando, Listar(maquina, EstadoOjo.Brillando));
        }

        [Test]
        public void OjoGolpeadoAntesDeCaducar_DuraLoNormalComoAbierto()
        {
            var maquina = MaquinaConCaducidad();
            int ojo = Buscar(maquina, EstadoOjo.Brillando);

            maquina.Tick(DuracionBrillando - 0.1f);
            Assert.AreEqual(ResultadoGolpe.Abrio, maquina.GolpeMelee(ojo));

            maquina.Tick(9.9f);
            Assert.AreEqual(EstadoOjo.Abierto, maquina.Estado(ojo));
            maquina.Tick(0.1f);
            Assert.AreEqual(EstadoOjo.Enfriando, maquina.Estado(ojo));
        }

        [Test]
        public void ConPocosOjos_ElOjoQueCaduca_NoEsElQueSeEnciendeDespues()
        {
            var maquina = new WeakPointStateMachine(
                totalOjos: 3,
                semilla: Semilla,
                ojosPorFase: new[] { 2 },
                duracionBrillando: DuracionBrillando
            );
            maquina.ActivarFase(1);
            var brillando = Listar(maquina, EstadoOjo.Brillando);
            int cerrado = Buscar(maquina, EstadoOjo.Cerrado);
            int golpeado = brillando[0];
            int caducara = brillando[1];
            maquina.GolpeMelee(golpeado);

            maquina.Tick(DuracionBrillando);

            Assert.AreEqual(EstadoOjo.Cerrado, maquina.Estado(caducara));
            Assert.AreEqual(EstadoOjo.Brillando, maquina.Estado(cerrado));
            Assert.AreEqual(2, ContarActivos(maquina));
        }

        [Test]
        public void ConUnSoloCandidatoCerrado_ElOjoQueCaducaPuedeRepetirse()
        {
            var maquina = new WeakPointStateMachine(
                totalOjos: 1,
                semilla: Semilla,
                ojosPorFase: new[] { 1 },
                duracionBrillando: DuracionBrillando
            );
            maquina.ActivarFase(1);

            maquina.Tick(DuracionBrillando);

            Assert.AreEqual(EstadoOjo.Brillando, maquina.Estado(0));
        }

        [Test]
        public void CambioDeFase_ConservaElTemporizadorDeLosOjosQueBrillan()
        {
            var maquina = new WeakPointStateMachine(
                semilla: Semilla,
                ojosPorFase: new[] { 1, 2 },
                duracionBrillando: DuracionBrillando
            );
            maquina.ActivarFase(1);
            int antiguo = Buscar(maquina, EstadoOjo.Brillando);

            maquina.Tick(3f);
            maquina.ActivarFase(2);
            maquina.Tick(1f);

            Assert.AreEqual(EstadoOjo.Cerrado, maquina.Estado(antiguo));
            Assert.AreEqual(2, Contar(maquina, EstadoOjo.Brillando));
        }

        [Test]
        public void Duracion_brillando_negativa_lanza()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new WeakPointStateMachine(duracionBrillando: -1f)
            );
        }

        [Test]
        public void MismaSemilla_ProduceLaMismaSecuenciaConCaducidad()
        {
            Assert.AreEqual(FirmaDeSecuencia(Semilla), FirmaDeSecuencia(Semilla));
        }

        [Test]
        public void SemillasDistintas_ProducenSecuenciasDistintas()
        {
            Assert.AreNotEqual(FirmaDeSecuencia(1), FirmaDeSecuencia(2));
        }

        private static WeakPointStateMachine MaquinaConCaducidad()
        {
            var maquina = new WeakPointStateMachine(
                semilla: Semilla,
                duracionBrillando: DuracionBrillando
            );
            maquina.ActivarFase(1);
            return maquina;
        }

        private static List<int> Listar(WeakPointStateMachine maquina, EstadoOjo estado)
        {
            var ojos = new List<int>();
            for (int ojo = 0; ojo < maquina.TotalOjos; ojo++)
            {
                if (maquina.Estado(ojo) == estado)
                    ojos.Add(ojo);
            }
            return ojos;
        }

        private static string FirmaDeSecuencia(int semilla)
        {
            var maquina = new WeakPointStateMachine(
                semilla: semilla,
                duracionBrillando: DuracionBrillando
            );
            maquina.ActivarFase(1);

            var firma = new StringBuilder();
            for (int ciclo = 0; ciclo < 10; ciclo++)
            {
                for (int ojo = 0; ojo < maquina.TotalOjos; ojo++)
                    firma.Append(maquina.Estado(ojo) == EstadoOjo.Brillando ? '1' : '0');
                firma.Append('|');
                maquina.Tick(DuracionBrillando);
            }
            return firma.ToString();
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
