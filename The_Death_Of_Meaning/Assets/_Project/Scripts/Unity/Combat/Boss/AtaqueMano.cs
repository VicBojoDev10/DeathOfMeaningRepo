using System.Collections.Generic;
using TDOM.Unity.Camera;
using TDOM.Unity.Combat;
using TDOM.Unity.Player;
using Unity.Netcode;
using UnityEngine;

namespace TDOM.Unity
{
    public class AtaqueMano : NetworkBehaviour
    {
        [Header("Tiempos")]
        [SerializeField]
        private float _aviso = 1.25f;

        [SerializeField]
        private float _barrido = 1.0f;

        [SerializeField]
        private float _retirada = 0.3f;

        [Header("Dimensiones de la Mano")]
        [SerializeField]
        private float _radio = 25f;

        [SerializeField]
        private float _alto = 1.2f;

        [SerializeField]
        private float _ancho = 2.0f;

        [Header("Visuales")]
        [SerializeField]
        private Renderer _manoRenderer;

        [SerializeField]
        private Transform _manoVisual;

        private enum FaseMano
        {
            Aviso,
            Barrido,
            Retirada,
            Despawn
        }

        private FaseMano _fase = FaseMano.Aviso;
        private float _tiempoEnFase = 0f;
        private Quaternion _rotacionBase = Quaternion.identity;
        private readonly HashSet<ulong> _jugadoresGolpeados = new HashSet<ulong>();

        // Flash de pantalla en el cliente golpeado
        private static float _flashRojoTimer = 0f;
        private LineRenderer _arcLineRenderer;

        private void Awake()
        {
            if (_manoRenderer == null)
                _manoRenderer = GetComponentInChildren<Renderer>();

            if (_manoVisual == null && _manoRenderer != null)
                _manoVisual = _manoRenderer.transform;

            ConfigurarGeometriaVisual();
            CrearArcoTelegraph();
        }

        private void ConfigurarGeometriaVisual()
        {
            if (_manoVisual != null)
            {
                _manoVisual.localScale = new Vector3(_ancho, _alto, _radio);
                _manoVisual.localPosition = new Vector3(0f, _alto * 0.5f, _radio * 0.5f);
            }
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                _fase = FaseMano.Aviso;
                _tiempoEnFase = 0f;
                _rotacionBase = transform.rotation;
                transform.rotation = _rotacionBase * Quaternion.Euler(0f, -90f, 0f);
                _jugadoresGolpeados.Clear();

                AvisoRpc();
            }
        }

        private void Update()
        {
            if (!IsServer)
                return;

            float dt = Time.deltaTime;
            _tiempoEnFase += dt;

            switch (_fase)
            {
                case FaseMano.Aviso:
                    transform.rotation = _rotacionBase * Quaternion.Euler(0f, -90f, 0f);
                    if (_tiempoEnFase >= _aviso)
                    {
                        _fase = FaseMano.Barrido;
                        _tiempoEnFase = 0f;
                    }
                    break;

                case FaseMano.Barrido:
                    float t = Mathf.Clamp01(_tiempoEnFase / _barrido);
                    float angulo = Mathf.Lerp(-90f, 90f, t);
                    transform.rotation = _rotacionBase * Quaternion.Euler(0f, angulo, 0f);

                    DetectarImpactos();

                    if (_tiempoEnFase >= _barrido)
                    {
                        _fase = FaseMano.Retirada;
                        _tiempoEnFase = 0f;
                    }
                    break;

                case FaseMano.Retirada:
                    if (_tiempoEnFase >= _retirada)
                    {
                        _fase = FaseMano.Despawn;
                        var no = GetComponent<NetworkObject>();
                        if (no != null && no.IsSpawned)
                            no.Despawn(true);
                        else
                            Destroy(gameObject);
                    }
                    break;
            }
        }

        private void DetectarImpactos()
        {
            Vector3 halfExtents = new Vector3(_ancho * 0.5f, _alto * 0.5f, _radio * 0.5f);
            Vector3 center = transform.position + transform.rotation * new Vector3(0f, _alto * 0.5f, _radio * 0.5f);
            Quaternion orientation = transform.rotation;

            Collider[] hits = Physics.OverlapBox(center, halfExtents, orientation, Physics.AllLayers, QueryTriggerInteraction.Collide);

            foreach (var hit in hits)
            {
                var player = hit.GetComponentInParent<PlayerRoot>();
                if (player != null)
                {
                    ulong clientId = player.OwnerClientId;
                    if (!_jugadoresGolpeados.Contains(clientId))
                    {
                        _jugadoresGolpeados.Add(clientId);
                        Debug.Log($"[AREK][Mano] golpeó a cliente {clientId}");
                        JugadorGolpeadoRpc(RpcTarget.Single(clientId, RpcTargetUse.Temp));
                    }
                }
            }
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void AvisoRpc()
        {
            if (_manoRenderer != null)
            {
                _manoRenderer.material.color = Color.red;
            }

            if (_arcLineRenderer != null)
            {
                _arcLineRenderer.enabled = true;
            }
        }

        [Rpc(SendTo.SpecifiedInParams)]
        private void JugadorGolpeadoRpc(RpcParams rpcParams = default)
        {
            _flashRojoTimer = 0.25f;

            var localPlayer = NetworkManager.Singleton?.LocalClient?.PlayerObject;
            if (localPlayer != null)
            {
                var cameraRig = localPlayer.GetComponentInChildren<PlayerCameraRig>();
                if (cameraRig != null)
                {
                    cameraRig.Impacto(2.5f);
                }

                var debugFeedback = localPlayer.GetComponentInChildren<CombatDebugFeedback>();
                if (debugFeedback != null)
                {
                    debugFeedback.FlashActive(0.2f);
                }
            }
        }

        private void CrearArcoTelegraph()
        {
            GameObject arcObj = new GameObject("FloorArcTelegraph");
            arcObj.transform.SetParent(transform, false);
            arcObj.transform.localPosition = new Vector3(0f, 0.05f, 0f);

            _arcLineRenderer = arcObj.AddComponent<LineRenderer>();
            _arcLineRenderer.useWorldSpace = false;
            _arcLineRenderer.loop = false;
            _arcLineRenderer.startWidth = 0.4f;
            _arcLineRenderer.endWidth = 0.4f;

            int segmentos = 36;
            _arcLineRenderer.positionCount = segmentos + 2;

            Vector3[] posiciones = new Vector3[segmentos + 2];
            posiciones[0] = Vector3.zero;

            for (int i = 0; i <= segmentos; i++)
            {
                float anguloRad = Mathf.Deg2Rad * Mathf.Lerp(-90f, 90f, (float)i / segmentos);
                float x = Mathf.Sin(anguloRad) * _radio;
                float z = Mathf.Cos(anguloRad) * _radio;
                posiciones[i + 1] = new Vector3(x, 0f, z);
            }

            _arcLineRenderer.SetPositions(posiciones);

            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");

            if (shader != null)
            {
                Material mat = new Material(shader);
                mat.color = new Color(1f, 0f, 0f, 0.35f);
                _arcLineRenderer.material = mat;
            }

            _arcLineRenderer.startColor = new Color(1f, 0f, 0f, 0.35f);
            _arcLineRenderer.endColor = new Color(1f, 0f, 0f, 0.35f);
            _arcLineRenderer.enabled = false;
        }

        private void OnGUI()
        {
            if (_flashRojoTimer > 0f)
            {
                _flashRojoTimer -= Time.deltaTime;
                Color prev = GUI.color;
                GUI.color = new Color(1f, 0f, 0f, 0.4f);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
                GUI.color = prev;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Vector3 halfExtents = new Vector3(_ancho * 0.5f, _alto * 0.5f, _radio * 0.5f);
            Vector3 center = transform.position + transform.rotation * new Vector3(0f, _alto * 0.5f, _radio * 0.5f);

            Matrix4x4 prev = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(center, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, halfExtents * 2f);
            Gizmos.matrix = prev;
        }
    }
}
