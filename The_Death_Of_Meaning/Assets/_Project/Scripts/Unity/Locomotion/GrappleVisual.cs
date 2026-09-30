using Unity.Netcode;
using UnityEngine;

namespace TDOM.Unity.Locomotion
{
    public sealed class GrappleVisual : NetworkBehaviour
    {
        [SerializeField]
        private LineRenderer _lineRenderer;

        [SerializeField]
        private Transform _manoTransform;

        [SerializeField]
        private Vector3 _offset = new Vector3(0f, 1.2f, 0f);

        private bool _activo;
        private Vector3 _puntoAnclaje;

        public bool Activo => _activo;

        private void Awake()
        {
            EnsureLineRenderer();

            if (_lineRenderer != null)
            {
                _lineRenderer.enabled = false;
            }
        }

        private void OnDisable()
        {
            AplicarOcultar();
            if (IsOwner && IsSpawned)
            {
                OcultarRpc();
            }
        }

        public override void OnNetworkDespawn()
        {
            AplicarOcultar();
            base.OnNetworkDespawn();
        }

        private void EnsureLineRenderer()
        {
            if (_lineRenderer == null)
            {
                _lineRenderer = GetComponentInChildren<LineRenderer>();
            }
        }

        private Vector3 ObtenerOrigen()
        {
            return _manoTransform != null ? _manoTransform.position : transform.position + _offset;
        }

        private void LateUpdate()
        {
            if (!_activo || _lineRenderer == null)
                return;

            _lineRenderer.SetPosition(0, ObtenerOrigen());
            _lineRenderer.SetPosition(1, _puntoAnclaje);
        }

        public void Mostrar(Vector3 punto)
        {
            AplicarMostrar(punto);

            if (IsSpawned)
            {
                MostrarRpc(punto);
            }
        }

        [Rpc(SendTo.NotOwner)]
        private void MostrarRpc(Vector3 punto)
        {
            AplicarMostrar(punto);
        }

        private void AplicarMostrar(Vector3 punto)
        {
            _activo = true;
            _puntoAnclaje = punto;

            EnsureLineRenderer();
            if (_lineRenderer != null)
            {
                _lineRenderer.positionCount = 2;
                _lineRenderer.SetPosition(0, ObtenerOrigen());
                _lineRenderer.SetPosition(1, punto);
                _lineRenderer.enabled = true;
            }
        }

        public void Ocultar()
        {
            AplicarOcultar();

            if (IsSpawned)
            {
                OcultarRpc();
            }
        }

        [Rpc(SendTo.NotOwner)]
        private void OcultarRpc()
        {
            AplicarOcultar();
        }

        private void AplicarOcultar()
        {
            _activo = false;
            if (_lineRenderer != null)
            {
                _lineRenderer.enabled = false;
            }
        }
    }
}
