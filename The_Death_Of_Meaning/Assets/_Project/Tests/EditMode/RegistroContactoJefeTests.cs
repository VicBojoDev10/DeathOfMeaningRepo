using System;
using NUnit.Framework;
using TDOM.Gameplay.Combat;

namespace TDOM.Tests.EditMode
{
    public class RegistroContactoJefeTests
    {
        private const ulong Ayla = 1;
        private const ulong Zendre = 2;

        [Test]
        public void El_primer_contacto_golpea()
        {
            var registro = new RegistroContactoJefe(1f);

            Assert.IsTrue(registro.TryGolpear(Ayla, 0f));
        }

        [Test]
        public void No_vuelve_a_golpear_dentro_del_intervalo()
        {
            var registro = new RegistroContactoJefe(1f);
            registro.TryGolpear(Ayla, 0f);

            Assert.IsFalse(registro.TryGolpear(Ayla, 0.5f));
            Assert.IsFalse(registro.TryGolpear(Ayla, 0.99f));
        }

        [Test]
        public void Vuelve_a_golpear_al_cumplirse_el_intervalo()
        {
            var registro = new RegistroContactoJefe(1f);
            registro.TryGolpear(Ayla, 0f);

            Assert.IsTrue(registro.TryGolpear(Ayla, 1f));
        }

        [Test]
        public void Cada_jugador_lleva_su_propia_pausa()
        {
            var registro = new RegistroContactoJefe(1f);
            registro.TryGolpear(Ayla, 0f);

            Assert.IsTrue(registro.TryGolpear(Zendre, 0.1f));
            Assert.IsFalse(registro.TryGolpear(Ayla, 0.1f));
        }

        [Test]
        public void Un_golpe_rechazado_no_extiende_la_pausa()
        {
            var registro = new RegistroContactoJefe(1f);
            registro.TryGolpear(Ayla, 0f);
            registro.TryGolpear(Ayla, 0.9f);

            Assert.IsTrue(registro.TryGolpear(Ayla, 1f));
        }

        [Test]
        public void Olvidar_libera_al_jugador()
        {
            var registro = new RegistroContactoJefe(1f);
            registro.TryGolpear(Ayla, 0f);

            registro.Olvidar(Ayla);

            Assert.IsTrue(registro.TryGolpear(Ayla, 0.1f));
        }

        [Test]
        public void Con_intervalo_cero_siempre_golpea()
        {
            var registro = new RegistroContactoJefe(0f);
            registro.TryGolpear(Ayla, 0f);

            Assert.IsTrue(registro.TryGolpear(Ayla, 0f));
        }

        [Test]
        public void Un_intervalo_negativo_es_invalido()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new RegistroContactoJefe(-1f));
        }
    }
}
