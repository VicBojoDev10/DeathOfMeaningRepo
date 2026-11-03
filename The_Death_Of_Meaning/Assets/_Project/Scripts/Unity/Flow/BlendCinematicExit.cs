using System;
using System.Collections;
using TDOM.Gameplay.Core;
using Unity.Cinemachine;
using UnityEngine;

namespace TDOM.Unity.Flow
{
    public sealed class BlendCinematicExit : ICinematicExit
    {
        private readonly CinemachineCamera _camara;
        private readonly ExitCameraPriority _prioridad;
        private readonly float _margenEspera;
        private readonly Action _detenerDirector;

        public BlendCinematicExit(
            CinemachineCamera camara,
            int prioridadElevada,
            float margenEspera,
            Action detenerDirector
        )
        {
            _camara = camara;
            _prioridad = new ExitCameraPriority(prioridadElevada);
            _margenEspera = margenEspera;
            _detenerDirector = detenerDirector;
        }

        public void Preparar() =>
            _camara.Priority.Value = _prioridad.Elevar(_camara.Priority.Value);

        public IEnumerator Salir()
        {
            _detenerDirector();
            Cancelar();

            yield return null;

            var cerebro = CinemachineCore.FindPotentialTargetBrain(_camara);
            if (cerebro == null)
                yield break;

            float limite = cerebro.DefaultBlend.Time + _margenEspera;
            float transcurrido = 0f;
            while (cerebro.IsBlending && transcurrido < limite)
            {
                transcurrido += Time.deltaTime;
                yield return null;
            }
        }

        public void Cancelar() =>
            _camara.Priority.Value = _prioridad.Restaurar(_camara.Priority.Value);
    }
}
