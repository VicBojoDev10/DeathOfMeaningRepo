using NUnit.Framework;
using TDOM.Gameplay.Core;

namespace TDOM.Tests.EditMode
{
    public class ExitCameraPriorityTests
    {
        private const int PrioridadElevada = 100;
        private const int PrioridadCinematica = -100;

        [Test]
        public void Elevar_devuelve_la_prioridad_alta()
        {
            var prioridad = new ExitCameraPriority(PrioridadElevada);

            Assert.AreEqual(PrioridadElevada, prioridad.Elevar(PrioridadCinematica));
        }

        [Test]
        public void Restaurar_devuelve_la_prioridad_que_tenia_antes_de_elevar()
        {
            var prioridad = new ExitCameraPriority(PrioridadElevada);

            int elevada = prioridad.Elevar(PrioridadCinematica);

            Assert.AreEqual(PrioridadCinematica, prioridad.Restaurar(elevada));
        }

        [Test]
        public void Restaurar_sin_elevar_no_cambia_la_prioridad()
        {
            var prioridad = new ExitCameraPriority(PrioridadElevada);

            Assert.AreEqual(PrioridadCinematica, prioridad.Restaurar(PrioridadCinematica));
        }

        [Test]
        public void Elevar_dos_veces_no_pierde_la_prioridad_original()
        {
            var prioridad = new ExitCameraPriority(PrioridadElevada);

            int elevada = prioridad.Elevar(PrioridadCinematica);
            elevada = prioridad.Elevar(elevada);

            Assert.AreEqual(PrioridadCinematica, prioridad.Restaurar(elevada));
        }

        [Test]
        public void Restaurar_dos_veces_no_vuelve_a_cambiar_la_prioridad()
        {
            var prioridad = new ExitCameraPriority(PrioridadElevada);

            int restaurada = prioridad.Restaurar(prioridad.Elevar(PrioridadCinematica));

            Assert.AreEqual(restaurada, prioridad.Restaurar(restaurada));
        }
    }
}
