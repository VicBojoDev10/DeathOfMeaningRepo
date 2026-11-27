using TDOM.Data;
using TDOM.Gameplay.Core;
using TDOM.Unity.Audio;
using TDOM.Unity.Flow;
using TDOM.Unity.Input;
using TDOM.Unity.Player;
using TDOM.Unity.Vfx;
using Unity.Netcode;
using UnityEngine;

namespace TDOM.Unity.Combat
{
    public sealed class AnchorLauncher : NetworkBehaviour
    {
        private const float RetrasoLanzamiento = .7f;

        public bool AnclaActiva { get; internal set; }
        public bool MinionCapturado => _minionCapturadoId.Value != 0;

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

        [SerializeField]
        private SfxBanco _sfxLanzamiento;

        private SfxRed _sfxRed;

        private bool _esperandoSoltar;
        private bool _incapacitado;
        private bool _lanzamientoPendiente;
        private float _tiempoParaLanzar;
        private VfxDisparoSostenido _vfxAnclaSostenido;
        private bool _usoAnclaServidor;
        private float _energiaGastadaUsoServidor;
        private readonly NetworkVariable<ulong> _minionCapturadoId = new(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );
        private readonly NetworkVariable<Vector3> _direccionApuntada = new(
            Vector3.zero,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner
        );
        private float _tiempoCapturaServidor;
        private float _velocidadGiroActual;

        public void EstablecerIncapacitado(bool incapacitado)
        {
            _incapacitado = incapacitado;
            if (!incapacitado)
                return;

            if (IsServer)
            {
                _usoAnclaServidor = false;
                if (_minionCapturadoId.Value != 0)
                    LiberarMinionServidor(transform.forward);
            }

            AnclaActiva = false;
            _esperandoSoltar = true;
            _lanzamientoPendiente = false;
            if (IsOwner)
                DetenerVfxAncla();
            if (IsServer)
                DespawnearAncla();
        }

        public void CancelarPorGolpe()
        {
            if (IsServer)
            {
                _usoAnclaServidor = false;
                if (_minionCapturadoId.Value != 0)
                    LiberarMinionServidor(transform.forward);
            }
            AnclaActiva = false;
            _esperandoSoltar = true;
            _lanzamientoPendiente = false;
            if (IsOwner)
                DetenerVfxAncla();
            if (IsServer)
                DespawnearAncla();
        }

        private NetworkObject _anclaSpawnada;

        public AnchorProfile Perfil => _perfil;

        public Ancla AnclaActual { get; private set; }

        public void RegistrarAncla(Ancla ancla)
        {
            AnclaActual = ancla;
            if (_vfxAnclaSostenido != null)
                _vfxAnclaSostenido.Apuntar(ancla != null ? ancla.transform : null);
        }

        public void NotificarDespawnAncla(bool conecto)
        {
            if (IsServer)
            {
                _usoAnclaServidor = false;
                if (!conecto && _perfil != null && _perfil.ReembolsoSinConectar > 0f)
                {
                    _root.ReembolsarEnergiaEnServidor(
                        _energiaGastadaUsoServidor * _perfil.ReembolsoSinConectar
                    );
                }
                _energiaGastadaUsoServidor = 0f;
            }
            AnclaActiva = false;
            _esperandoSoltar = true;
            _lanzamientoPendiente = false;
            if (IsOwner)
                DetenerVfxAncla();
        }

        public override void OnNetworkSpawn()
        {
            PrepararVfxAncla();
            _sfxRed = GetComponentInParent<SfxRed>();
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
            if (
                PauseManager.PausadoGlobal
                || PauseManager.ResultadoGlobal != ResultadoPartida.EnCurso
            )
                return;

            if (IsServer)
            {
                ActualizarConsumoAnclaServidor(Time.deltaTime);
                ActualizarMinionCapturadoServidor(Time.deltaTime);
            }

            if (!IsOwner || _incapacitado || _perfil == null)
                return;

            float dt = Time.deltaTime;

            var input = _input.Read();
            bool holdingL2R1 = input.AimHeld && input.GrappleHeld;

            if (_minionCapturadoId.Value != 0)
            {
                _direccionApuntada.Value = _root.ResolverApuntado(_origen.position).Direction;
                if (!holdingL2R1)
                    SoltarMinionRpc(_direccionApuntada.Value);
                return;
            }

            if (holdingL2R1 && !_root.AtaqueActivo && !AnclaActiva && !_esperandoSoltar)
            {
                if (!_root.PuedePagarGancho)
                {
                    _root.NotificarFaltaEnergia();
                    _esperandoSoltar = true;
                    return;
                }
                AnclaActiva = true;
                _lanzamientoPendiente = true;
                _tiempoParaLanzar = RetrasoLanzamiento;
            }

            if (AnclaActiva && _lanzamientoPendiente && holdingL2R1)
            {
                _tiempoParaLanzar -= dt;
                if (_tiempoParaLanzar <= 0f)
                {
                    _lanzamientoPendiente = false;
                    var direccion = _root.ResolverApuntado(_origen.position).Direction;
                    LanzarAnclaRpc(_origen.position, direccion);
                    LanzarVfxAncla(direccion);
                }
            }
            else if (AnclaActiva && !holdingL2R1)
            {
                AnclaActiva = false;
                SolicitarDetenerUsoAnclaRpc();
                _lanzamientoPendiente = false;
                SoltarAnclaRpc();
            }

            if (!holdingL2R1)
            {
                _esperandoSoltar = false;
            }
        }

        public Vector3 DireccionLanzamiento =>
            _direccionApuntada.Value.sqrMagnitude > 0.001f
                ? _direccionApuntada.Value
                : transform.forward;

        public float AvanzarGiroMinion(float dt)
        {
            if (!IsOwner || !MinionCapturado || _perfil == null)
                return 0f;
            _velocidadGiroActual = Mathf.Min(
                Mathf.Max(0f, _perfil.VelocidadGiroMaximaMinion),
                _velocidadGiroActual + Mathf.Max(0f, _perfil.AceleracionGiroMinion) * dt
            );
            return _velocidadGiroActual * dt;
        }

        public void IntentarCapturarMinion(MinionController minion)
        {
            if (!IsServer || minion == null || _perfil == null || _minionCapturadoId.Value != 0)
                return;
            if (!minion.AgarrarPorZendre(transform, _perfil))
                return;
            _minionCapturadoId.Value = minion.NetworkObjectId;
            _tiempoCapturaServidor = 0f;
            _velocidadGiroActual = Mathf.Max(0f, _perfil.VelocidadGiroMinion);
            _root?.ActivarVulnerabilidadCapturaMinion(1f);
        }

        public void IntentarCapturarMinion(SniperMinionController minion)
        {
            if (!IsServer || minion == null || _perfil == null || _minionCapturadoId.Value != 0)
                return;
            if (!minion.AgarrarPorZendre(transform, _perfil))
                return;
            _minionCapturadoId.Value = minion.NetworkObjectId;
            _tiempoCapturaServidor = 0f;
            _velocidadGiroActual = Mathf.Max(0f, _perfil.VelocidadGiroMinion);
            _root?.ActivarVulnerabilidadCapturaMinion(1f);
        }

        private void ActualizarMinionCapturadoServidor(float dt)
        {
            if (_minionCapturadoId.Value == 0)
                return;
            if (
                NetworkManager?.SpawnManager == null
                || !NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(
                    _minionCapturadoId.Value,
                    out var capturado
                )
                || capturado == null
                || capturado.GetComponent<MinionController>()?.EstaMuerto == true
                || capturado.GetComponent<SniperMinionController>()?.EstaMuerto == true
            )
            {
                _minionCapturadoId.Value = 0;
                _tiempoCapturaServidor = 0f;
                return;
            }
            _tiempoCapturaServidor += dt;
            if (_tiempoCapturaServidor < Mathf.Max(0.1f, _perfil.TiempoMaximoGiroMinion))
                return;
            LiberarMinionServidor(DireccionLanzamiento);
        }

        [Rpc(SendTo.Server)]
        private void SoltarMinionRpc(Vector3 direccion) => LiberarMinionServidor(direccion);

        private void LiberarMinionServidor(Vector3 direccion)
        {
            if (!IsServer || _minionCapturadoId.Value == 0)
                return;
            ulong id = _minionCapturadoId.Value;
            if (
                NetworkManager?.SpawnManager != null
                && NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(id, out var obj)
            )
            {
                var normal = obj.GetComponent<MinionController>();
                var sniper = obj.GetComponent<SniperMinionController>();
                if (normal != null)
                    normal.SoltarPorZendre(direccion);
                else if (sniper != null)
                    sniper.SoltarPorZendre(direccion);
            }
            _minionCapturadoId.Value = 0;
            _tiempoCapturaServidor = 0f;
            _velocidadGiroActual = 0f;
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
            if (_incapacitado || _perfil == null)
            {
                RechazarLanzamientoServidor();
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
                RechazarLanzamientoServidor();
                return;
            }

            if (_root.Energia.Value <= 0f)
            {
                RechazarLanzamientoServidor();
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
                _usoAnclaServidor = _perfil.StaminaCosto > 0f;
                _energiaGastadaUsoServidor = 0f;
            }

            ReproducirSfxLanzamiento(origen);
        }

        private void ReproducirSfxLanzamiento(Vector3 origen)
        {
            if (_sfxRed != null && _sfxLanzamiento != null)
                _sfxRed.Reproducir(_sfxLanzamiento, origen);
        }

        private void RechazarLanzamientoServidor()
        {
            _usoAnclaServidor = false;
            RechazarLanzamientoRpc();
        }

        [Rpc(SendTo.Server)]
        private void SoltarAnclaRpc()
        {
            _usoAnclaServidor = false;
            DespawnearAncla();
        }

        [Rpc(SendTo.Server)]
        private void SolicitarDetenerUsoAnclaRpc()
        {
            _usoAnclaServidor = false;
        }

        private void ActualizarConsumoAnclaServidor(float deltaTime)
        {
            if (!_usoAnclaServidor || _perfil == null || _root == null)
                return;

            float costo = _perfil.StaminaCosto * deltaTime;
            if (costo <= 0f)
                return;

            float gastado = _root.ConsumirEnergiaParcialEnServidor(costo);
            _energiaGastadaUsoServidor += gastado;
            if (gastado + 0.0001f < costo)
            {
                _usoAnclaServidor = false;
                DespawnearAncla();
                AgotarUsoAnclaRpc();
            }
        }

        [Rpc(SendTo.Owner)]
        private void AgotarUsoAnclaRpc()
        {
            AnclaActiva = false;
            _lanzamientoPendiente = false;
            _esperandoSoltar = true;
            DetenerVfxAncla();
            _root?.NotificarFaltaEnergia();
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
            AnclaActiva = false;
            _lanzamientoPendiente = false;
            _esperandoSoltar = true;
            if (_root != null && !_root.PuedePagarGancho)
                _root.NotificarFaltaEnergia();
        }
    }
}
