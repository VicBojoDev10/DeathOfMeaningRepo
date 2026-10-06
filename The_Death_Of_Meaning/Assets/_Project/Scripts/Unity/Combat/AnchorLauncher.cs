using TDOM.Data;
using TDOM.Gameplay.Core;
using TDOM.Unity.Input;
using TDOM.Unity.Player;
using Unity.Netcode;
using UnityEngine;

namespace TDOM.Unity.Combat
{
    public sealed class AnchorLauncher : NetworkBehaviour
    {
        public bool AnclaActiva { get; internal set; }

        [SerializeField]
        private AnchorProfile _perfil;

        [SerializeField]
        private PlayerInputReader _input;

        [SerializeField]
        private PlayerRoot _root;

        [SerializeField]
        private Transform _origen;

        [SerializeField]
        private GameObject _anclaPrefab;

        private CooldownTimer _cooldown;

        public override void OnNetworkSpawn()
        {
            if (_perfil != null)
                _cooldown = new CooldownTimer(_perfil.Cooldown);
        }

        private void Update()
        {
            if (!IsOwner || _perfil == null || _cooldown == null)
                return;

            float dt = Time.deltaTime;
            _cooldown.Tick(dt);

            var input = _input.Read();

            if (
                input.AimHeld
                && input.GrapplePressed
                && !_root.AtaqueActivo
                && _cooldown.Listo
                && !AnclaActiva
            )
            {
                _cooldown.Disparar();
                AnclaActiva = true;
                LanzarAnclaRpc(_origen.position, _origen.forward);
            }
        }

        [Rpc(SendTo.Server)]
        private void LanzarAnclaRpc(Vector3 origen, Vector3 dir)
        {
            float limit =
                _perfil != null && _perfil.ToleranciaOrigen > 0f ? _perfil.ToleranciaOrigen : 6f;
            float distance = Vector3.Distance(origen, transform.position);

            if (distance > limit)
            {
                Debug.LogWarning(
                    $"[AnchorLauncher] Lanzamiento rechazado: distancia {distance:F2} > límite {limit:F2}. Zendre se movía demasiado rápido."
                );
                RechazarLanzamientoRpc();
                return;
            }

            var go = Instantiate(_anclaPrefab, origen, Quaternion.identity);
            var ancla = go.GetComponent<Ancla>();

            if (ancla != null)
            {
                ancla.Inicializar(dir, _perfil, NetworkObjectId);
            }

            var netObj = go.GetComponent<NetworkObject>();
            if (netObj != null)
                netObj.Spawn();
        }

        [Rpc(SendTo.Owner)]
        private void RechazarLanzamientoRpc()
        {
            if (_cooldown != null)
            {
                _cooldown.Resetear();
                AnclaActiva = false;
            }
        }
    }
}
