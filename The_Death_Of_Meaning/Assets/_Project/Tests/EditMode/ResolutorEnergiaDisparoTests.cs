using NUnit.Framework;
using TDOM.Contracts;
using TDOM.Gameplay.Combat;

namespace TDOM.Tests.EditMode
{
    public class ResolutorEnergiaDisparoTests
    {
        private const float CostoCargado = 35f;
        private const float DanioLigero = 12f;
        private const float DanioCargado = 20f;

        private static readonly AttackStep[] Pasos =
        {
            new AttackStep { Damage = DanioLigero },
            new AttackStep { Damage = DanioLigero },
        };

        [TestCase(CostoCargado)]
        [TestCase(CostoCargado + 0.01f)]
        public void con_energia_suficiente_el_disparo_cargado_sale_cargado(float energia)
        {
            var evento = Resolver(Cargado(1), energia);

            Assert.AreEqual(AttackKind.Charged, evento.Kind);
            Assert.AreEqual(DanioCargado, evento.Damage);
        }

        [Test]
        public void sin_energia_para_el_cargado_sale_un_disparo_normal_del_mismo_paso()
        {
            var evento = Resolver(Cargado(1), CostoCargado - 0.01f);

            Assert.AreEqual(AttackKind.Light, evento.Kind);
            Assert.AreEqual(1, evento.ComboIndex);
            Assert.AreEqual(0f, evento.ChargeRatio);
            Assert.AreEqual(DanioLigero, evento.Damage);
        }

        [Test]
        public void un_disparo_normal_no_cambia_aunque_no_haya_energia()
        {
            var evento = Resolver(new AttackEvent(AttackKind.Light, 0, 0f, DanioLigero), 0f);

            Assert.AreEqual(AttackKind.Light, evento.Kind);
            Assert.AreEqual(DanioLigero, evento.Damage);
        }

        [Test]
        public void sin_pasos_ligeros_el_cargado_se_queda_igual()
        {
            var evento = ResolutorEnergiaDisparo.SegunEnergia(Cargado(0), 0f, CostoCargado, null);

            Assert.AreEqual(AttackKind.Charged, evento.Kind);
        }

        private static AttackEvent Cargado(int indice) =>
            new AttackEvent(AttackKind.Charged, indice, 0.5f, DanioCargado);

        private static AttackEvent Resolver(AttackEvent evento, float energia) =>
            ResolutorEnergiaDisparo.SegunEnergia(evento, energia, CostoCargado, Pasos);
    }
}
