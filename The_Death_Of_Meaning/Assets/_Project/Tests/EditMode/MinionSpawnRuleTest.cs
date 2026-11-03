using NUnit.Framework;
using TDOM.Gameplay.Combat;

namespace TDOM.Tests.EditMode
{
    public class MinionSpawnRuleTest
    {
        [Test]
        public void la_primera_oleada_no_necesita_danio_previo()
        {
            var regla = new MinionSpawnRule();

            Assert.IsTrue(regla.PuedeSpawnear);
            Assert.AreEqual(3, regla.CantidadASpawnear(3));
        }

        [Test]
        public void fase_con_cero_minions_no_spawnea_ni_bloquea()
        {
            var regla = new MinionSpawnRule();

            regla.RegistrarSpawn(regla.CantidadASpawnear(0));

            Assert.AreEqual(0, regla.Vivos);
            Assert.IsTrue(regla.PuedeSpawnear);
        }

        [Test]
        public void con_minions_vivos_no_spawnea_mas()
        {
            var regla = new MinionSpawnRule();
            regla.RegistrarSpawn(3);
            regla.RegistrarMuerte();
            regla.RegistrarDanioAlJefe();

            Assert.AreEqual(2, regla.Vivos);
            Assert.IsFalse(regla.PuedeSpawnear);
            Assert.AreEqual(0, regla.CantidadASpawnear(5));
        }

        [Test]
        public void todos_muertos_sin_danio_al_jefe_no_spawnea()
        {
            var regla = new MinionSpawnRule();
            regla.RegistrarSpawn(2);
            regla.RegistrarMuerte();
            regla.RegistrarMuerte();

            Assert.AreEqual(0, regla.Vivos);
            Assert.IsFalse(regla.PuedeSpawnear);
        }

        [Test]
        public void danio_al_jefe_mientras_hay_vivos_no_cuenta()
        {
            var regla = new MinionSpawnRule();
            regla.RegistrarSpawn(2);
            regla.RegistrarDanioAlJefe();
            regla.RegistrarMuerte();
            regla.RegistrarMuerte();

            Assert.IsFalse(regla.PuedeSpawnear);
        }

        [Test]
        public void todos_muertos_y_danio_al_jefe_vuelve_a_spawnear()
        {
            var regla = new MinionSpawnRule();
            regla.RegistrarSpawn(2);
            regla.RegistrarMuerte();
            regla.RegistrarMuerte();
            regla.RegistrarDanioAlJefe();

            Assert.IsTrue(regla.PuedeSpawnear);
            Assert.AreEqual(7, regla.CantidadASpawnear(7));
        }

        [Test]
        public void cambio_de_fase_spawnea_aunque_queden_vivos_y_estos_se_quedan()
        {
            var regla = new MinionSpawnRule();
            regla.RegistrarSpawn(3);
            regla.RegistrarMuerte();
            regla.RegistrarMuerte();

            regla.LiberarPorCambioDeFase();

            Assert.IsTrue(regla.PuedeSpawnear);
            Assert.AreEqual(5, regla.CantidadASpawnear(5));

            regla.RegistrarSpawn(5);
            Assert.AreEqual(6, regla.Vivos);
        }

        [Test]
        public void despues_del_spawn_por_cambio_de_fase_vuelve_la_regla_normal()
        {
            var regla = new MinionSpawnRule();
            regla.RegistrarSpawn(3);
            regla.LiberarPorCambioDeFase();
            regla.RegistrarSpawn(5);

            Assert.IsFalse(regla.PuedeSpawnear);

            for (int i = 0; i < 8; i++)
                regla.RegistrarMuerte();
            Assert.IsFalse(regla.PuedeSpawnear);

            regla.RegistrarDanioAlJefe();
            Assert.IsTrue(regla.PuedeSpawnear);
        }

        [Test]
        public void cambio_de_fase_sin_vivos_no_necesita_danio_al_jefe()
        {
            var regla = new MinionSpawnRule();
            regla.RegistrarSpawn(2);
            regla.RegistrarMuerte();
            regla.RegistrarMuerte();

            regla.LiberarPorCambioDeFase();

            Assert.IsTrue(regla.PuedeSpawnear);
        }

        [Test]
        public void mas_muertes_que_vivos_no_baja_de_cero()
        {
            var regla = new MinionSpawnRule();
            regla.RegistrarSpawn(1);
            regla.RegistrarMuerte();
            regla.RegistrarMuerte();

            Assert.AreEqual(0, regla.Vivos);
        }
    }
}
