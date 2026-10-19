using System.Collections.Generic;
using UnityEngine;

namespace TDOM.Gameplay.Camera
{
    public sealed class LockOnResolver
    {
        public const int SinObjetivo = -1;
        private const float DistanciaMinimaCuadrada = 0.0001f;

        private readonly LockOnSettings _ajustes;
        private float _velocidadYaw;
        private float _velocidadPitch;

        public bool Activo => ObjetivoId != SinObjetivo;
        public int ObjetivoId { get; private set; } = SinObjetivo;
        public Vector3 PuntoObjetivo { get; private set; }
        public float Yaw { get; private set; }
        public float Pitch { get; private set; }

        public LockOnResolver(LockOnSettings ajustes)
        {
            _ajustes = ajustes;
        }

        public void Tick(
            bool alternar,
            Vector3 origen,
            Vector3 frenteCamara,
            float yawCamara,
            float pitchCamara,
            IReadOnlyList<LockOnCandidate> candidatos,
            float dt
        )
        {
            if (alternar)
                Alternar(origen, frenteCamara, yawCamara, pitchCamara, candidatos);

            if (!Activo)
                return;

            if (!TryObjetivoVigente(origen, candidatos, out Vector3 punto))
            {
                Liberar();
                return;
            }

            PuntoObjetivo = punto;
            Seguir(origen, punto, dt);
        }

        public float YawHacia(Vector3 desde)
        {
            Vector3 direccion = PuntoObjetivo - desde;
            return Mathf.Atan2(direccion.x, direccion.z) * Mathf.Rad2Deg;
        }

        public void FijarA(LockOnCandidate candidato)
        {
            if (!Activo)
                return;

            ObjetivoId = candidato.Id;
            PuntoObjetivo = candidato.Position;
        }

        public void Liberar()
        {
            ObjetivoId = SinObjetivo;
            _velocidadYaw = 0f;
            _velocidadPitch = 0f;
        }

        private void Alternar(
            Vector3 origen,
            Vector3 frenteCamara,
            float yawCamara,
            float pitchCamara,
            IReadOnlyList<LockOnCandidate> candidatos
        )
        {
            if (Activo)
            {
                Liberar();
                return;
            }

            if (!TrySeleccionar(origen, frenteCamara, candidatos, out LockOnCandidate elegido))
                return;

            ObjetivoId = elegido.Id;
            PuntoObjetivo = elegido.Position;
            Yaw = yawCamara;
            Pitch = pitchCamara;
        }

        private bool TrySeleccionar(
            Vector3 origen,
            Vector3 frenteCamara,
            IReadOnlyList<LockOnCandidate> candidatos,
            out LockOnCandidate elegido
        )
        {
            elegido = default;
            bool encontrado = false;
            float mejorAngulo = float.MaxValue;
            float mejorDistancia = float.MaxValue;

            foreach (var candidato in candidatos)
            {
                Vector3 haciaCandidato = candidato.Position - origen;
                float distancia = haciaCandidato.magnitude;
                if (distancia > _ajustes.Range)
                    continue;

                float angulo = Vector3.Angle(frenteCamara, haciaCandidato);
                if (!EsMejor(angulo, distancia, mejorAngulo, mejorDistancia))
                    continue;

                elegido = candidato;
                mejorAngulo = angulo;
                mejorDistancia = distancia;
                encontrado = true;
            }

            return encontrado;
        }

        private static bool EsMejor(
            float angulo,
            float distancia,
            float mejorAngulo,
            float mejorDistancia
        )
        {
            return angulo < mejorAngulo
                || (Mathf.Approximately(angulo, mejorAngulo) && distancia < mejorDistancia);
        }

        private bool TryObjetivoVigente(
            Vector3 origen,
            IReadOnlyList<LockOnCandidate> candidatos,
            out Vector3 punto
        )
        {
            punto = default;
            foreach (var candidato in candidatos)
            {
                if (candidato.Id != ObjetivoId)
                    continue;

                punto = candidato.Position;
                return (punto - origen).magnitude <= _ajustes.Range;
            }

            return false;
        }

        private void Seguir(Vector3 origen, Vector3 punto, float dt)
        {
            Vector3 direccion = punto - origen;
            if (direccion.sqrMagnitude < DistanciaMinimaCuadrada)
                return;

            direccion.Normalize();
            float yawObjetivo = Mathf.Atan2(direccion.x, direccion.z) * Mathf.Rad2Deg;
            float pitchObjetivo = Mathf.Clamp(
                -Mathf.Asin(direccion.y) * Mathf.Rad2Deg,
                _ajustes.PitchMin,
                _ajustes.PitchMax
            );

            Yaw = Mathf.SmoothDampAngle(
                Yaw,
                yawObjetivo,
                ref _velocidadYaw,
                _ajustes.SmoothTime,
                float.PositiveInfinity,
                dt
            );
            Pitch = Mathf.SmoothDamp(
                Pitch,
                pitchObjetivo,
                ref _velocidadPitch,
                _ajustes.SmoothTime,
                float.PositiveInfinity,
                dt
            );
        }
    }
}
