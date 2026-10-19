using TDOM.Data;
using TDOM.Unity.Player;
using Unity.Netcode;
using UnityEngine;

namespace TDOM.Unity.Combat
{
    public sealed class Ancla : NetworkBehaviour
    {
        [SerializeField]
        private LineRenderer _lineRenderer;

        private NetworkVariable<ulong> _duenoId = new NetworkVariable<ulong>();

        public NetworkVariable<bool> Pegada = new NetworkVariable<bool>();
        public bool PegadaEnOjo { get; private set; }

        private Vector3 _direccion;
        private AnchorProfile _perfil;

        private float _distanciaRecorrida;
        private float _tiempoPegada;
        private float _radio = 0.2f;
        private OjosArek _ojosAnclados;
        private HitZone _ojoAnclado;

        private readonly RaycastHit[] _hitsBuffer = new RaycastHit[16];

        public override void OnNetworkSpawn()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.SpawnManager != null)
            {
                if (
                    NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(
                        _duenoId.Value,
                        out var duenoObj
                    )
                )
                {
                    var launcher = duenoObj.GetComponentInChildren<AnchorLauncher>();
                    if (launcher != null)
                    {
                        launcher.RegistrarAncla(this);
                    }
                }
            }
        }

        public void Inicializar(Vector3 dir, AnchorProfile perfil, ulong duenoId)
        {
            _direccion = dir.normalized;
            _perfil = perfil;
            _duenoId.Value = duenoId;
        }

        private void Update()
        {
            if (IsServer)
                UpdateServer();

            UpdateClientVisuals();
        }

        private void UpdateServer()
        {
            if (_perfil == null)
                return;

            float dt = Time.deltaTime;

            if (Pegada.Value)
            {
                if (_ojoAnclado != null && !_ojosAnclados.EstaAbierto(_ojoAnclado))
                {
                    Despegar();
                    return;
                }

                _tiempoPegada += dt;
                float duracion = PegadaEnOjo ? _perfil.StuckDurationOnEye : _perfil.StuckDuration;
                if (_tiempoPegada >= duracion && NetworkObject.IsSpawned)
                    NetworkObject.Despawn();
            }
            else
            {
                float step = _perfil.TravelSpeed * dt;

                int hitCount = Physics.SphereCastNonAlloc(
                    transform.position,
                    _radio,
                    _direccion,
                    _hitsBuffer,
                    step,
                    Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore
                );

                float minDistance = float.MaxValue;
                RaycastHit? closestValidHit = null;
                OjosArek ojosImpactados = null;
                HitZone zonaImpactada = null;
                bool tocandoInicialmente = false;
                OjosArek ojosContactoInicial = null;
                HitZone zonaContactoInicial = null;

                for (int i = 0; i < hitCount; i++)
                {
                    var hit = _hitsBuffer[i];

                    if (!PuedeAnclarse(hit.collider, out var ojos, out var zona))
                        continue;

                    if (hit.distance <= 0f)
                    {
                        tocandoInicialmente = true;
                        if (ojos != null && zona != null)
                        {
                            ojosContactoInicial = ojos;
                            zonaContactoInicial = zona;
                        }
                        continue;
                    }

                    if (hit.distance < minDistance)
                    {
                        minDistance = hit.distance;
                        closestValidHit = hit;
                        ojosImpactados = ojos;
                        zonaImpactada = zona;
                    }
                }

                if (closestValidHit.HasValue)
                {
                    var hit = closestValidHit.Value;
                    transform.position = hit.point;
                    if (ojosImpactados != null && zonaImpactada != null)
                        FijarOjo(ojosImpactados, zonaImpactada);
                    Pegada.Value = true;
                    _tiempoPegada = 0f;
                }
                else if (tocandoInicialmente)
                {
                    PegadaEnOjo = ojosContactoInicial != null && zonaContactoInicial != null;
                    if (PegadaEnOjo)
                        FijarOjo(ojosContactoInicial, zonaContactoInicial);
                    Pegada.Value = true;
                    _tiempoPegada = 0f;
                }
                else
                {
                    transform.position += _direccion * step;
                    _distanciaRecorrida += step;

                    if (_distanciaRecorrida >= _perfil.Range)
                    {
                        if (NetworkObject.IsSpawned)
                            NetworkObject.Despawn();
                    }
                }
            }
        }

        private bool PuedeAnclarse(Collider collider, out OjosArek ojos, out HitZone zona)
        {
            ojos = collider.GetComponentInParent<OjosArek>();
            zona = collider.GetComponentInParent<HitZone>();

            if (collider.GetComponentInParent<PlayerRoot>() != null)
                return false;

            var jefe = collider.GetComponentInParent<BossController>();
            if (jefe != null)
                return ojos != null && zona != null && ojos.EstaAbierto(zona);

            return true;
        }

        private void FijarOjo(OjosArek ojos, HitZone zona)
        {
            _ojosAnclados = ojos;
            _ojoAnclado = zona;
            PegadaEnOjo = true;
        }

        private void Despegar()
        {
            _ojosAnclados = null;
            _ojoAnclado = null;
            PegadaEnOjo = false;
            Pegada.Value = false;
            _tiempoPegada = 0f;
            if (NetworkObject.IsSpawned)
                NetworkObject.Despawn();
        }

        public override void OnNetworkDespawn()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.SpawnManager != null)
            {
                if (
                    NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(
                        _duenoId.Value,
                        out var duenoObj
                    )
                )
                {
                    var launcher = duenoObj.GetComponentInChildren<AnchorLauncher>();
                    if (launcher != null)
                    {
                        launcher.NotificarDespawnAncla();
                    }
                }
            }
        }

        private void UpdateClientVisuals()
        {
            if (_lineRenderer == null)
                return;

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.SpawnManager != null)
            {
                if (
                    NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(
                        _duenoId.Value,
                        out var duenoObj
                    )
                )
                {
                    var root = duenoObj.GetComponent<PlayerRoot>();
                    Vector3 startPos =
                        root != null
                            ? root.transform.position + Vector3.up * 0.4f
                            : duenoObj.transform.position;

                    _lineRenderer.positionCount = 2;
                    _lineRenderer.SetPosition(0, startPos);
                    _lineRenderer.SetPosition(1, transform.position);
                    return;
                }
            }

            _lineRenderer.positionCount = 0;
        }
    }
}
