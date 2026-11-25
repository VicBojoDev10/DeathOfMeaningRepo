using NUnit.Framework;
using TDOM.Gameplay.Audio;

namespace TDOM.Tests.EditMode
{
    public class SonidoBotonTests
    {
        private const string Positivo = "positivo";
        private const string Negativo = "negativo";
        private const string Neutral = "neutral";
        private const string Especial = "especial";

        [TestCase(TipoSonidoBoton.Positivo, Positivo)]
        [TestCase(TipoSonidoBoton.Negativo, Negativo)]
        [TestCase(TipoSonidoBoton.Neutral, Neutral)]
        [TestCase(TipoSonidoBoton.Especial, Especial)]
        public void elige_el_clip_que_corresponde_a_cada_tipo(TipoSonidoBoton tipo, string esperado)
        {
            Assert.AreEqual(
                esperado,
                SonidoBoton.Elegir(tipo, Positivo, Negativo, Neutral, Especial)
            );
        }

        [Test]
        public void devuelve_nulo_si_el_clip_del_tipo_no_esta_asignado()
        {
            Assert.IsNull(
                SonidoBoton.Elegir<string>(
                    TipoSonidoBoton.Especial,
                    Positivo,
                    Negativo,
                    Neutral,
                    null
                )
            );
        }

        [Test]
        public void devuelve_nulo_con_un_tipo_desconocido()
        {
            Assert.IsNull(
                SonidoBoton.Elegir((TipoSonidoBoton)99, Positivo, Negativo, Neutral, Especial)
            );
        }

        [Test]
        public void el_tipo_por_defecto_es_neutral()
        {
            Assert.AreEqual(TipoSonidoBoton.Neutral, default(TipoSonidoBoton));
        }
    }
}
