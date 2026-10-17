using NUnit.Framework;
using TDOM.Contracts;
using TDOM.Gameplay.Combat;
using TDOM.POCO.ScriptableObjects;
using UnityEngine;

namespace TDOM.Tests.EditMode
{
    public class BossWeakPointDamageTest
    {
        private const int Semilla = 1234;
        private const int TotalOjos = 12;
        private const float DuracionAbierto = 10f;
        private const float Cooldown = 5f;
        private const float DuracionTransicion = 3f;
        private const float MargenTransicion = 0.1f;
        private const float DanioQueCruzaUmbral = 0.25f;
        private const float DanioMenor = 0.05f;
        private const float Dt60 = 1f / 60f;
        private const float Dt30 = 1f / 30f;
        private static readonly float[] Umbrales = { 0.8f, 0.6f, 0.4f, 0.2f, 0f };

        [Test]
        public void Disparo_a_un_ojo_cerrado_no_es_admitido_y_la_vida_no_baja()
        {
            var fases = CrearFases();
            var danio = new BossWeakPointDamage(CrearOjos(), fases);
            int ojo = Buscar(danio, EstadoOjo.Cerrado);

            Assert.IsFalse(danio.AdmiteImpacto(ojo, TipoImpactoOjo.Disparo));
            Assert.AreEqual(1f, fases.VidaActual);
            Assert.AreEqual(EstadoOjo.Cerrado, danio.Estado(ojo));
            Assert.AreEqual(1, fases.FaseActual);
        }

        [Test]
        public void Golpe_melee_a_un_ojo_cerrado_no_es_admitido()
        {
            var danio = new BossWeakPointDamage(CrearOjos(), CrearFases());
            int ojo = Buscar(danio, EstadoOjo.Cerrado);

            Assert.IsFalse(danio.AdmiteImpacto(ojo, TipoImpactoOjo.Melee));
            Assert.AreEqual(EstadoOjo.Cerrado, danio.Estado(ojo));
        }

        [Test]
        public void Golpe_melee_a_un_ojo_brillando_lo_abre_sin_hacer_danio()
        {
            var danio = new BossWeakPointDamage(CrearOjos(), CrearFases());
            int ojo = Buscar(danio, EstadoOjo.Brillando);

            Assert.IsFalse(danio.AdmiteImpacto(ojo, TipoImpactoOjo.Melee));
            Assert.AreEqual(EstadoOjo.Abierto, danio.Estado(ojo));
        }

        [Test]
        public void Disparo_a_un_ojo_brillando_no_es_admitido()
        {
            var danio = new BossWeakPointDamage(CrearOjos(), CrearFases());
            int ojo = Buscar(danio, EstadoOjo.Brillando);

            Assert.IsFalse(danio.AdmiteImpacto(ojo, TipoImpactoOjo.Disparo));
            Assert.AreEqual(EstadoOjo.Brillando, danio.Estado(ojo));
        }

        [Test]
        public void Golpe_y_disparo_a_un_ojo_abierto_son_admitidos()
        {
            var danio = new BossWeakPointDamage(CrearOjos(), CrearFases());
            int ojo = AbrirUnOjo(danio);

            Assert.IsTrue(danio.AdmiteImpacto(ojo, TipoImpactoOjo.Melee));
            Assert.IsTrue(danio.AdmiteImpacto(ojo, TipoImpactoOjo.Disparo));
        }

        [Test]
        public void Danio_a_un_ojo_abierto_baja_la_vida_sin_cambiar_de_fase_antes_del_umbral()
        {
            var fases = CrearFases();
            var danio = new BossWeakPointDamage(CrearOjos(), fases);
            int ojo = AbrirUnOjo(danio);

            Assert.IsTrue(danio.AdmiteImpacto(ojo, TipoImpactoOjo.Disparo));
            Assert.IsFalse(danio.AplicarDanio(DanioMenor));

            Assert.AreEqual(1f - DanioMenor, fases.VidaActual, 1e-5f);
            Assert.AreEqual(1, fases.FaseActual);
        }

        [Test]
        public void Danio_a_un_ojo_abierto_que_cruza_el_umbral_cambia_de_fase()
        {
            var fases = CrearFases();
            var danio = new BossWeakPointDamage(CrearOjos(), fases);
            int ojo = AbrirUnOjo(danio);

            Assert.IsTrue(danio.AdmiteImpacto(ojo, TipoImpactoOjo.Disparo));
            Assert.IsTrue(danio.AplicarDanio(DanioQueCruzaUmbral));

            Assert.AreEqual(2, fases.FaseActual);
            Assert.AreEqual(Umbrales[0], fases.VidaActual, 1e-5f);
        }

        [Test]
        public void Danio_que_deja_la_vida_justo_en_el_umbral_cambia_de_fase()
        {
            var fases = CrearFases();
            var danio = new BossWeakPointDamage(CrearOjos(), fases);

            Assert.IsTrue(danio.AplicarDanio(1f - Umbrales[0]));
            Assert.AreEqual(2, fases.FaseActual);
        }

        [Test]
        public void Al_cambiar_de_fase_los_ojos_se_reactivan_para_la_nueva_fase()
        {
            var fases = CrearFases();
            var danio = new BossWeakPointDamage(CrearOjos(), fases);
            int ojo = AbrirUnOjo(danio);

            danio.AplicarDanio(DanioQueCruzaUmbral);
            Avanzar(danio, fases, DuracionTransicion + MargenTransicion, Dt60);
            danio.AplicarDanio(DanioQueCruzaUmbral);
            Avanzar(danio, fases, DuracionTransicion + MargenTransicion, Dt60);
            danio.AplicarDanio(DanioQueCruzaUmbral);

            Assert.AreEqual(4, fases.FaseActual);
            Assert.AreEqual(
                3,
                Contar(danio, EstadoOjo.Brillando) + Contar(danio, EstadoOjo.Abierto)
            );
            Assert.AreNotEqual(EstadoOjo.Cerrado, danio.Estado(ojo));
        }

        [Test]
        public void El_jefe_pasa_de_fase_1_a_2_y_a_3_con_danio_en_ojos_abiertos()
        {
            var fases = CrearFases();
            var danio = new BossWeakPointDamage(CrearOjos(), fases);

            GolpearOjoAbierto(danio);
            Assert.AreEqual(2, fases.FaseActual);

            Avanzar(danio, fases, DuracionTransicion + MargenTransicion, Dt60);
            GolpearOjoAbierto(danio);
            Assert.AreEqual(3, fases.FaseActual);
        }

        [Test]
        public void Determinismo_con_dt_fijo_da_el_mismo_resultado_a_60_y_30_fps()
        {
            var fasesA = CrearFases();
            var fasesB = CrearFases();
            var a = new BossWeakPointDamage(CrearOjos(), fasesA);
            var b = new BossWeakPointDamage(CrearOjos(), fasesB);

            GolpearOjoAbierto(a);
            GolpearOjoAbierto(b);
            Avanzar(a, fasesA, DuracionAbierto + Cooldown, Dt60);
            Avanzar(b, fasesB, DuracionAbierto + Cooldown, Dt30);
            AssertMismoEstado(a, fasesA, b, fasesB);

            GolpearOjoAbierto(a);
            GolpearOjoAbierto(b);
            Avanzar(a, fasesA, DuracionAbierto, Dt60);
            Avanzar(b, fasesB, DuracionAbierto, Dt30);
            AssertMismoEstado(a, fasesA, b, fasesB);
        }

        private static void GolpearOjoAbierto(BossWeakPointDamage danio)
        {
            int ojo = AbrirUnOjo(danio);
            if (danio.AdmiteImpacto(ojo, TipoImpactoOjo.Disparo))
                danio.AplicarDanio(DanioQueCruzaUmbral);
        }

        private static WeakPointStateMachine CrearOjos() =>
            new WeakPointStateMachine(TotalOjos, DuracionAbierto, Cooldown, Semilla);

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

        private static int AbrirUnOjo(BossWeakPointDamage danio)
        {
            int ojo = Buscar(danio, EstadoOjo.Brillando);
            danio.AdmiteImpacto(ojo, TipoImpactoOjo.Melee);
            return ojo;
        }

        private static void Avanzar(
            BossWeakPointDamage danio,
            BossPhaseStateMachine fases,
            float segundos,
            float dt
        )
        {
            int pasos = Mathf.RoundToInt(segundos / dt);
            for (int i = 0; i < pasos; i++)
            {
                fases.Tick(dt);
                danio.Tick(dt);
            }
        }

        private static int Buscar(BossWeakPointDamage danio, EstadoOjo estado)
        {
            for (int ojo = 0; ojo < danio.TotalOjos; ojo++)
            {
                if (danio.Estado(ojo) == estado)
                    return ojo;
            }

            Assert.Fail($"No hay ningún ojo en estado {estado}.");
            return -1;
        }

        private static int Contar(BossWeakPointDamage danio, EstadoOjo estado)
        {
            int total = 0;
            for (int ojo = 0; ojo < danio.TotalOjos; ojo++)
            {
                if (danio.Estado(ojo) == estado)
                    total++;
            }
            return total;
        }

        private static void AssertMismoEstado(
            BossWeakPointDamage a,
            BossPhaseStateMachine fasesA,
            BossWeakPointDamage b,
            BossPhaseStateMachine fasesB
        )
        {
            Assert.AreEqual(fasesA.FaseActual, fasesB.FaseActual);
            Assert.AreEqual(fasesA.VidaActual, fasesB.VidaActual, 1e-5f);
            for (int ojo = 0; ojo < a.TotalOjos; ojo++)
                Assert.AreEqual(a.Estado(ojo), b.Estado(ojo), $"El ojo {ojo} difiere.");
        }
    }
}
