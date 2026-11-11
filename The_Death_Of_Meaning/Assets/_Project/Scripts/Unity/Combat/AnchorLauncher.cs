using TDOM.Data;
using TDOM.Gameplay.Core;
using TDOM.Unity.Input;
using TDOM.Unity.Player;
using TDOM.Unity.Vfx;
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

        [SerializeField]
        private VfxRed _vfxRed;

        [SerializeField]
        private VfxBase _vfxAncla;

        private bool _esperandoSoltar;
        private bool _incapacitado;
        private VfxDisparoSostenido _vfxAnclaSostenido;

        public void EstablecerIncapacitado(bool incapacitado)
        {
            _incapacitado = incapacitado;
            if (!incapacitado)
                return;

            AnclaActiva = false;
            _esperandoSoltar = true;
            if (IsOwner)
                DetenerVfxAncla();
            if (IsServer)
                DespawnearAncla();
        }

        private CooldownTimer _cooldown;
        private NetworkObject _anclaSpawnada;

        public AnchorProfile Perfil => _perfil;
        public float CooldownNormalizado => _cooldown != null ? _cooldown.Normalizado : 0f;

        public Ancla AnclaActual { get; private set; }

        public void RegistrarAncla(Ancla ancla)
        {
            AnclaActual = ancla;
            if (_vfxAnclaSostenido != null)
                _vfxAnclaSostenido.Apuntar(ancla != null ? ancla.transform : null);
        }

        public void NotificarDespawnAncla(bool conecto)
        {
            AnclaActiva = false;
            _esperandoSoltar = true;
            if (IsOwner)
                DetenerVfxAncla();
            if (!conecto && IsOwner && _cooldown != null)
                _cooldown.Recortar(_perfil.FactorCooldownSinConectar);
        }

        public override void OnNetworkSpawn()
        {
            if (_perfil != null)
                _cooldown = new CooldownTimer(_perfil.Cooldown);

            PrepararVfxAncla();
        }

        private void PrepararVfxAncla()
        {
            if (_vfxRed == null)
                _vfxRed = GetComponentInParent<VfxRed>();

            VfxPool pool = _vfxRed != null ? _vfxRed.Pool : null;
            if (pool == null || _vfxAncla == null)
                return;

            _vfxAnclaSostenido = pool.Pedir(pool.Indice(_vfxAncla)) as VfxDisparoSostenido;
            if (_vfxAnclaSostenido != null)
                _vfxAnclaSostenido.Seguir(_origen);
        }

        private void Update()
        {
            if (!IsOwner || _incapacitado || _perfil == null || _cooldown == null)
                return;

            float dt = Time.deltaTime;
            _cooldown.Tick(dt);

            var input = _input.Read();
            bool holdingL2R1 = input.AimHeld && input.GrappleHeld;

            if (
                holdingL2R1
                && !_root.AtaqueActivo
                && _cooldown.Listo
                && !AnclaActiva
                && !_esperandoSoltar
            )
            {
                _cooldown.Disparar();
                AnclaActiva = true;
                var direccion = _root.ResolverApuntado(_origen.position).Direction;
                LanzarAnclaRpc(_origen.position, direccion);
                LanzarVfxAncla(direccion);
            }
            else if (AnclaActiva && !holdingL2R1)
            {
                AnclaActiva = false;
                SoltarAnclaRpc();
            }

            if (!holdingL2R1)
            {
                _esperandoSoltar = false;
            }
        }

        private void LanzarVfxAncla(Vector3 direccion)
        {
            if (_vfxRed == null || _vfxAncla == null || direccion == Vector3.zero)
                return;

            _vfxRed.Lanzar(_vfxAncla, _origen.position, Quaternion.LookRotation(direccion));
        }

        private void DetenerVfxAncla()
        {
            if (_vfxRed != null && _vfxAncla != null)
                _vfxRed.Detener(_vfxAncla);
        }

        [Rpc(SendTo.Server)]
        private void LanzarAnclaRpc(Vector3 origen, Vector3 dir)
        {
            if (_incapacitado)
            {
                RechazarLanzamientoRpc();
                return;
            }

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

            var rotacion = dir != Vector3.zero ? Quaternion.LookRotation(dir) : Quaternion.identity;
            var go = Instantiate(_anclaPrefab, origen, rotacion);
            var ancla = go.GetComponent<Ancla>();

            if (ancla != null)
            {
                ancla.Inicializar(dir, _perfil, NetworkObjectId);
            }

            var netObj = go.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                netObj.Spawn();
                _anclaSpawnada = netObj;
            }
        }

        [Rpc(SendTo.Server)]
        private void SoltarAnclaRpc()
        {
            DespawnearAncla();
        }

        private void DespawnearAncla()
        {
            if (_anclaSpawnada != null && _anclaSpawnada.IsSpawned)
            {
                _anclaSpawnada.Despawn();
                _anclaSpawnada = null;
            }
        }

        [Rpc(SendTo.Owner)]
        private void RechazarLanzamientoRpc()
        {
            DetenerVfxAncla();
            if (_cooldown != null)
            {
                _cooldown.Resetear();
                AnclaActiva = false;
            }
        }
    }
}
