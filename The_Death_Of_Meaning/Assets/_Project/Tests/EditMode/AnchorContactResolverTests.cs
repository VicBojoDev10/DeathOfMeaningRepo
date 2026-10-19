using NUnit.Framework;
using TDOM.Gameplay.Combat;

namespace TDOM.Tests.EditMode
{
    public class AnchorContactResolverTests
    {
        private const float Cerca = 1f;
        private const float Lejos = 3f;
        private const float EnContacto = 0f;

        private static ContactoAncla Contacto(TipoContactoAncla tipo, float distancia) =>
            new ContactoAncla(tipo, distancia);

        [Test]
        public void Sin_contactos_el_ancla_sigue_su_camino()
        {
            var decision = AnchorContactResolver.Resolver(new ContactoAncla[0]);

            Assert.AreEqual(AccionAncla.Seguir, decision.Accion);
        }

        [Test]
        public void Los_contactos_ignorados_no_detienen_el_ancla()
        {
            var decision = AnchorContactResolver.Resolver(
                new[] { Contacto(TipoContactoAncla.Ignorar, Cerca) }
            );

            Assert.AreEqual(AccionAncla.Seguir, decision.Accion);
        }

        [Test]
        public void Se_ancla_al_contacto_valido_mas_cercano_y_se_mueve_al_punto()
        {
            var decision = AnchorContactResolver.Resolver(
                new[]
                {
                    Contacto(TipoContactoAncla.Superficie, Lejos),
                    Contacto(TipoContactoAncla.OjoValido, Cerca),
                }
            );

            Assert.AreEqual(AccionAncla.Anclar, decision.Accion);
            Assert.AreEqual(1, decision.Indice);
            Assert.IsTrue(decision.MoverAlPunto);
        }

        [Test]
        public void Si_lo_mas_cercano_es_un_ojo_equivocado_el_ancla_se_retrae()
        {
            var decision = AnchorContactResolver.Resolver(
                new[]
                {
                    Contacto(TipoContactoAncla.OjoValido, Lejos),
                    Contacto(TipoContactoAncla.OjoEquivocado, Cerca),
                }
            );

            Assert.AreEqual(AccionAncla.Retraer, decision.Accion);
        }

        [Test]
        public void Un_ojo_equivocado_detras_de_uno_valido_no_retrae_el_ancla()
        {
            var decision = AnchorContactResolver.Resolver(
                new[]
                {
                    Contacto(TipoContactoAncla.OjoEquivocado, Lejos),
                    Contacto(TipoContactoAncla.OjoValido, Cerca),
                }
            );

            Assert.AreEqual(AccionAncla.Anclar, decision.Accion);
            Assert.AreEqual(1, decision.Indice);
        }

        [Test]
        public void En_contacto_inicial_se_prefiere_el_ojo_valido_sin_moverse()
        {
            var decision = AnchorContactResolver.Resolver(
                new[]
                {
                    Contacto(TipoContactoAncla.Superficie, EnContacto),
                    Contacto(TipoContactoAncla.OjoValido, EnContacto),
                }
            );

            Assert.AreEqual(AccionAncla.Anclar, decision.Accion);
            Assert.AreEqual(1, decision.Indice);
            Assert.IsFalse(decision.MoverAlPunto);
        }

        [Test]
        public void Un_impacto_por_delante_gana_al_contacto_inicial()
        {
            var decision = AnchorContactResolver.Resolver(
                new[]
                {
                    Contacto(TipoContactoAncla.OjoValido, EnContacto),
                    Contacto(TipoContactoAncla.Superficie, Cerca),
                }
            );

            Assert.AreEqual(AccionAncla.Anclar, decision.Accion);
            Assert.AreEqual(1, decision.Indice);
            Assert.IsTrue(decision.MoverAlPunto);
        }

        [Test]
        public void Tocar_solo_un_ojo_equivocado_retrae_el_ancla()
        {
            var decision = AnchorContactResolver.Resolver(
                new[] { Contacto(TipoContactoAncla.OjoEquivocado, EnContacto) }
            );

            Assert.AreEqual(AccionAncla.Retraer, decision.Accion);
        }

        [Test]
        public void Un_contacto_inicial_valido_gana_a_un_ojo_equivocado_inicial()
        {
            var decision = AnchorContactResolver.Resolver(
                new[]
                {
                    Contacto(TipoContactoAncla.OjoEquivocado, EnContacto),
                    Contacto(TipoContactoAncla.OjoValido, EnContacto),
                }
            );

            Assert.AreEqual(AccionAncla.Anclar, decision.Accion);
            Assert.AreEqual(1, decision.Indice);
        }
    }
}
