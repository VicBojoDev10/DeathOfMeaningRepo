using System.Collections.Generic;
using UnityEngine;

namespace TDOM.Gameplay.Camera
{
    public sealed class LockOnSwitcher
    {
        private const float DeltaMinimaCuadrada = 0.0001f;

        private readonly LockOnSwitchSettings _ajustes;
        private bool _armado = true;

        public LockOnSwitcher(LockOnSwitchSettings ajustes) => _ajustes = ajustes;

        public bool TryCambiar(
            Vector2 stick,
            Vector3 origen,
            Vector3 puntoActual,
            int idActual,
            IReadOnlyList<LockOnCandidate> candidatos,
            out LockOnCandidate elegido
        )
        {
            elegido = default;
            float magnitud = stick.magnitude;

            if (magnitud < _ajustes.ResetThreshold)
            {
                _armado = true;
                return false;
            }

            if (!_armado || magnitud < _ajustes.SwitchThreshold)
                return false;

            _armado = false;
            return TryElegirEnDireccion(
                stick / magnitud,
                origen,
                puntoActual,
                idActual,
                candidatos,
                out elegido
            );
        }

        private bool TryElegirEnDireccion(
            Vector2 direccion,
            Vector3 origen,
            Vector3 puntoActual,
            int idActual,
            IReadOnlyList<LockOnCandidate> candidatos,
            out LockOnCandidate elegido
        )
        {
            elegido = default;
            bool encontrado = false;
            float mejorPuntaje = float.MaxValue;
            Vector2 angulosActual = Angulos(puntoActual - origen);

            foreach (var candidato in candidatos)
            {
                Vector3 hacia = candidato.Position - origen;
                if (candidato.Id == idActual || hacia.magnitude > _ajustes.Range)
                    continue;

                Vector2 angulos = Angulos(hacia);
                var delta = new Vector2(
                    Mathf.DeltaAngle(angulosActual.x, angulos.x),
                    angulos.y - angulosActual.y
                );
                if (delta.sqrMagnitude < DeltaMinimaCuadrada)
                    continue;

                float desvio = Vector2.Angle(direccion, delta);
                if (desvio > _ajustes.MaxAngle)
                    continue;

                float puntaje = delta.magnitude * (1f + desvio / _ajustes.MaxAngle);
                if (puntaje >= mejorPuntaje)
                    continue;

                mejorPuntaje = puntaje;
                elegido = candidato;
                encontrado = true;
            }

            return encontrado;
        }

        private static Vector2 Angulos(Vector3 direccion)
        {
            float yaw = Mathf.Atan2(direccion.x, direccion.z) * Mathf.Rad2Deg;
            float elevacion =
                Mathf.Asin(Mathf.Clamp(direccion.normalized.y, -1f, 1f)) * Mathf.Rad2Deg;
            return new Vector2(yaw, elevacion);
        }
    }
}
