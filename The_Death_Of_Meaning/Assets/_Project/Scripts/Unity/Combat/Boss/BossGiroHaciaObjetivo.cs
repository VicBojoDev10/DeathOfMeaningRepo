using Unity.Netcode;
using UnityEngine;

namespace TDOM.Unity.Combat
{
    /// <summary>
    /// Gira al jefe en Y hacia su objetivo mientras haya un jugador dentro del radio de detección
    /// (por agro o el más cercano, igual que AtaquesArek). Solo corre en el servidor; los clientes
    /// lo ven por el NetworkTransform de BossArek. Se detiene mientras el jefe está pausado
    /// (cinemáticas). La velocidad está en BossTargetingProfile.
    /// </summary>
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

        // Solo para depurar: avisa en consola cuando el jefe empieza a mirar a alguien o lo pierde.
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

        // Radio de detección en el editor (seleccionar BossArek).
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
