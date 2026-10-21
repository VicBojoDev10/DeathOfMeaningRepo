using System.Collections.Generic;
using TDOM.Data;
using TDOM.Gameplay.Core;
using TDOM.Unity.Camera;
using TDOM.Unity.Combat;
using TDOM.Unity.Player;
using Unity.Netcode;
using UnityEngine;

namespace TDOM.Unity
{
    public class AtaqueMano : AtaqueArekBase
    {
        [Header("Visuales")]
        [SerializeField]
        private Renderer _manoRenderer;

        [SerializeField]
        private Transform _manoVisual;

        [Header("Aviso")]
        [SerializeField]
        private Material _materialAviso; // AvisoAtaque.mat

        [SerializeField]
        private AvisoParpadeo _avisoParpadeo;

        private enum FaseMano
        {
            Aviso,
            Barrido,
            Retirada,
            Despawn,
        }

        // Tuning: lo lee de AtaquesArek (BossArek.prefab) al spawnear.
        private AtaqueManoProfile _perfil;

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

            if (_avisoParpadeo == null)
                _avisoParpadeo = GetComponent<AvisoParpadeo>();
        }

        private void ConfigurarGeometriaVisual()
        {
            if (_manoVisual != null)
            {
                _manoVisual.localScale = new Vector3(_perfil.ancho, _perfil.alto, _perfil.radio);
                _manoVisual.localPosition = new Vector3(
                    0f,
                    _perfil.alto * 0.5f,
                    _perfil.radio * 0.5f
                );
            }
        }

        protected override void AlSpawnear()
        {
            _perfil = Lanzador != null ? Lanzador.PerfilMano : null;
            if (_perfil == null)
            {
                Debug.LogError("[AtaqueMano] Falta el AtaqueManoProfile en AtaquesArek.");
                return;
            }

            ConfigurarGeometriaVisual();
            CrearArcoTelegraph();

            if (IsServer)
            {
                // Sale a la altura del piso, no a la del pivote del jefe (que puede estar arriba).
                Vector3 origen = transform.position;
                origen.y = AlturaDelPiso(origen);
                transform.position = origen;

                _fase = FaseMano.Aviso;
                _tiempoEnFase = 0f;
                _rotacionBase = transform.rotation;
                transform.rotation = _rotacionBase * Quaternion.Euler(0f, -90f, 0f);
                _jugadoresGolpeados.Clear();

                AvisoRpc();
            }
        }

        // Busca el piso debajo del punto; si hay varios golpes (jefe, jugadores), el más bajo es el piso.
        private static float AlturaDelPiso(Vector3 punto)
        {
            var golpes = Physics.RaycastAll(
                punto + Vector3.up * 30f,
                Vector3.down,
                100f,
                Physics.AllLayers,
                QueryTriggerInteraction.Ignore
            );

            if (golpes.Length == 0)
                return punto.y;

            float alturaMinima = float.MaxValue;
            foreach (var golpe in golpes)
                alturaMinima = Mathf.Min(alturaMinima, golpe.point.y);

            return alturaMinima;
        }

        private void Update()
        {
            if (
                PauseManager.PausadoGlobal
                || (PauseManager.ResultadoGlobal != ResultadoPartida.EnCurso)
            )
                return;
            if (!IsServer || _perfil == null)
                return;

            float dt = Time.deltaTime;
            _tiempoEnFase += dt;

            switch (_fase)
            {
                case FaseMano.Aviso:
                    transform.rotation = _rotacionBase * Quaternion.Euler(0f, -90f, 0f);
                    if (_tiempoEnFase >= _perfil.aviso)
                    {
                        _fase = FaseMano.Barrido;
                        _tiempoEnFase = 0f;
                    }
                    break;

                case FaseMano.Barrido:
                    float t = Mathf.Clamp01(_tiempoEnFase / _perfil.barrido);
                    float angulo = Mathf.Lerp(-90f, 90f, t);
                    transform.rotation = _rotacionBase * Quaternion.Euler(0f, angulo, 0f);

                    DetectarImpactos();

                    if (_tiempoEnFase >= _perfil.barrido)
                    {
                        _fase = FaseMano.Retirada;
                        _tiempoEnFase = 0f;
                    }
                    break;

                case FaseMano.Retirada:
                    if (_tiempoEnFase >= _perfil.retirada)
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
            Vector3 halfExtents = new Vector3(
                _perfil.ancho * 0.5f,
                _perfil.alto * 0.5f,
                _perfil.radio * 0.5f
            );
            Vector3 center =
                transform.position
                + transform.rotation * new Vector3(0f, _perfil.alto * 0.5f, _perfil.radio * 0.5f);
            Quaternion orientation = transform.rotation;

            Collider[] hits = Physics.OverlapBox(
                center,
                halfExtents,
                orientation,
                Physics.AllLayers,
                QueryTriggerInteraction.Collide
            );

            foreach (var hit in hits)
            {
                var damageable = hit.GetComponentInParent<IDamageable>();
                if (damageable != null)
                {
                    var netObj = hit.GetComponentInParent<NetworkObject>();
                    ulong clientId = netObj != null ? netObj.OwnerClientId : 0;
                    if (!_jugadoresGolpeados.Contains(clientId))
                    {
                        _jugadoresGolpeados.Add(clientId);
                        Debug.Log($"[AREK][Mano] golpeó a cliente {clientId}");
                        float dano = _perfil != null ? _perfil.Dano : 20f;
                        bool esInstaKill = _perfil != null && _perfil.EsInstaKill;
                        damageable.RecibirDanio(dano, esInstaKill);
                        if (netObj != null)
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

                if (_avisoParpadeo != null && _perfil != null)
                    _avisoParpadeo.Iniciar(
                        Lanzador.PerfilAviso,
                        _perfil.aviso,
                        new Renderer[] { _arcLineRenderer },
                        AvisoParpadeo.Forma.FranjaV
                    );
            }
        }

        [Rpc(SendTo.SpecifiedInParams)]
        private void JugadorGolpeadoRpc(RpcParams rpcParams = default)
        {
            _flashRojoTimer = 0.25f;
            NotificarGolpeAlJugadorLocal();
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
                float x = Mathf.Sin(anguloRad) * _perfil.radio;
                float z = Mathf.Cos(anguloRad) * _perfil.radio;
                posiciones[i + 1] = new Vector3(x, 0f, z);
            }

            _arcLineRenderer.SetPositions(posiciones);

            if (_materialAviso != null)
                _arcLineRenderer.sharedMaterial = _materialAviso;
            else
                Debug.LogWarning("[AtaqueMano] Falta asignar _materialAviso (AvisoAtaque.mat).");

            _arcLineRenderer.enabled = false;
        }

        private void OnGUI()
        {
            if (_flashRojoTimer > 0f)
            {
                _flashRojoTimer -= Time.deltaTime;
                Color prev = GUI.color;
                GUI.color = new Color(1f, 0f, 0f, 0.4f);
                GUI.DrawTexture(
                    new Rect(0, 0, Screen.width, Screen.height),
                    Texture2D.whiteTexture
                );
                GUI.color = prev;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (_perfil == null)
                return;

            Gizmos.color = Color.red;
            Vector3 halfExtents = new Vector3(
                _perfil.ancho * 0.5f,
                _perfil.alto * 0.5f,
                _perfil.radio * 0.5f
            );
            Vector3 center =
                transform.position
                + transform.rotation * new Vector3(0f, _perfil.alto * 0.5f, _perfil.radio * 0.5f);

            Matrix4x4 prev = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(center, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, halfExtents * 2f);
            Gizmos.matrix = prev;
        }
    }
}
