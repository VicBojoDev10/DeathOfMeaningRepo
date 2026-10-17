using Unity.Netcode;
using UnityEngine;

namespace TDOM.Unity.Combat
{
    public class BossGiroHaciaObjetivo : NetworkBehaviour
    {
        [SerializeField]
        private AtaquesArek _ataques;

        [SerializeField]
        private BossController _jefe;

        private ulong? _objetivoMirado;

        private void Awake()
        {
            if (_ataques == null)
                _ataques = GetComponent<AtaquesArek>();

            if (_jefe == null)
                _jefe = GetComponent<BossController>();
        }

        private void Update()
        {
            if (!IsServer || !IsSpawned || _ataques == null)
                return;

            if (PauseManager.PausadoGlobal || (_jefe != null && _jefe.Pausado))
                return;

            var perfil = _ataques.PerfilObjetivo;
            if (perfil == null)
                return;

            if (!_ataques.TryObjetivoVisible(out ulong objetivoId))
            {
                CambiarObjetivoMirado(null);
                return;
            }

            CambiarObjetivoMirado(objetivoId);

            if (!_ataques.TryPosicionJugador(objetivoId, out Vector3 posicion))
                return;

            Vector3 hacia = posicion - transform.position;
            hacia.y = 0f;
            if (hacia.sqrMagnitude < 0.0001f)
                return;

            Quaternion objetivo = Quaternion.LookRotation(hacia.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                objetivo,
                perfil.velocidadGiro * Time.deltaTime
            );
        }

        private void CambiarObjetivoMirado(ulong? objetivoId)
        {
            if (_objetivoMirado == objetivoId)
                return;

            _objetivoMirado = objetivoId;
            Debug.Log(
                objetivoId.HasValue
                    ? $"[BossGiro] Mirando a cliente {objetivoId.Value}."
                    : "[BossGiro] Ningún jugador dentro del radio de detección."
            );
        }

        private void OnDrawGizmosSelected()
        {
            var ataques = _ataques != null ? _ataques : GetComponent<AtaquesArek>();
            if (ataques == null || ataques.PerfilObjetivo == null)
                return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, ataques.PerfilObjetivo.radioDeteccion);
        }
    }
}
