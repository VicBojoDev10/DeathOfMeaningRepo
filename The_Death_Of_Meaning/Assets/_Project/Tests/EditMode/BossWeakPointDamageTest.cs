using System;
using System.Collections.Generic;
using NUnit.Framework;
using TDOM.Contracts;
using TDOM.Gameplay.Combat;
using TDOM.POCO.ScriptableObjects;
using UnityEngine;

namespace TDOM.Tests.EditMode
{
    public class BossWeakPointDamageTest
    {
        private const int TotalOjos = 8;
        private const float VidaOjo = 160f;
        private const float DuracionTransicion = 3f;
        private const float Tolerancia = 1e-4f;
        private static readonly float[] Umbrales = { 0.8f, 0.6f, 0.4f, 0.2f, 0f };
        private static readonly float[] VidaPorFase = { 0.2f, 0.2f, 0.2f, 0.2f, 0.2f };

        // ---------- Daño por ojo ----------

        [Test]
        public void AlEmpezar_TodosLosOjosEstanAbiertosYSePuedenGolpear()
        {
            var danio = new BossWeakPointDamage(CrearOjos(), CrearFases(), VidaPorFase);

            for (int ojo = 0; ojo < TotalOjos; ojo++)
            {
                Assert.AreEqual(EstadoOjo.Abierto, danio.Estado(ojo));
                Assert.IsTrue(danio.AdmiteImpacto(ojo, TipoImpactoOjo.Melee, false));
            }
        }

        [Test]
        public void MatarUnOjo_QuitaSuParteDeLaVidaDeLaFase()
        {
            var fases = CrearFases();
            var danio = new BossWeakPointDamage(CrearOjos(), fases, VidaPorFase);

            bool cambioDeFase = danio.AplicarDanioOjo(0, VidaOjo, out float fraccion);

            Assert.IsFalse(cambioDeFase);
            Assert.AreEqual(0.025f, fraccion, Tolerancia); // 0.2 de la fase / 8 ojos
            Assert.AreEqual(0.975f, fases.VidaActual, Tolerancia);
            Assert.AreEqual(EstadoOjo.Cerrado, danio.Estado(0));
        }

        [Test]
        public void UnGolpeParcial_QuitaVidaEnProporcionALaVidaQueLeQuitaAlOjo()
        {
            var fases = CrearFases();
            var danio = new BossWeakPointDamage(CrearOjos(), fases, VidaPorFase);

            danio.AplicarDanioOjo(2, VidaOjo / 2f, out float fraccion);

            Assert.AreEqual(0.0125f, fraccion, Tolerancia);
            Assert.AreEqual(0.9875f, fases.VidaActual, Tolerancia);
            Assert.AreEqual(EstadoOjo.Abierto, danio.Estado(2));
            Assert.AreEqual(VidaOjo / 2f, danio.VidaOjo(2), Tolerancia);
        }

        [Test]
        public void ElDanioQueSobraDeUnOjo_NoCuentaDeMas()
        {
            var fases = CrearFases();
            var danio = new BossWeakPointDamage(CrearOjos(), fases, VidaPorFase);

            danio.AplicarDanioOjo(0, VidaOjo * 10f, out float fraccion);

            Assert.AreEqual(0.025f, fraccion, Tolerancia);
            Assert.AreEqual(0.975f, fases.VidaActual, Tolerancia);
        }

        [Test]
        public void UnOjoCerrado_NoAdmiteImpactosNiBajaLaVidaDelJefe()
        {
            var fases = CrearFases();
            var danio = new BossWeakPointDamage(CrearOjos(), fases, VidaPorFase);
            danio.AplicarDanioOjo(0, VidaOjo, out _);
            float vida = fases.VidaActual;

            bool cambioDeFase = danio.AplicarDanioOjo(0, VidaOjo, out float fraccion);

            Assert.IsFalse(danio.AdmiteImpacto(0, TipoImpactoOjo.Melee, false));
            Assert.IsFalse(cambioDeFase);
            Assert.AreEqual(0f, fraccion);
            Assert.AreEqual(vida, fases.VidaActual);
        }

        [Test]
        public void ElDisparoDeAyla_NoSeAdmiteYElDeZendreSi()
        {
            var danio = new BossWeakPointDamage(CrearOjos(), CrearFases(), VidaPorFase);

            Assert.IsFalse(danio.AdmiteImpacto(0, TipoImpactoOjo.Disparo, atacanteEsAyla: true));
            Assert.IsTrue(danio.AdmiteImpacto(0, TipoImpactoOjo.Disparo, atacanteEsAyla: false));
            Assert.IsTrue(danio.AdmiteImpacto(0, TipoImpactoOjo.Melee, atacanteEsAyla: true));
        }

        // ---------- Cambio de fase ----------

        [Test]
        public void LaFaseNoCambiaMientrasQuedeUnOjoAbierto()
        {
            var fases = CrearFases();
            var danio = new BossWeakPointDamage(CrearOjos(), fases, VidaPorFase);

            for (int ojo = 0; ojo < TotalOjos - 1; ojo++)
                Assert.IsFalse(danio.AplicarDanioOjo(ojo, VidaOjo, out _));

            Assert.AreEqual(1, fases.FaseActual);
            Assert.AreEqual(EstadoOjo.Abierto, danio.Estado(TotalOjos - 1));
        }

        [Test]
        public void ElUltimoOjoCerrado_CambiaDeFaseYDejaLaVidaJustoEnElUmbral()
        {
            var fases = CrearFases();
            var danio = new BossWeakPointDamage(CrearOjos(), fases, VidaPorFase);

            bool cambioDeFase = CerrarTodos(danio);

            Assert.IsTrue(cambioDeFase);
            Assert.AreEqual(2, fases.FaseActual);
            Assert.AreEqual(Umbrales[0], fases.VidaActual, Tolerancia);
        }

        [Test]
        public void GolpesIrregulares_TambienDejanLaVidaJustoEnElUmbral()
        {
            var fases = CrearFases();
            var danio = new BossWeakPointDamage(CrearOjos(), fases, VidaPorFase);
            float[] golpes = { 33.3f, 71.9f, 12.7f, 160f };

            bool cambioDeFase = false;
            for (int ojo = 0; ojo < TotalOjos; ojo++)
            {
                int turno = 0;
                while (danio.Estado(ojo) == EstadoOjo.Abierto)
                {
                    cambioDeFase |= danio.AplicarDanioOjo(
                        ojo,
                        golpes[turno++ % golpes.Length],
                        out _
                    );
                }
            }

            Assert.IsTrue(cambioDeFase);
            Assert.AreEqual(2, fases.FaseActual);
            Assert.AreEqual(Umbrales[0], fases.VidaActual, Tolerancia);
        }

        [Test]
        public void DespuesDelCambioDeFase_LosOjosSiguenCerradosHastaReabrirlos()
        {
            var fases = CrearFases();
            var danio = new BossWeakPointDamage(CrearOjos(), fases, VidaPorFase);
            CerrarTodos(danio);

            Assert.IsTrue(danio.EsperandoReapertura);
            for (int ojo = 0; ojo < TotalOjos; ojo++)
            {
                Assert.AreEqual(EstadoOjo.Cerrado, danio.Estado(ojo));
                Assert.IsFalse(danio.AdmiteImpacto(ojo, TipoImpactoOjo.Melee, false));
            }

            Assert.IsFalse(danio.AplicarDanioOjo(0, VidaOjo, out float fraccion));
            Assert.AreEqual(0f, fraccion);
        }

        [Test]
        public void ReabrirOjos_AbreTodosConLaVidaLlena_YElDanioVuelveAContar()
        {
            var fases = CrearFases();
            var danio = new BossWeakPointDamage(CrearOjos(), fases, VidaPorFase);
            CerrarTodos(danio);
            TerminarTransicion(fases);

            danio.ReabrirOjos();

            Assert.IsFalse(danio.EsperandoReapertura);
            for (int ojo = 0; ojo < TotalOjos; ojo++)
            {
                Assert.AreEqual(EstadoOjo.Abierto, danio.Estado(ojo));
                Assert.AreEqual(VidaOjo, danio.VidaOjo(ojo));
            }

            danio.AplicarDanioOjo(0, VidaOjo, out float fraccion);
            Assert.AreEqual(0.025f, fraccion, Tolerancia);
            Assert.AreEqual(Umbrales[0] - 0.025f, fases.VidaActual, Tolerancia);
        }

        [Test]
        public void CadaFaseCierraSusOjosYBajaLaVidaHastaSuUmbral()
        {
            var fases = CrearFases();
            var danio = new BossWeakPointDamage(CrearOjos(), fases, VidaPorFase);

            for (int fase = 1; fase < Umbrales.Length; fase++)
            {
                Assert.AreEqual(fase, fases.FaseActual);
                Assert.IsTrue(
                    CerrarTodos(danio),
                    $"La fase {fase} debía terminar al cerrar todos los ojos."
                );
                Assert.AreEqual(Umbrales[fase - 1], fases.VidaActual, Tolerancia);
                TerminarTransicion(fases);
                danio.ReabrirOjos();
            }

            Assert.AreEqual(Umbrales.Length, fases.FaseActual);
        }

        [Test]
        public void CerrarTodosLosOjosDeLaUltimaFase_MataAlJefe()
        {
            var fases = CrearFases();
            var danio = new BossWeakPointDamage(CrearOjos(), fases, VidaPorFase);

            for (int fase = 1; fase < Umbrales.Length; fase++)
            {
                CerrarTodos(danio);
                TerminarTransicion(fases);
                danio.ReabrirOjos();
            }

            CerrarTodos(danio);

            Assert.IsTrue(fases.EstaMuerto);
            Assert.AreEqual(0f, fases.VidaActual, Tolerancia);
        }

        [Test]
        public void SinPerfilesDeFase_Lanza()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new BossWeakPointDamage(CrearOjos(), CrearFases(), null)
            );
            Assert.Throws<ArgumentException>(() =>
                new BossWeakPointDamage(CrearOjos(), CrearFases(), new float[0])
            );
        }

        // ---------- Escudos ----------

        private static string Texto(ColorEscudo[] capas)
        {
            string texto = "";
            foreach (var capa in capas)
                texto += capa == ColorEscudo.Azul ? "A" : "R";
            return texto;
        }

        [Test]
        public void SinPlanDeEscudos_NingunOjoTieneEscudoEnNingunaFase()
        {
            var fases = CrearFases();
            var danio = new BossWeakPointDamage(CrearOjos(), fases, VidaPorFase);

            CerrarTodos(danio);
            TerminarTransicion(fases);
            danio.ReabrirOjos();

            for (int ojo = 0; ojo < TotalOjos; ojo++)
                Assert.AreEqual(0, danio.CapasEscudo(ojo));
        }

        [Test]
        public void PlanEscudos_FasesSinEscudoNoTienenCapas()
        {
            var plan = new PlanEscudos(ColorEscudo.Azul);

            Assert.AreEqual(0, plan.Capas(1, 0, TotalOjos).Length);
            Assert.AreEqual(0, plan.Capas(6, 0, TotalOjos).Length);
        }

        [Test]
        public void PlanEscudos_Fases2Y3_UnaCapaYLaTerceraEsDelColorOpuesto()
        {
            var azulPrimero = new PlanEscudos(ColorEscudo.Azul);
            var rojoPrimero = new PlanEscudos(ColorEscudo.Rojo);

            Assert.AreEqual("A", Texto(azulPrimero.Capas(2, 0, TotalOjos)));
            Assert.AreEqual("R", Texto(azulPrimero.Capas(3, 0, TotalOjos)));
            Assert.AreEqual("R", Texto(rojoPrimero.Capas(2, 3, TotalOjos)));
            Assert.AreEqual("A", Texto(rojoPrimero.Capas(3, 3, TotalOjos)));
        }

        [Test]
        public void PlanEscudos_ConSemilla_NuncaRepiteColorEnLasFases2Y3_YElColorDeLaFase2Varia()
        {
            bool salioAzul = false;
            bool salioRojo = false;
            for (int semilla = 0; semilla < 50; semilla++)
            {
                var plan = new PlanEscudos(semilla);
                Assert.AreNotEqual(
                    Texto(plan.Capas(2, 0, TotalOjos)),
                    Texto(plan.Capas(3, 0, TotalOjos))
                );
                salioAzul |= Texto(plan.Capas(2, 0, TotalOjos)) == "A";
                salioRojo |= Texto(plan.Capas(2, 0, TotalOjos)) == "R";
            }

            Assert.IsTrue(salioAzul && salioRojo, "El color de la fase 2 debía variar.");
        }

        [Test]
        public void PlanEscudos_Fase4_MitadAzulYMitadRoja_UnaCapaPorOjo()
        {
            for (int semilla = 0; semilla < 50; semilla++)
            {
                var plan = new PlanEscudos(semilla);
                int azules = 0;
                int rojos = 0;
                for (int ojo = 0; ojo < TotalOjos; ojo++)
                {
                    string capas = Texto(plan.Capas(4, ojo, TotalOjos));
                    if (capas == "A")
                        azules++;
                    else if (capas == "R")
                        rojos++;
                    else
                        Assert.Fail($"Fase 4: se esperaba una capa y salió '{capas}'.");
                }

                Assert.AreEqual(TotalOjos / 2, azules);
                Assert.AreEqual(TotalOjos / 2, rojos);
            }
        }

        [Test]
        public void PlanEscudos_Fase5_MitadRojoAzulRojoYMitadAzulRojoAzul()
        {
            for (int semilla = 0; semilla < 50; semilla++)
            {
                var plan = new PlanEscudos(semilla);
                int rar = 0;
                int ara = 0;
                for (int ojo = 0; ojo < TotalOjos; ojo++)
                {
                    string capas = Texto(plan.Capas(5, ojo, TotalOjos));
                    if (capas == "RAR")
                        rar++;
                    else if (capas == "ARA")
                        ara++;
                    else
                        Assert.Fail($"Fase 5: combinación inesperada '{capas}'.");
                }

                Assert.AreEqual(TotalOjos / 2, rar);
                Assert.AreEqual(TotalOjos / 2, ara);
            }
        }

        [Test]
        public void PlanEscudos_ElRepartoDependeDeLaSemillaPeroSiempreEsElMismoParaUnaSemilla()
        {
            Assert.AreEqual(RepartoFase4(7), RepartoFase4(7));

            var distintos = new HashSet<string>();
            for (int semilla = 0; semilla < 50; semilla++)
                distintos.Add(RepartoFase4(semilla));
            Assert.Greater(distintos.Count, 1);
        }

        private static string RepartoFase4(int semilla)
        {
            var plan = new PlanEscudos(semilla);
            string texto = "";
            for (int ojo = 0; ojo < TotalOjos; ojo++)
                texto += Texto(plan.Capas(4, ojo, TotalOjos));
            return texto;
        }

        [Test]
        public void PlanEscudos_SinColorInicial_Lanza()
        {
            Assert.Throws<ArgumentException>(() => new PlanEscudos(ColorEscudo.Ninguno));
        }

        [Test]
        public void EscudoAzul_LoRompeZendreYElRojoAyla()
        {
            Assert.IsTrue(EscudoOjo.Rompe(ColorEscudo.Azul, atacanteEsAyla: false));
            Assert.IsFalse(EscudoOjo.Rompe(ColorEscudo.Azul, atacanteEsAyla: true));
            Assert.IsTrue(EscudoOjo.Rompe(ColorEscudo.Rojo, atacanteEsAyla: true));
            Assert.IsFalse(EscudoOjo.Rompe(ColorEscudo.Rojo, atacanteEsAyla: false));
            Assert.IsFalse(EscudoOjo.Rompe(ColorEscudo.Ninguno, atacanteEsAyla: true));
        }

        [Test]
        public void ElEscudoSePuedeEmpaquetarYLeerCapaPorCapa()
        {
            byte empaquetado = EscudoOjo.Empaquetar(
                new[] { ColorEscudo.Rojo, ColorEscudo.Azul, ColorEscudo.Rojo }
            );

            Assert.AreEqual(3, EscudoOjo.NumCapas(empaquetado));
            Assert.AreEqual(ColorEscudo.Rojo, EscudoOjo.Capa(empaquetado, 0));
            Assert.AreEqual(ColorEscudo.Azul, EscudoOjo.Capa(empaquetado, 1));
            Assert.AreEqual(ColorEscudo.Rojo, EscudoOjo.Capa(empaquetado, 2));

            byte sinExterior = EscudoOjo.SinExterior(empaquetado);
            Assert.AreEqual(2, EscudoOjo.NumCapas(sinExterior));
            Assert.AreEqual(ColorEscudo.Azul, EscudoOjo.Exterior(sinExterior));
            Assert.AreEqual(0, EscudoOjo.NumCapas(EscudoOjo.Empaquetar(new ColorEscudo[0])));
        }

        [Test]
        public void EnLaFase1_LosOjosNoTienenEscudo()
        {
            var danio = new BossWeakPointDamage(
                CrearOjos(),
                CrearFases(),
                VidaPorFase,
                new PlanEscudos(ColorEscudo.Azul)
            );

            for (int ojo = 0; ojo < TotalOjos; ojo++)
                Assert.AreEqual(ColorEscudo.Ninguno, danio.Escudo(ojo));
        }

        [Test]
        public void AlReabrirLosOjosEnLaFase2_TodosTienenElEscudoDeLaFase()
        {
            var danio = CrearEnFase2ConEscudo(CrearFases(), ColorEscudo.Azul);

            for (int ojo = 0; ojo < TotalOjos; ojo++)
            {
                Assert.AreEqual(EstadoOjo.Abierto, danio.Estado(ojo));
                Assert.AreEqual(ColorEscudo.Azul, danio.Escudo(ojo));
            }
        }

        [Test]
        public void UnOjoConEscudo_NoAdmiteDanioNiLaVidaBaja()
        {
            var fases = CrearFases();
            var danio = CrearEnFase2ConEscudo(fases, ColorEscudo.Azul);
            float vida = fases.VidaActual;

            bool cambioDeFase = danio.AplicarDanioOjo(0, VidaOjo, out float fraccion);

            Assert.IsFalse(danio.AdmiteImpacto(0, TipoImpactoOjo.Melee, false));
            Assert.IsFalse(cambioDeFase);
            Assert.AreEqual(0f, fraccion);
            Assert.AreEqual(vida, fases.VidaActual);
            Assert.AreEqual(VidaOjo, danio.VidaOjo(0));
        }

        [Test]
        public void ElAtacanteDelColorCorrecto_RompeElEscudoSinHacerDanio()
        {
            var fases = CrearFases();
            var danio = CrearEnFase2ConEscudo(fases, ColorEscudo.Azul);
            float vida = fases.VidaActual;

            bool roto = danio.IntentarRomperEscudo(0, TipoImpactoOjo.Melee, atacanteEsAyla: false);

            Assert.IsTrue(roto);
            Assert.AreEqual(ColorEscudo.Ninguno, danio.Escudo(0));
            Assert.AreEqual(
                ColorEscudo.Azul,
                danio.Escudo(1),
                "Solo se rompe el escudo de ese ojo."
            );
            Assert.AreEqual(vida, fases.VidaActual);
            Assert.AreEqual(VidaOjo, danio.VidaOjo(0));
        }

        [Test]
        public void ElAtacanteDelColorEquivocado_NoRompeElEscudo()
        {
            var danio = CrearEnFase2ConEscudo(CrearFases(), ColorEscudo.Azul);

            Assert.IsFalse(
                danio.IntentarRomperEscudo(0, TipoImpactoOjo.Melee, atacanteEsAyla: true)
            );
            Assert.AreEqual(ColorEscudo.Azul, danio.Escudo(0));
        }

        [Test]
        public void ElDisparoDeAyla_NoRompeNiUnEscudoRojo()
        {
            var danio = CrearEnFase2ConEscudo(CrearFases(), ColorEscudo.Rojo);

            Assert.IsFalse(
                danio.IntentarRomperEscudo(0, TipoImpactoOjo.Disparo, atacanteEsAyla: true)
            );
            Assert.IsTrue(
                danio.IntentarRomperEscudo(0, TipoImpactoOjo.Melee, atacanteEsAyla: true)
            );
        }

        [Test]
        public void SinEscudo_ElOjoVuelveALaMecanicaNormal()
        {
            var danio = CrearEnFase2ConEscudo(CrearFases(), ColorEscudo.Rojo);
            danio.IntentarRomperEscudo(0, TipoImpactoOjo.Melee, atacanteEsAyla: true);

            Assert.IsTrue(danio.AdmiteImpacto(0, TipoImpactoOjo.Melee, true));
            danio.AplicarDanioOjo(0, VidaOjo, out float fraccion);

            Assert.AreEqual(EstadoOjo.Cerrado, danio.Estado(0));
            Assert.AreEqual(0.025f, fraccion, Tolerancia);
        }

        [Test]
        public void EnLaFase3_ElEscudoEsDelOtroColor()
        {
            var fases = CrearFases();
            var danio = CrearEnFase2ConEscudo(fases, ColorEscudo.Azul);
            AvanzarDeFase(danio, fases);

            for (int ojo = 0; ojo < TotalOjos; ojo++)
            {
                Assert.AreEqual(1, danio.CapasEscudo(ojo));
                Assert.AreEqual(ColorEscudo.Rojo, danio.Escudo(ojo));
            }
        }

        [Test]
        public void EnLaFase4_LosOjosLlevanUnaCapaMitadAzulYMitadRoja()
        {
            var danio = CrearEnFase4(CrearFases());

            int azules = 0;
            int rojos = 0;
            for (int ojo = 0; ojo < TotalOjos; ojo++)
            {
                Assert.AreEqual(1, danio.CapasEscudo(ojo));
                if (danio.Escudo(ojo) == ColorEscudo.Azul)
                    azules++;
                else
                    rojos++;
            }

            Assert.AreEqual(TotalOjos / 2, azules);
            Assert.AreEqual(TotalOjos / 2, rojos);
        }

        [Test]
        public void EnLaFase5_LosOjosLlevanTresCapasQueSeRompenDeAfueraHaciaAdentro()
        {
            var fases = CrearFases();
            var danio = CrearEnFase4(fases);
            AvanzarDeFase(danio, fases);

            int ojoRojo = -1;
            for (int ojo = 0; ojo < TotalOjos; ojo++)
            {
                Assert.AreEqual(3, danio.CapasEscudo(ojo));
                if (ojoRojo < 0 && danio.Escudo(ojo) == ColorEscudo.Rojo)
                    ojoRojo = ojo; // rojo-azul-rojo
            }
            Assert.GreaterOrEqual(ojoRojo, 0);

            // Capa 1 (roja): solo Ayla; mientras tanto el ojo no recibe daño.
            Assert.IsFalse(
                danio.IntentarRomperEscudo(ojoRojo, TipoImpactoOjo.Melee, atacanteEsAyla: false)
            );
            Assert.IsTrue(
                danio.IntentarRomperEscudo(ojoRojo, TipoImpactoOjo.Melee, atacanteEsAyla: true)
            );
            Assert.AreEqual(2, danio.CapasEscudo(ojoRojo));
            Assert.IsFalse(danio.AdmiteImpacto(ojoRojo, TipoImpactoOjo.Melee, true));

            // Capa 2 (azul): ahora Ayla ya no la rompe, Zendre sí.
            Assert.AreEqual(ColorEscudo.Azul, danio.Escudo(ojoRojo));
            Assert.IsFalse(
                danio.IntentarRomperEscudo(ojoRojo, TipoImpactoOjo.Melee, atacanteEsAyla: true)
            );
            Assert.IsTrue(
                danio.IntentarRomperEscudo(ojoRojo, TipoImpactoOjo.Melee, atacanteEsAyla: false)
            );
            Assert.IsFalse(danio.AdmiteImpacto(ojoRojo, TipoImpactoOjo.Melee, true));

            // Capa 3 (roja): Ayla, y con eso el ojo queda normal.
            Assert.AreEqual(ColorEscudo.Rojo, danio.Escudo(ojoRojo));
            Assert.IsTrue(
                danio.IntentarRomperEscudo(ojoRojo, TipoImpactoOjo.Melee, atacanteEsAyla: true)
            );
            Assert.AreEqual(0, danio.CapasEscudo(ojoRojo));
            Assert.IsTrue(danio.AdmiteImpacto(ojoRojo, TipoImpactoOjo.Melee, true));
        }

        [Test]
        public void MientrasSeEsperaLaReapertura_NoSeRompenEscudos()
        {
            var danio = new BossWeakPointDamage(
                CrearOjos(),
                CrearFases(),
                VidaPorFase,
                new PlanEscudos(ColorEscudo.Azul)
            );
            CerrarTodos(danio);

            Assert.IsFalse(danio.IntentarRomperEscudo(0, TipoImpactoOjo.Melee, false));
        }

        // ---------- Ayudas ----------

        private static BossWeakPointDamage CrearEnFase2ConEscudo(
            BossPhaseStateMachine fases,
            ColorEscudo color
        )
        {
            var danio = new BossWeakPointDamage(
                CrearOjos(),
                fases,
                VidaPorFase,
                new PlanEscudos(color)
            );
            CerrarTodos(danio);
            TerminarTransicion(fases);
            danio.ReabrirOjos();
            return danio;
        }

        // Rompe todos los escudos (cada capa con quien le toca), cierra todos los ojos y pasa a la
        // fase siguiente.
        private static void AvanzarDeFase(BossWeakPointDamage danio, BossPhaseStateMachine fases)
        {
            for (int ojo = 0; ojo < danio.TotalOjos; ojo++)
            {
                while (danio.CapasEscudo(ojo) > 0)
                {
                    bool ayla = danio.Escudo(ojo) == ColorEscudo.Rojo;
                    Assert.IsTrue(danio.IntentarRomperEscudo(ojo, TipoImpactoOjo.Melee, ayla));
                }
            }

            CerrarTodos(danio);
            TerminarTransicion(fases);
            danio.ReabrirOjos();
        }

        private static BossWeakPointDamage CrearEnFase4(BossPhaseStateMachine fases)
        {
            var danio = CrearEnFase2ConEscudo(fases, ColorEscudo.Azul); // fase 2
            AvanzarDeFase(danio, fases); // fase 3
            AvanzarDeFase(danio, fases); // fase 4
            return danio;
        }

        private static WeakPointStateMachine CrearOjos() =>
            new WeakPointStateMachine(TotalOjos, VidaOjo);

        private static BossPhaseStateMachine CrearFases()
        {
            var perfiles = new BossPhaseProfile[Umbrales.Length];
            for (int i = 0; i < Umbrales.Length; i++)
            {
                var perfil = ScriptableObject.CreateInstance<BossPhaseProfile>();
                perfil.Fase = i + 1;
                perfil.OrdenDeAtaque = new[] { new BossAttackStep(BossAttackKind.Basico, 10) };
                perfil.VidaTransicion = Umbrales[i];
                perfil.DuracionTransicion = DuracionTransicion;
                perfiles[i] = perfil;
            }
            return new BossPhaseStateMachine(perfiles);
        }

        // El jefe ignora el daño mientras dura la transición, y los ojos se reabren al terminarla.
        private static void TerminarTransicion(BossPhaseStateMachine fases) =>
            fases.Tick(DuracionTransicion + 0.1f);

        // Cierra todos los ojos abiertos de un golpe cada uno. Devuelve true si cambió la fase.
        private static bool CerrarTodos(BossWeakPointDamage danio)
        {
            bool cambioDeFase = false;
            for (int ojo = 0; ojo < danio.TotalOjos; ojo++)
            {
                if (danio.Estado(ojo) == EstadoOjo.Abierto)
                    cambioDeFase |= danio.AplicarDanioOjo(ojo, VidaOjo, out _);
            }
            return cambioDeFase;
        }
    }
}
