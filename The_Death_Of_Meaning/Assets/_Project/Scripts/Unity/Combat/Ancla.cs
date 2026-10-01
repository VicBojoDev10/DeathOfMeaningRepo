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

        private Vector3 _direccion;
        private AnchorProfile _perfil;

        private float _distanciaRecorrida;
        private float _tiempoPegada;
        private float _radio = 0.2f;

        private readonly RaycastHit[] _hitsBuffer = new RaycastHit[16];

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
                _tiempoPegada += dt;
                if (_tiempoPegada >= _perfil.StuckDuration)
                {
                    if (NetworkObject.IsSpawned)
                        NetworkObject.Despawn();
                }
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

                for (int i = 0; i < hitCount; i++)
                {
                    var hit = _hitsBuffer[i];
                    if (
                        hit.distance < minDistance
                        && hit.collider.GetComponentInParent<PlayerRoot>() == null
                    )
                    {
                        minDistance = hit.distance;
                        closestValidHit = hit;
                    }
                }

                if (closestValidHit.HasValue)
                {
                    transform.position = closestValidHit.Value.point;
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
                            ? root.transform.position + Vector3.up * 1.2f
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
