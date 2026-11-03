using NUnit.Framework;
using TDOM.Gameplay.Core;

namespace TDOM.Tests.EditMode
{
    public class ConfirmedCharacterSelectionTests
    {
        private ConfirmedCharacterSelection _selection;

        [SetUp]
        public void SetUp() => _selection = new ConfirmedCharacterSelection();

        [Test]
        public void elegir_un_personaje_libre_se_acepta() =>
            Assert.IsTrue(_selection.Elegir(1, ConfirmedCharacterSelection.Character.Ayla));

        [Test]
        public void elegir_el_personaje_de_otro_jugador_se_rechaza()
        {
            Assert.IsTrue(_selection.Elegir(1, ConfirmedCharacterSelection.Character.Ayla));
            Assert.IsFalse(_selection.Elegir(2, ConfirmedCharacterSelection.Character.Ayla));
        }

        [Test]
        public void marcar_listo_sin_personaje_no_cambia_nada()
        {
            _selection.MarcarListo(1);
            Assert.AreEqual(0, _selection.Players.Count);
            Assert.IsFalse(_selection.PuedeIniciar);
        }

        [Test]
        public void con_dos_jugadores_listos_se_puede_iniciar()
        {
            _selection.Elegir(1, ConfirmedCharacterSelection.Character.Ayla);
            _selection.Elegir(2, ConfirmedCharacterSelection.Character.Zendre);
            _selection.MarcarListo(1);
            _selection.MarcarListo(2);
            Assert.IsTrue(_selection.PuedeIniciar);
        }

        [Test]
        public void con_un_solo_jugador_listo_no_se_puede_iniciar()
        {
            _selection.Elegir(1, ConfirmedCharacterSelection.Character.Ayla);
            _selection.Elegir(2, ConfirmedCharacterSelection.Character.Zendre);
            _selection.MarcarListo(1);
            Assert.IsFalse(_selection.PuedeIniciar);
        }

        [Test]
        public void al_desconectarse_un_jugador_el_otro_deja_de_estar_listo()
        {
            _selection.Elegir(1, ConfirmedCharacterSelection.Character.Ayla);
            _selection.Elegir(2, ConfirmedCharacterSelection.Character.Zendre);
            _selection.MarcarListo(1);
            _selection.MarcarListo(2);
            _selection.Desconectar(2);
            Assert.AreEqual(1, _selection.Players.Count);
            Assert.IsFalse(_selection.Players[0].Ready);
            Assert.IsFalse(_selection.PuedeIniciar);
        }
    }
}
