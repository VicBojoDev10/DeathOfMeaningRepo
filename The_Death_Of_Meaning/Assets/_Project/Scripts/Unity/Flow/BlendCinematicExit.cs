using System;
using System.Collections;
using TDOM.Gameplay.Core;
using Unity.Cinemachine;

namespace TDOM.Unity.Flow
{
    public sealed class BlendCinematicExit : ICinematicExit
    {
        private readonly CinemachineCamera _camara;
        private readonly ExitCameraPriority _prioridad;
        private readonly Action _detenerDirector;

        public BlendCinematicExit(
            CinemachineCamera camara,
            int prioridadElevada,
            Action detenerDirector
        )
        {
            _camara = camara;
            _prioridad = new ExitCameraPriority(prioridadElevada);
            _detenerDirector = detenerDirector;
        }

        public void Preparar() =>
            _camara.Priority.Value = _prioridad.Elevar(_camara.Priority.Value);

        public IEnumerator Salir()
        {
            _detenerDirector();
            _camara.Priority.Value = _prioridad.Restaurar(_camara.Priority.Value);
            yield break;
        }
    }
}
