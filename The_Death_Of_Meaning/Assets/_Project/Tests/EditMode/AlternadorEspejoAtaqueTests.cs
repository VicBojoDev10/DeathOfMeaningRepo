using NUnit.Framework;
using TDOM.Contracts;
using TDOM.Gameplay.Combat;

namespace TDOM.Tests.EditMode
{
    public class AlternadorEspejoAtaqueTests
    {
        private AlternadorEspejoAtaque alternador;

        [SetUp]
        public void Setup()
        {
            alternador = new AlternadorEspejoAtaque();
        }

        private static AttackEvent Ligero(int indice) =>
            new AttackEvent(AttackKind.Light, indice, 0f, 10f);

        private static AttackEvent Cargado(int indice) =>
            new AttackEvent(AttackKind.Charged, indice, 1f, 25f);

        [Test]
        public void El_primer_ataque_basico_sale_normal()
        {
            Assert.IsFalse(alternador.Resolver(Ligero(0)));
        }

        [Test]
        public void Ataques_basicos_sueltos_se_alternan()
        {
            Assert.IsFalse(alternador.Resolver(Ligero(0)));
            Assert.IsTrue(alternador.Resolver(Ligero(0)));
            Assert.IsFalse(alternador.Resolver(Ligero(0)));
            Assert.IsTrue(alternador.Resolver(Ligero(0)));
        }

        [Test]
        public void Un_combo_conserva_el_mismo_lado_en_todos_sus_golpes()
        {
            alternador.Resolver(Ligero(0)); // normal
            Assert.IsTrue(alternador.Resolver(Ligero(0))); // combo nuevo: espejado
            Assert.IsTrue(alternador.Resolver(Ligero(1)));
            Assert.IsTrue(alternador.Resolver(Ligero(2)));
            Assert.IsFalse(alternador.Resolver(Ligero(0))); // combo nuevo: vuelve a normal
        }

        [Test]
        public void Un_cargado_directo_sale_normal_y_no_consume_la_alternancia()
        {
            Assert.IsFalse(alternador.Resolver(Ligero(0)));
            Assert.IsFalse(alternador.Resolver(Cargado(0)));
            Assert.IsTrue(alternador.Resolver(Ligero(0)));
        }

        [Test]
        public void Un_cargado_dentro_del_combo_hereda_el_lado_del_combo()
        {
            alternador.Resolver(Ligero(0));
            alternador.Resolver(Ligero(0)); // espejado
            alternador.Resolver(Ligero(1));
            Assert.IsTrue(alternador.Resolver(Cargado(2)));
        }

        [Test]
        public void Reiniciar_vuelve_al_estado_inicial()
        {
            alternador.Resolver(Ligero(0));
            alternador.Resolver(Ligero(0));
            alternador.Reiniciar();

            Assert.IsFalse(alternador.Espejado);
            Assert.IsFalse(alternador.Resolver(Ligero(0)));
        }
    }
}
